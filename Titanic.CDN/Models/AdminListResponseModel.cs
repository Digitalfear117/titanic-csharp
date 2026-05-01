using Newtonsoft.Json;
using System.Collections.Generic;

namespace Titanic.CDN.Models;

public class AdminListResponseModel
{
    [JsonProperty("prefix")]
    public string Prefix { get; set; } = null!;

    [JsonProperty("items")]
    public List<AdminListItemModel> Items { get; set; } = null!;

    [JsonProperty("next_cursor")]
    public string NextCursor { get; set; } = null!;
}