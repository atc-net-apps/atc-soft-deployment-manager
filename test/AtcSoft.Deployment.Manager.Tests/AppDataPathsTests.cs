namespace AtcSoft.Deployment.Manager.Tests;

public sealed class AppDataPathsTests
{
    [Fact]
    public void Resolve_WithoutOverride_UsesProgramDataAndLocalAppData()
    {
        // Act
        var actual = AppDataPaths.Resolve(overridePath: null);

        // Assert
        actual.IsOverridden.Should().BeFalse();
        actual.MachineRoot.FullName.Should().Be(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AtcSoft", "AtcSoft.Deployment.Manager"));
        actual.UserRoot.FullName.Should().Be(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtcSoft", "AtcSoft.Deployment.Manager"));
    }

    [Fact]
    public void Resolve_WithOverride_PutsTheUserDataInALocalSubfolder()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), "dm-data");

        // Act
        var actual = AppDataPaths.Resolve(root);

        // Assert
        actual.IsOverridden.Should().BeTrue();
        actual.MachineRoot.FullName.Should().Be(root);
        actual.UserRoot.FullName.Should().Be(Path.Combine(root, "local"));
        actual.ProjectsDirectory.FullName.Should().Be(Path.Combine(root, "Projects"));
        actual.LogsDirectory.FullName.Should().Be(Path.Combine(root, "Logs"));
        actual.CustomAppSettingsFile.FullName.Should().Be(Path.Combine(root, "appsettings.custom.json"));
    }

    [Fact]
    public void EnsureCreated_CreatesAllFolders()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory("dm-data-");
        var paths = AppDataPaths.Resolve(root.FullName);

        try
        {
            // Act
            paths.EnsureCreated();

            // Assert
            paths.ProjectsDirectory.Exists.Should().BeTrue();
            paths.LogsDirectory.Exists.Should().BeTrue();
            paths.UserRoot.Exists.Should().BeTrue();
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void LegacyRoot_IsTheFolderOfVersion1()
        => AppDataPaths.LegacyRoot.FullName.Should().Be(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ATC", "atc-installer"));
}