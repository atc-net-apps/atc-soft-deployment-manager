// ReSharper disable CheckNamespace
namespace AtcSoft.Deployment.Manager;

public enum ComponentEndpointType
{
    [Description("Unknown")]
    Unknown,

    [Description("BrowserLink")]
    BrowserLink,

    [Description("ReportingAssemblyInfo")]
    ReportingAssemblyInfo,

    [Description("ReportingHealthCheck")]
    ReportingHealthCheck,
}