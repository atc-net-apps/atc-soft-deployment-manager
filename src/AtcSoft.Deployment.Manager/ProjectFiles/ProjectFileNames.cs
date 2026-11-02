namespace AtcSoft.Deployment.Manager.ProjectFiles;

/// <summary>
/// The files in a project folder.
/// </summary>
public static class ProjectFileNames
{
    /// <summary>
    /// The settings that are local to this server: the template's values filled in or overridden.
    /// </summary>
    public const string CustomSettings = "CustomSettings.json";

    /// <summary>
    /// The shared settings, downloaded with the installation files. Secrets are blank in it.
    /// </summary>
    public const string TemplateSettings = "TemplateSettings.json";

    /// <summary>
    /// The settings the app uses: <see cref="TemplateSettings"/> with <see cref="CustomSettings"/> merged in.
    /// </summary>
    public const string InstallationSettings = "InstallationSettings.json";
}