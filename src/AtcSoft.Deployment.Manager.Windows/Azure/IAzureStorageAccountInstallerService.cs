namespace AtcSoft.Deployment.Manager.Windows.Azure;

public interface IAzureStorageAccountInstallerService
{
    Task<IList<FileInfo>> DownloadLatestFilesByNames(
        string storageConnectionString,
        string blobContainerName,
        string downloadFolder,
        IReadOnlyList<(string ComponentName, string? ContentHash)> components);
}