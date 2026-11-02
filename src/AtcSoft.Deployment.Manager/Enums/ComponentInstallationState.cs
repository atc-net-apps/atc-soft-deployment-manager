// ReSharper disable CheckNamespace
namespace AtcSoft.Deployment.Manager;

public enum ComponentInstallationState
{
    [Description("Unknown")]
    Unknown,

    [Description("Checking")]
    Checking,

    [Description("No installations files")]
    NoInstallationsFiles,

    [Description("Not installed")]
    NotInstalled,

    [Description("Installing")]
    Installing,

    [Description("Installed with old version")]
    InstalledWithOldVersion,

    [Description("Installed")]
    Installed,
}