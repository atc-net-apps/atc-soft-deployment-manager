// ReSharper disable NotAccessedField.Local
// ReSharper disable AsyncVoidMethod
namespace AtcSoft.Deployment.Manager.Wpf.App;

public partial class App
{
    private readonly IHost host;
    private IConfiguration? configuration;

    public App()
    {
        RestoreInstallerCustomAppSettingsIfNeeded();

        host = Host.CreateDefaultBuilder()
            .ConfigureLogging((_, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddDebug();
                    logging.AddSerilog(CreateLoggerConfigurationForSerilogFileLog());
                })
            .ConfigureAppConfiguration(
                configurationBuilder =>
                {
                    configuration = configurationBuilder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile(Constants.CustomAppSettingsFileName, optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
                })
            .ConfigureServices((_, services) => ConfigureServices(services))
            .Build();

        ServiceProvider = host.Services;

        if (TryParsePath(
                host.Services.GetRequiredService<IConfiguration>()["Application:TempPath"],
                out var directoryInfo))
        {
            MoveOldTempPathIfNeeded(WpfApp.InstallerTempDirectory, directoryInfo);

            WpfApp.InstallerTempDirectory = directoryInfo;
        }

        EnsureInstallerDirectoriesIsCreated();

        TaskHelper.RunSync(UpdateProjectsInstallerFilesIfNeeded);
    }

    public IServiceProvider ServiceProvider { get; }

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

    private async void ApplicationStartup(
        object sender,
        StartupEventArgs e)
    {
        await host
            .StartAsync()
            .ConfigureAwait(false);

        var applicationOptions = new ApplicationOptions();
        configuration!
            .GetRequiredSection(BasicApplicationOptions.SectionName)
            .Bind(applicationOptions);

        CultureManager.SetCultures(applicationOptions.Language);

        ThemeManagerHelper.SetThemeAndAccent(Current, applicationOptions.Theme);

        var mainWindow = host
            .Services
            .GetService<MainWindow>()!;

        mainWindow.Show();
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs e)
    {
        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();
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

    private static void EnsureInstallerDirectoriesIsCreated()
    {
        if (!WpfApp.InstallerTempDirectory.Exists)
        {
            Directory.CreateDirectory(WpfApp.InstallerTempDirectory.FullName);
        }

        if (!WpfApp.InstallerProgramDataDirectory.Exists)
        {
            Directory.CreateDirectory(WpfApp.InstallerProgramDataDirectory.FullName);
        }

        if (!WpfApp.InstallerProgramDataProjectsDirectory.Exists)
        {
            Directory.CreateDirectory(WpfApp.InstallerProgramDataProjectsDirectory.FullName);
        }
    }

    private static void RestoreInstallerCustomAppSettingsIfNeeded()
    {
        var currentFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.CustomAppSettingsFileName));
        var backupFile = new FileInfo(Path.Combine(WpfApp.InstallerProgramDataDirectory.FullName, Constants.CustomAppSettingsFileName));
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
        foreach (var projectDirectory in Directory.GetDirectories(WpfApp.InstallerProgramDataProjectsDirectory.FullName))
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
                path: Path.Combine(WpfApp.InstallerProgramDataLogsDirectory.FullName, $"{systemName}-.log"),
                rollingInterval: RollingInterval.Day,
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