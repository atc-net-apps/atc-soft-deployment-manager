namespace AtcSoft.Deployment.Manager.Wpf.Dialogs;

/// <summary>
/// The "Check for updates" dialog: the running version, the latest release on GitHub, and a link to its installer.
/// </summary>
/// <remarks>
/// The installer is opened in the browser from github.com, not downloaded by the app: the app runs elevated,
/// and the browser shows where the file comes from.
/// </remarks>
public class CheckForUpdatesBoxDialogViewModel : ViewModelBase, ICheckForUpdatesBoxDialogViewModel, IDisposable
{
    private readonly IGitHubReleaseService gitHubReleaseService;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private GitHubRelease? latestRelease;
    private string latestVersion = string.Empty;
    private string latestLink = string.Empty;
    private bool hasNewVersion;

    public CheckForUpdatesBoxDialogViewModel(
        IGitHubReleaseService gitHubReleaseService)
    {
        this.gitHubReleaseService = gitHubReleaseService ?? throw new ArgumentNullException(nameof(gitHubReleaseService));

        CurrentVersion = WpfApp.CurrentVersion.ToString(3);
        LatestVersion = CurrentVersion;

        if (UpdateCheck.IsSkipped(Environment.GetEnvironmentVariable(UpdateCheck.SkipVariable)))
        {
            return;
        }

        // Check now and then every hour while the app runs.
        Task.Run(
            async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await RetrieveLatestFromGitHub(cancellationTokenSource.Token).ConfigureAwait(false);

                    await Task
                        .Delay(TimeSpan.FromHours(1), cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                }
            },
            cancellationTokenSource.Token);
    }

    public IRelayCommand DownloadLatestCommand
        => new RelayCommand(
            DownloadLatestCommandHandler,
            CanDownloadLatestCommandHandler);

    public static IRelayCommand<NiceDialogBox> CancelCommand
        => new RelayCommand<NiceDialogBox>(
            CancelCommandHandler);

    public string CurrentVersion { get; }

    public string LatestVersion
    {
        get => latestVersion;
        set
        {
            latestVersion = value;
            RaisePropertyChanged();
        }
    }

    public string LatestLink
    {
        get => latestLink;
        set
        {
            latestLink = value;
            RaisePropertyChanged();
        }
    }

    public bool HasNewVersion
    {
        get => hasNewVersion;
        set
        {
            hasNewVersion = value;
            RaisePropertyChanged();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    private async Task RetrieveLatestFromGitHub(
        CancellationToken cancellationToken)
    {
        var check = await gitHubReleaseService
            .CheckAsync(cancellationToken)
            .ConfigureAwait(false);

        if (check.Release is null)
        {
            return;
        }

        latestRelease = check.Release;
        LatestVersion = check.Release.Version.ToString(3);
        LatestLink = check.Release.DownloadUrl.AbsoluteUri;
        HasNewVersion = check.Release.IsNewerThan(WpfApp.CurrentVersion);
    }

    private bool CanDownloadLatestCommandHandler()
        => HasNewVersion && latestRelease is not null;

    private void DownloadLatestCommandHandler()
    {
        if (latestRelease is null)
        {
            return;
        }

        // Only github.com links get here (GitHubRelease.Parse checks them).
        Process.Start(new ProcessStartInfo(latestRelease.DownloadUrl.AbsoluteUri) { UseShellExecute = true });
    }

    private static void CancelCommandHandler(NiceDialogBox dialogBox)
        => dialogBox.Close();
}