namespace AtcSoft.Deployment.Manager.Wpf;

/// <summary>
/// App-wide values that the views and view models need.
/// </summary>
public static class WpfApp
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    public static readonly BitmapImage DefaultIcon = new(new Uri("pack://application:,,,/AtcSoft.Deployment.Manager.Wpf;component/Resources/AppIcon.ico", UriKind.Absolute));

    /// <summary>
    /// Gets the running version, which release-please sets in Directory.Build.props.
    /// </summary>
    public static Version CurrentVersion
        => (Assembly.GetEntryAssembly() ?? typeof(WpfApp).Assembly).GetName().Version ?? new Version(0, 0, 0);

    /// <summary>
    /// Gets the data folders (see <see cref="AppDataPaths"/>).
    /// </summary>
    public static AppDataPaths DataPaths { get; } = AppDataPaths.FromEnvironment();

    /// <summary>
    /// Gets or sets the folder for downloaded, unpacked and backed-up installation files.
    /// </summary>
    public static DirectoryInfo TempDirectory { get; set; } = AppDataPaths.DefaultTempDirectory;

    public static JsonSerializerOptions JsonSerializerOptions
        => ProjectFileJson.SerializerOptions;
}