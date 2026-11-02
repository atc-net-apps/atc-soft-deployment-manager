namespace AtcSoft.Deployment.Manager;

public interface IGitHubReleaseService
{
    Task<Version?> GetLatestVersion();

    Task<Uri?> GetLatestMsiLink();

    Task<byte[]> DownloadFileByLink(Uri uri);
}