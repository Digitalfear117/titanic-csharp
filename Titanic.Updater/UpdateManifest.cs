namespace Titanic.Updater;

#nullable disable

public class UpdateManifest
{
    [JsonProperty("format_version")]
    public int FormatVersion { get; set; }

    [JsonProperty("client")]
    public string Client { get; set; }

    [JsonProperty("force_update")]
    public bool ForceUpdate { get; set; }

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

    [JsonProperty("source_url_patch")]
    public string SourceUrlPatch { get; set; }

    [JsonProperty("source_url_full")]
    public string SourceUrlFull { get; set; }

    [JsonProperty("destination")]
    public string Destination { get; set; }

    [JsonProperty("source_checksum")]
    public string SourceChecksum { get; set; }

    [JsonProperty("patch_checksum")]
    public string PatchChecksum { get; set; }

    [JsonProperty("checksum")]
    public string Checksum { get; set; }

    [JsonProperty("algorithm")]
    public string Algorithm { get; set; }
}
