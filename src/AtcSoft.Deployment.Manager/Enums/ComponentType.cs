// ReSharper disable CheckNamespace
namespace AtcSoft.Deployment.Manager;

public enum ComponentType
{
    None,

    [Description("Application")]
    Application,

    [Description("ElasticSearch")]
    ElasticSearchServer,

    [Description("IIS")]
    InternetInformationService,

    [Description("PostgreSql")]
    PostgreSqlServer,

    [Description("NT-Service")]
    WindowsService,
}