namespace AtcSoft.Deployment.Manager.ProjectFiles;

/// <summary>
/// How project files are written: property names as in the model, and no escaping of characters such as
/// <c>&lt;</c> or <c>&amp;</c>, so the files stay readable when edited by hand.
/// </summary>
public static class ProjectFileJson
{
    /// <summary>
    /// Gets new serializer options for project files.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions
    {
        get
        {
            var jsonSerializerOptions = JsonSerializerOptionsFactory.Create();
            jsonSerializerOptions.PropertyNamingPolicy = null;
            jsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            return jsonSerializerOptions;
        }
    }
}