using Newtonsoft.Json;
using System.Collections.Generic;

namespace Titanic.CDN.Models;

public class AdminSessionModel
{
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("prefixes")]
    public List<string> Prefixes { get; set; } = null!;

    [JsonProperty("permissions")]
    public List<string> Permissions { get; set; } = null!;

    [JsonProperty("upload_mode")]
    public string UploadMode { get; set; } = null!;

    [JsonProperty("track_downloads")]
    public bool TrackDownloads { get; set; }
}