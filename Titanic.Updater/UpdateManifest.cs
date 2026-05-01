namespace Titanic.Updater;

#nullable disable

public class UpdateManifest
{
    [JsonProperty("format_version")]
    public int FormatVersion { get; set; }

    [JsonProperty("client")]
    public string Client { get; set; }

    [JsonProperty("from")]
    public UpdateManifestEndpoint From { get; set; }

    [JsonProperty("to")]
    public UpdateManifestEndpoint To { get; set; }

    [JsonProperty("actions")]
    public List<UpdateAction> Actions { get; set; }
}

public class UpdateManifestEndpoint
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("executable_checksum")]
    public string ExecutableChecksum { get; set; }
}

public class UpdateAction
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("source")]
    public string Source { get; set; }

    [JsonProperty("destination")]
    public string Destination { get; set; }

    [JsonProperty("source_checksum")]
    public string SourceChecksum { get; set; }

    [JsonProperty("patch_checksum")]
    public string PatchChecksum { get; set; }

    [JsonProperty("result_checksum")]
    public string ResultChecksum { get; set; }

    [JsonProperty("checksum")]
    public string Checksum { get; set; }

    [JsonProperty("algorithm")]
    public string Algorithm { get; set; }
}
