// ReSharper disable AsyncVoidMethod
namespace AtcSoft.Deployment.Manager.Wpf.App;

public partial class App
{
    private readonly ILogger<App> logger;
    private readonly IHost host;
    private IConfiguration? configuration;

    public App()
    {
        var dataFolderError = EnsureDataFoldersAreCreated();
        RestoreInstallerCustomAppSettingsIfNeeded();

        host = Host.CreateDefaultBuilder()
            .ConfigureLogging((_, logging) =>
                {
                    logging
                        .ClearProviders()
                        .SetMinimumLevel(LogLevel.Trace)
                        .AddDebug()
                        .AddSerilog(CreateLoggerConfigurationForSerilogFileLog());
                })
            .ConfigureAppConfiguration(
                configurationBuilder =>
                {
                    // Next to the exe, not the current folder: a shortcut or a terminal can start the app from anywhere.
                    configuration = configurationBuilder
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile(Constants.CustomAppSettingsFileName, optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
                })
            .ConfigureServices((_, services) => ConfigureServices(services))
            .Build();

        ServiceProvider = host.Services;
        logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<App>();

        if (dataFolderError is not null)
        {
            logger.LogError(dataFolderError, "Could not create the data folders under {DataFolder}", WpfApp.DataPaths.MachineRoot.FullName);
        }

        if (TryParsePath(
                host.Services.GetRequiredService<IConfiguration>()["Application:TempPath"],
                out var directoryInfo))
        {
            MoveOldTempPathIfNeeded(WpfApp.TempDirectory, directoryInfo);

            WpfApp.TempDirectory = directoryInfo;
        }

        Directory.CreateDirectory(WpfApp.TempDirectory.FullName);

        TaskHelper.RunSync(UpdateProjectsInstallerFilesIfNeeded);
    }

    public IServiceProvider ServiceProvider { get; }

    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        logger.LogInformation("App initializing");

        // Hook on errors before the app really starts.
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        Current.DispatcherUnhandledException += ApplicationOnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services
            .AddOptions<ApplicationOptions>()
            .Bind(configuration!.GetRequiredSection(BasicApplicationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();
        services.AddSingleton<INetworkShellService, NetworkShellService>();
        services.AddSingleton<IWindowsFirewallService, WindowsFirewallService>();
        services.AddSingleton<IInstalledAppsInstallerService, InstalledAppsInstallerService>();

        services.AddSingleton<IElasticSearchServerInstallerService, ElasticSearchServerInstallerService>();
        services.AddSingleton<IInternetInformationServerInstallerService, InternetInformationServerInstallerService>();
        services.AddSingleton<IPostgreSqlServerInstallerService, PostgreSqlServerInstallerService>();
        services.AddSingleton<IWindowsApplicationInstallerService, WindowsApplicationInstallerService>();
        services.AddSingleton<IAzureStorageAccountInstallerService, AzureStorageAccountInstallerService>();

        services.AddSingleton<ICheckForUpdatesBoxDialogViewModel, CheckForUpdatesBoxDialogViewModel>();
        services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private void CurrentDomainUnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e is not { ExceptionObject: Exception ex })
        {
            return;
        }

        var exceptionMessage = ex.GetMessage(includeInnerMessage: true);
        logger.LogError(ex, "CurrentDomain Unhandled Exception: {Message}", exceptionMessage);
        ErrorDialog.Show("CurrentDomain Unhandled Exception", exceptionMessage);
    }

    private void ApplicationOnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        var exceptionMessage = e.Exception.GetMessage(includeInnerMessage: true);
        if (exceptionMessage.Contains(
                "BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'ComboBoxItem'",
                StringComparison.Ordinal))
        {
            e.Handled = true;
            return;
        }

        logger.LogError(e.Exception, "Dispatcher Unhandled Exception: {Message}", exceptionMessage);
        ErrorDialog.Show("Dispatcher Unhandled Exception", exceptionMessage);

        e.Handled = true;
        Shutdown(-1);
    }

