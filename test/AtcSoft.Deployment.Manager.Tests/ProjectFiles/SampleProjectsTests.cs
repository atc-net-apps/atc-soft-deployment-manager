namespace AtcSoft.Deployment.Manager.Tests.ProjectFiles;

/// <summary>
/// The sample project files in <c>sample-projects/</c> load with the current model, and nothing is lost when they
/// are saved again.
/// </summary>
public sealed class SampleProjectsTests
{
    public static TheoryData<string> SampleFiles
        => new(
            Directory
                .GetFiles(Path.Combine(AppContext.BaseDirectory, "TestData", "SampleProjects"), "*InstallationSettings.json", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)!);

    [Fact]
    public void SampleFiles_AreFound()
        => SampleFiles.Should().HaveCount(2);

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public async Task Load_ASample_ReadsItsComponents(string fileName)
    {
        // Act
        var actual = await ConfigurationFileHelper.LoadInstallationSettings(SampleFile(fileName));

        // Assert
        actual.Name.Should().NotBeNullOrEmpty();
        actual.Applications.Should().NotBeEmpty();
        actual.Applications.Should().OnlyContain(x => !string.IsNullOrEmpty(x.Name));
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void Deserialize_ASample_HasNoPropertiesTheModelDoesNotKnow(
        string fileName)
    {
        // Arrange
        var options = ProjectFileJson.SerializerOptions;
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        var json = File.ReadAllText(SampleFile(fileName).FullName);

        // Act
        var act = () => JsonSerializer.Deserialize<InstallationOption>(json, options);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public async Task SaveAndLoad_ASample_GivesTheSameSettings(string fileName)
    {
        // Arrange
        var original = await ConfigurationFileHelper.LoadInstallationSettings(SampleFile(fileName));
        var copy = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{fileName}"));

        try
        {
            // Act
            await ConfigurationFileHelper.SaveInstallationSettings(copy, original);
            var actual = await ConfigurationFileHelper.LoadInstallationSettings(copy);

            // Assert
            JsonSerializer.Serialize(actual, ProjectFileJson.SerializerOptions)
                .Should().Be(JsonSerializer.Serialize(original, ProjectFileJson.SerializerOptions));
        }
        finally
        {
            copy.Delete();
        }
    }

    private static FileInfo SampleFile(string fileName)
        => new(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "TestData", "SampleProjects"), fileName, SearchOption.AllDirectories).Single());
}