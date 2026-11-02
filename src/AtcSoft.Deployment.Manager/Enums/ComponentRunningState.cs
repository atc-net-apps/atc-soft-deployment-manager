// ReSharper disable CheckNamespace
namespace AtcSoft.Deployment.Manager;

public enum ComponentRunningState
{
    [Description("Unknown")]
    Unknown,

    [Description("Checking")]
    Checking,

    [Description("Not available")]
    NotAvailable,

    [Description("Stopped")]
    Stopped,

    [Description("Partially running")]
    PartiallyRunning,

    [Description("Running")]
    Running,
}