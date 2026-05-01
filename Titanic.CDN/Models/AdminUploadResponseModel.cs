using Newtonsoft.Json;

namespace Titanic.CDN.Models;

public class AdminUploadResponseModel
{
    [JsonProperty("key")]
    public string Key { get; set; } = null!;

    [JsonProperty("etag")]
    public string ETag { get; set; } = null!;
}