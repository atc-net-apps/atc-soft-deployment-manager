// ReSharper disable SuggestBaseTypeForParameterInConstructor
namespace AtcSoft.Deployment.Manager.Wpf.ComponentProvider.Models;

public class ReportingFile
{
    public ReportingFile(
        FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        FullName = fileInfo.FullName;
    }

    public string FullName { get; }

    public string? Version { get; set; }

    public string? TargetFramework { get; set; }

    public bool IsDebugBuild { get; set; }
}