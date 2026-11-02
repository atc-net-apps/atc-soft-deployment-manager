namespace AtcSoft.Deployment.Manager.Wpf.Dialogs;

public partial class ApplicationSettingsDialog
{
    public ApplicationSettingsDialog(
        IApplicationSettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        ThemeManager.Current.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(
        object? sender,
        ThemeChangedEventArgs e)
    {
        ThemeManager.Current.ChangeTheme(this, e.NewTheme.Name);
    }
}