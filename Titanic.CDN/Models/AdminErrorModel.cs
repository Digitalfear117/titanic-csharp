using Newtonsoft.Json;

namespace Titanic.CDN.Models;

public class AdminErrorModel
{
    [JsonProperty("error")]
    public string Error { get; set; } = null!;

    [JsonProperty("message")]
    public string Message { get; set; } = null!;
}
