namespace AtcSoft.Deployment.Manager.Windows;

public interface IInstalledAppsInstallerService : IInstallerService
{
    bool IsMicrosoftDotNetFramework48();

    bool IsMicrosoftDotNet7();

    bool IsMicrosoftDotNet8();

    Task<bool> IsNodeJs18();

    bool IsJavaRuntime8();

    bool IsAppInstalledByDisplayName(
        string appDisplayName);

    Version? GetAppInstalledVersionByDisplayName(
        string appDisplayName);
}