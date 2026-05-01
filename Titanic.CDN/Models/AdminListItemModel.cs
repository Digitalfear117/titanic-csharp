using System;
using Newtonsoft.Json;

namespace Titanic.CDN.Models;

public class AdminListItemModel
{
    [JsonProperty("key")]
    public string Key { get; set; } = null!;

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("etag")]
    public string ETag { get; set; } = null!;

    [JsonProperty("last_modified")]
    public DateTime LastModified { get; set; }

    [JsonProperty("download_count")]
    public ulong? DownloadCount { get; set; }
}