    private void TaskSchedulerUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        logger.LogError(e.Exception, "Unobserved Task Exception: {Message}", e.Exception.GetMessage(includeInnerMessage: true));
        e.SetObserved();
    }

    private async void ApplicationStartup(
        object sender,
        StartupEventArgs e)
    {
        logger.LogInformation("App starting");

        await host
            .StartAsync()
            .ConfigureAwait(false);

        var applicationOptions = new ApplicationOptions();
        configuration!
            .GetRequiredSection(BasicApplicationOptions.SectionName)
            .Bind(applicationOptions);

        CultureManager.SetCultures(applicationOptions.Language);

        ThemeManagerHelper.SetThemeAndAccent(Current, applicationOptions.Theme);

        Current.MainWindow = host
            .Services
            .GetService<MainWindow>()!;

        Current.MainWindow.Show();

        logger.LogInformation("App started");
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs e)
    {
        logger.LogInformation("App closing");

        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();
    }

    /// <summary>
    /// Creates the data folders. The installer normally creates the machine-wide one with write access for all users.
    /// </summary>
    /// <returns>The error when a folder could not be created; it is logged and the app starts anyway.</returns>
    private static Exception? EnsureDataFoldersAreCreated()
    {
        try
        {
            WpfApp.DataPaths.EnsureCreated();
            return null;
        }
        catch (IOException ex)
        {
            return ex;
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex;
        }
    }

    private static void MoveOldTempPathIfNeeded(
        DirectoryInfo tempSource,
        DirectoryInfo tempDestination)
    {
        if (tempSource.FullName == tempDestination.FullName ||
            !Directory.Exists(tempSource.FullName) ||
            (Directory.GetFiles(tempSource.FullName).Length == 0 &&
             Directory.GetDirectories(tempSource.FullName).Length == 0))
        {
            return;
        }

        tempDestination.Create();

        foreach (var file in tempSource.GetFiles())
        {
            file.MoveTo(
                Path.Combine(
                    tempDestination.FullName,
                    file.Name));
        }

        foreach (var dir in tempSource.GetDirectories())
        {
            var targetDir = new DirectoryInfo(
                Path.Combine(
                    tempDestination.FullName,
                    dir.Name));

            if (!targetDir.Exists)
            {
                targetDir.Create();
            }

            MoveOldTempPathIfNeeded(dir, targetDir);

            if (Directory.Exists(dir.FullName))
            {
                dir.Delete(true);
            }
        }

        if (Directory.Exists(tempSource.FullName))
        {
            tempSource.Delete(true);
        }
    }

    /// <summary>
    /// An MSI upgrade replaces <c>appsettings.custom.json</c> next to the exe; the settings dialog keeps a copy
    /// in the data folder, which is put back here.
    /// </summary>
    private static void RestoreInstallerCustomAppSettingsIfNeeded()
    {
        var currentFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, Constants.CustomAppSettingsFileName));
        var backupFile = WpfApp.DataPaths.CustomAppSettingsFile;
        if (!currentFile.Exists ||
            !backupFile.Exists ||
            currentFile.LastWriteTime == backupFile.LastWriteTime)
        {
            return;
        }

        File.Copy(backupFile.FullName, currentFile.FullName, overwrite: true);
    }

    private static async Task UpdateProjectsInstallerFilesIfNeeded()
    {
        if (!WpfApp.DataPaths.ProjectsDirectory.Exists)
        {
            return;
        }

        foreach (var projectDirectory in Directory.GetDirectories(WpfApp.DataPaths.ProjectsDirectory.FullName))
        {
            await ConfigurationFileHelper
                .UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(new DirectoryInfo(projectDirectory))
                .ConfigureAwait(true);
        }
    }

    private static Serilog.Core.Logger CreateLoggerConfigurationForSerilogFileLog()
    {
        var systemName = AssemblyHelper.GetSystemNameAsKebabCasing();
        var loggerConfig = new LoggerConfiguration();
        return loggerConfig
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("System", systemName)
            .Enrich.WithExceptionDetails()
            .WriteTo.File(
                path: Path.Combine(WpfApp.DataPaths.LogsDirectory.FullName, $"{systemName}-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                formatProvider: GlobalizationConstants.EnglishCultureInfo)
            .CreateLogger();
    }

    public static bool TryParsePath(
        string? path,
        out DirectoryInfo directoryInfo)
    {
        directoryInfo = new DirectoryInfo(Path.GetTempPath());

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        try
        {
            // This throws an exception if the path contains
            // invalid characters or is incorrectly formatted.
            directoryInfo = new DirectoryInfo(Path.GetFullPath(path));
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}