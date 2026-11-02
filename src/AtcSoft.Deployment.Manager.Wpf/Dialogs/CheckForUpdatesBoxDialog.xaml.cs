// ReSharper disable InvertIf
namespace AtcSoft.Deployment.Manager.Wpf.Dialogs;

public partial class CheckForUpdatesBoxDialog
{
    public CheckForUpdatesBoxDialog(
        ICheckForUpdatesBoxDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}