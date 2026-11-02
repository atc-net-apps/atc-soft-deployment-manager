// ReSharper disable InvertIf
namespace AtcSoft.Deployment.Manager.Wpf.Dialogs;

/// <summary>
/// Interaction logic for CheckForUpdatesBoxDialog.
/// </summary>
public partial class CheckForUpdatesBoxDialog
{
    public CheckForUpdatesBoxDialog(
        ICheckForUpdatesBoxDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}