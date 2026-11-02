namespace AtcSoft.Deployment.Manager;

/// <summary>
/// Where the app keeps its data.
/// </summary>
/// <remarks>
/// Projects, logs and the app's custom settings are machine-wide (<c>%ProgramData%</c>), because the admins
/// who maintain a server share them, and in an elevated session <c>%LocalAppData%</c> can belong to another
/// admin. The installer creates that folder with write access for all users. Per-user items, such as the
/// recent files, are in <c>%LocalAppData%</c>.
/// </remarks>
public sealed class AppDataPaths
{
    /// <summary>
    /// The environment variable that moves all data folders, for example for tests.
    /// The machine data goes into the folder itself and the per-user data into its <c>local</c> subfolder.
    /// </summary>
    public const string DataPathVariable = "ATCSOFT_DEPLOYMENTMANAGER_DATA_PATH";

    public AppDataPaths(
        DirectoryInfo machineRoot,
        DirectoryInfo userRoot)
        : this(machineRoot, userRoot, isOverridden: false)
    {
    }

    private AppDataPaths(
        DirectoryInfo machineRoot,
        DirectoryInfo userRoot,
        bool isOverridden)
    {
        ArgumentNullException.ThrowIfNull(machineRoot);
        ArgumentNullException.ThrowIfNull(userRoot);

        MachineRoot = machineRoot;
        UserRoot = userRoot;
        IsOverridden = isOverridden;
    }

    /// <summary>
    /// The folder of version 1.x (Atc.Installer): <c>%ProgramData%\ATC\atc-installer</c>.
    /// </summary>
    public static DirectoryInfo LegacyRoot
        => new(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ATC",
                "atc-installer"));

    /// <summary>
    /// The default folder for downloaded and unpacked installation files, when the settings don't set one.
    /// </summary>
    public static DirectoryInfo DefaultTempDirectory
        => new(Path.Combine(Path.GetTempPath(), AppIdentity.AppId));

    /// <summary>
    /// Gets the machine-wide folder: <c>%ProgramData%\AtcSoft\AtcSoft.Deployment.Manager</c>.
    /// </summary>
    public DirectoryInfo MachineRoot { get; }

    /// <summary>
    /// Gets the per-user folder: <c>%LocalAppData%\AtcSoft\AtcSoft.Deployment.Manager</c>.
    /// </summary>
    public DirectoryInfo UserRoot { get; }

    /// <summary>
    /// Gets a value indicating whether the folders were set with <see cref="DataPathVariable"/>.
    /// </summary>
    public bool IsOverridden { get; }

    public DirectoryInfo ProjectsDirectory
        => new(Path.Combine(MachineRoot.FullName, "Projects"));

    public DirectoryInfo LogsDirectory
        => new(Path.Combine(MachineRoot.FullName, "Logs"));

    public FileInfo CustomAppSettingsFile
        => new(Path.Combine(MachineRoot.FullName, "appsettings.custom.json"));

    /// <summary>
    /// Gets the default folders.
    /// </summary>
    /// <returns>The paths.</returns>
    public static AppDataPaths ForCurrentUser()
        => new(
            new DirectoryInfo(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    AppIdentity.Vendor,
                    AppIdentity.AppId)),
            new DirectoryInfo(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppIdentity.Vendor,
                    AppIdentity.AppId)));

    /// <summary>
    /// Gets the folders set with <see cref="DataPathVariable"/>, or else the default folders.
    /// </summary>
    /// <returns>The paths.</returns>
    public static AppDataPaths FromEnvironment()
        => Resolve(Environment.GetEnvironmentVariable(DataPathVariable));

    /// <summary>
    /// Gets the folders under <paramref name="overridePath"/> when it is set, or else the default folders.
    /// </summary>
    /// <param name="overridePath">The folder to use instead of the default folders.</param>
    /// <returns>The paths.</returns>
    public static AppDataPaths Resolve(string? overridePath)
        => string.IsNullOrWhiteSpace(overridePath)
            ? ForCurrentUser()
            : new AppDataPaths(
                new DirectoryInfo(overridePath),
                new DirectoryInfo(Path.Combine(overridePath, "local")),
                isOverridden: true);

    /// <summary>
    /// Creates the folders when they are missing.
    /// </summary>
    public void EnsureCreated()
    {
        Directory.CreateDirectory(MachineRoot.FullName);
        Directory.CreateDirectory(ProjectsDirectory.FullName);
        Directory.CreateDirectory(LogsDirectory.FullName);
        Directory.CreateDirectory(UserRoot.FullName);
    }
}