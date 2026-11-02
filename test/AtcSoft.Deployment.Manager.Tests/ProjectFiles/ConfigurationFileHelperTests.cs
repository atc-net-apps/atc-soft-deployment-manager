namespace AtcSoft.Deployment.Manager.Tests.ProjectFiles;

/// <summary>
/// How CustomSettings.json is merged into TemplateSettings.json to give InstallationSettings.json.
/// These describe the behavior of version 1.x, which the migration guide starts from.
/// </summary>
public sealed class ConfigurationFileHelperTests : IDisposable
{
    private readonly DirectoryInfo projectFolder = Directory.CreateTempSubdirectory("AtcSoft.Deployment.Manager.Tests-");

    public void Dispose()
        => projectFolder.Delete(recursive: true);

    [Fact]
    public async Task Update_WithoutCustomOrTemplateSettings_DoesNothing()
    {
        // Arrange
        await Write(ProjectFileNames.TemplateSettings, Template());

        // Act
        var actual = await ConfigurationFileHelper.UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(projectFolder);

        // Assert
        actual.Should().BeFalse();
        File.Exists(Path.Combine(projectFolder.FullName, ProjectFileNames.InstallationSettings)).Should().BeFalse();
    }

    [Fact]
    public async Task Update_CustomAzureSettings_ReplaceTheTemplates()
    {
        // Arrange
        var custom = Custom();
        custom.Azure = new AzureOptions { StorageConnectionString = "local-connection", BlobContainerName = "local-container" };
        await WriteBoth(Template(), custom);

        // Act
        var actual = await Merge();

        // Assert
        actual.Azure.StorageConnectionString.Should().Be("local-connection");
        actual.Azure.BlobContainerName.Should().Be("local-container");
    }

    [Fact]
    public async Task Update_CustomDefaultSettings_OverwriteOnlyKeysTheTemplateHas()
    {
        // Arrange
        var template = Template();
        template.DefaultApplicationSettings["HostName"] = "template-host";
        var custom = Custom();
        custom.DefaultApplicationSettings["HostName"] = "local-host";
        custom.DefaultApplicationSettings["OnlyInCustom"] = "x";
        await WriteBoth(template, custom);

        // Act
        var actual = await Merge();

        // Assert
        actual.DefaultApplicationSettings["HostName"].ToString().Should().Be("local-host");
        actual.DefaultApplicationSettings.Should().NotContainKey("OnlyInCustom");
    }

    [Fact]
    public async Task Update_CustomApplicationSettings_AreAddedOnlyWhenTheTemplateLacksThem()
    {
        // Arrange
        var template = Template();
        template.Applications[0].ApplicationSettings["HttpPort"] = 8080;
        var custom = Custom();
        custom.Applications[0].ApplicationSettings["HttpPort"] = 9090;
        custom.Applications[0].ApplicationSettings["Password"] = "secret";
        await WriteBoth(template, custom);

        // Act
        var actual = await Merge();

        // Assert
        var settings = actual.Applications.Single().ApplicationSettings;
        settings["HttpPort"].ToString().Should().Be("8080");
        settings["Password"].ToString().Should().Be("secret");
    }

    [Fact]
    public async Task Update_AnApplicationOnlyInCustom_IsNotAdded()
    {
        // Arrange
        var custom = Custom();
        custom.Applications.Add(new ApplicationOption { Name = "OnlyInCustom" });
        await WriteBoth(Template(), custom);

        // Act
        var actual = await Merge();

        // Assert
        actual.Applications.Select(x => x.Name).Should().Equal("Api");
    }

    [Fact]
    public async Task Update_ACustomXmlSettingWithTwoNewValues_IsAddedOnce()
    {
        // Arrange
        var template = Template();
        template.Applications[0].ConfigurationSettingsFiles.Add(new ConfigurationSettingsFileOption { FileName = "web.config" });
        var custom = Custom();
        var file = new ConfigurationSettingsFileOption { FileName = "web.config" };
        file.XmlSettings.Add(XmlSetting(("key", "LogLevel"), ("value", "Debug")));
        custom.Applications[0].ConfigurationSettingsFiles.Add(file);
        await WriteBoth(template, custom);

        // Act
        var actual = await Merge();

        // Assert
        actual.Applications.Single().ConfigurationSettingsFiles.Single().XmlSettings.Should().ContainSingle();
    }

