namespace AtcSoft.Deployment.Manager.Wpf.Tests;

/// <summary>
/// Every view loads its XAML with the app's style dictionaries. A style key that Atc.Wpf renamed or removed,
/// or a wrong pack URI, fails here instead of when a user opens the view. No admin rights needed.
/// </summary>
public sealed class XamlLoadTests
{
    // The same dictionaries as App.xaml in the exe project.
    private static readonly string[] AppDictionaries =
    [
        "pack://application:,,,/Atc.Wpf.Theming;component/Styles/Default.xaml",
        "pack://application:,,,/Atc.Wpf.Controls;component/Styles/Controls.xaml",
        "pack://application:,,,/Atc.Wpf.Components;component/Styles/Controls.xaml",
    ];

    [Fact]
    public void EveryViewWithoutConstructorParameters_LoadsItsXaml()
    {
        // Arrange
        var viewTypes = typeof(WpfApp).Assembly
            .GetTypes()
            .Where(x => x is { IsAbstract: false, IsPublic: true } &&
                        typeof(FrameworkElement).IsAssignableFrom(x) &&
                        x.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
        var failures = new List<string>();
        var loaded = new List<string>();

        // Act
        RunOnStaThread(() =>
        {
            var application = Application.Current ?? new Application();
            foreach (var dictionary in AppDictionaries)
            {
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(dictionary, UriKind.Absolute) });
            }

            foreach (var viewType in viewTypes)
            {
                try
                {
                    _ = Activator.CreateInstance(viewType);
                    loaded.Add(viewType.Name);
                }
                catch (Exception ex)
                {
                    failures.Add($"{viewType.FullName}: {ex.GetBaseException().Message}");
                }
            }
        });

        // Assert
        failures.Should().BeEmpty();
        loaded.Should().Contain(["ComponentProviderPanelView", "ApplicationSettingsView", "FirewallRulesView", "MainWindowProjectContent"]);
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw new InvalidOperationException("The STA thread failed.", error);
        }
    }
}