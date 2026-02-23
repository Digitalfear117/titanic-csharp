using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Titanic.API.Constants;

namespace Titanic.API.Models
{
    public class BeatmapModelCompact
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("set_id")]
        public int SetId { get; set; }

        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("md5")]
        public string MD5 { get; set; }

        [JsonProperty("status")]
        public OnlineBeatmapStatus Status { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("diff")]
        public float Diff { get; set; }

        [JsonProperty("ar")] 
        public float AR { get; set; }

        [JsonProperty("cs")] 
        public float CS { get; set; }

        [JsonProperty("od")] 
        public float OD { get; set; }

        [JsonProperty("hp")] 
        public float HP { get; set; }
    }

    public class BeatmapModel : BeatmapModelCompact
    {
        [JsonProperty("beatmapset")]
        public BeatmapSetModelCompact BeatmapSet { get; set; }
    }
}