    [Fact]
    public async Task Update_ACustomXmlSettingTheTemplateHas_IsNotAddedAgain()
    {
        // Arrange
        var template = Template();
        var templateFile = new ConfigurationSettingsFileOption { FileName = "web.config" };
        templateFile.XmlSettings.Add(XmlSetting(("key", "LogLevel"), ("value", "Info")));
        template.Applications[0].ConfigurationSettingsFiles.Add(templateFile);
        var custom = Custom();
        var customFile = new ConfigurationSettingsFileOption { FileName = "web.config" };
        customFile.XmlSettings.Add(XmlSetting(("key", "LogLevel"), ("value", "Info")));
        custom.Applications[0].ConfigurationSettingsFiles.Add(customFile);
        await WriteBoth(template, custom);

        // Act
        var actual = await Merge();

        // Assert
        actual.Applications.Single().ConfigurationSettingsFiles.Single().XmlSettings.Should().ContainSingle();
    }

    [Fact]
    public async Task Update_ACustomXmlSettingWhoseValuesAreAllInOtherTemplateSettings_IsNotAdded()
    {
        // Arrange: matching is by attribute value anywhere in the file, not by key
        var template = Template();
        var templateFile = new ConfigurationSettingsFileOption { FileName = "web.config" };
        templateFile.XmlSettings.Add(XmlSetting(("key", "Theme"), ("value", "Dark")));
        templateFile.XmlSettings.Add(XmlSetting(("key", "Dark"), ("value", "Theme")));
        template.Applications[0].ConfigurationSettingsFiles.Add(templateFile);
        var custom = Custom();
        var customFile = new ConfigurationSettingsFileOption { FileName = "web.config" };
        customFile.XmlSettings.Add(XmlSetting(("key", "Dark"), ("value", "Dark")));
        custom.Applications[0].ConfigurationSettingsFiles.Add(customFile);
        await WriteBoth(template, custom);

        // Act
        var actual = await Merge();

        // Assert
        actual.Applications.Single().ConfigurationSettingsFiles.Single().XmlSettings.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_Twice_ChangesNothingTheSecondTime()
    {
        // Arrange
        await WriteBoth(Template(), Custom());
        await ConfigurationFileHelper.UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(projectFolder);

        // Act
        var actual = await ConfigurationFileHelper.UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(projectFolder);

        // Assert
        actual.Should().BeFalse();
    }

    private static InstallationOption Template()
        => new()
        {
            Name = "Sample",
            Applications = { new ApplicationOption { Name = "Api", ComponentType = ComponentType.InternetInformationService } },
        };

    private static InstallationOption Custom()
        => new()
        {
            Name = "Sample",
            Applications = { new ApplicationOption { Name = "Api" } },
        };

    private static XmlElementSettingsOptions XmlSetting(
        params (string Name, string Value)[] attributes)
    {
        var setting = new XmlElementSettingsOptions { Path = "configuration:appSettings", Element = "add" };
        foreach (var (name, value) in attributes)
        {
            setting.Attributes[name] = value;
        }

        return setting;
    }

    private async Task WriteBoth(
        InstallationOption template,
        InstallationOption custom)
    {
        await Write(ProjectFileNames.TemplateSettings, template);
        await Write(ProjectFileNames.CustomSettings, custom);
    }

    private Task Write(
        string fileName,
        InstallationOption settings)
        => ConfigurationFileHelper.SaveInstallationSettings(
            new FileInfo(Path.Combine(projectFolder.FullName, fileName)),
            settings);

    private async Task<InstallationOption> Merge()
    {
        var updated = await ConfigurationFileHelper.UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(projectFolder);
        updated.Should().BeTrue();

        return await ConfigurationFileHelper.LoadInstallationSettings(
            new FileInfo(Path.Combine(projectFolder.FullName, ProjectFileNames.InstallationSettings)));
    }
}