namespace AtcSoft.Deployment.Manager.InstallationConfigurations;

public class RegistrySettingOption
{
    public string Key { get; set; } = string.Empty;

    public InsertRemoveType Action { get; set; }

    public override string ToString()
        => $"{nameof(Key)}: {Key}, {nameof(Action)}: {Action}";
}