namespace AtcSoft.Deployment.Manager.Wpf;

/// <summary>
/// App-wide values that the views and view models need. They used to be statics on the App class,
/// which now lives in the exe project and can't be referenced from here.
/// </summary>
public static class WpfApp
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    public static readonly BitmapImage DefaultIcon = new(new Uri("pack://application:,,,/AtcSoft.Deployment.Manager.Wpf;component/Resources/AppIcon.ico", UriKind.Absolute));

    public static DirectoryInfo InstallerTempDirectory { get; set; } = new(Path.Combine(Path.GetTempPath(), "atc-installer"));

    public static DirectoryInfo InstallerProgramDataDirectory => new(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ATC"), "atc-installer"));

    public static DirectoryInfo InstallerProgramDataLogsDirectory => new(Path.Combine(InstallerProgramDataDirectory.FullName, "Logs"));

    public static DirectoryInfo InstallerProgramDataProjectsDirectory => new(Path.Combine(InstallerProgramDataDirectory.FullName, "Projects"));

    public static JsonSerializerOptions JsonSerializerOptions
    {
        get
        {
            var jsonSerializerOptions = JsonSerializerOptionsFactory.Create();
            jsonSerializerOptions.PropertyNamingPolicy = null;
            jsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            return jsonSerializerOptions;
        }
    }
}
