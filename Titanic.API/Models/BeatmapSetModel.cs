using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Titanic.API.Constants;

namespace Titanic.API.Models
{
    public class BeatmapSetModelCompact
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("creator_id")]
        public int? CreatorId { get; set; }

        [JsonProperty("topic_id")]
        public int? TopicId { get; set; }

        [JsonProperty("status")]
        public OnlineBeatmapStatus Status { get; set; }

        [JsonProperty("has_video")]
        public bool HasVideo { get; set; }

        [JsonProperty("has_storyboard")]
        public bool HasStoryboard { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("server")]
        public int Server { get; set; }

        [JsonProperty("download_server")]
        public int DownloadServer { get; set; }

        [JsonProperty("available")]
        public bool Available { get; set; }

        [JsonProperty("enhanced")]
        public bool Enhanced { get; set; }

        [JsonProperty("explicit")]
        public bool Explicit { get; set; }

        [JsonProperty("language_id")]
        public BeatmapLanguage Language { get; set; }

        [JsonProperty("genre_id")]
        public BeatmapGenre Genre { get; set; }

        [JsonProperty("display_title")]
        public string DisplayTitle { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("last_update")]
        public DateTime LastUpdate { get; set; }

        [JsonProperty("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [JsonProperty("approved_by")]
        public int? ApprovedBy { get; set; }

        [JsonProperty("rating_average")]
        public float RatingAverage { get; set; }

        [JsonProperty("rating_count")]
        public int RatingCount { get; set; }

        [JsonProperty("favourite_count")]
        public int FavouriteCount { get; set; }

        [JsonProperty("total_playcount")]
        public int TotalPlaycount { get; set; }

        [JsonProperty("max_diff")]
        public float MaxDiff { get; set; }

        [JsonProperty("osz_filesize")]
        public int OszFilesize { get; set; }

        [JsonProperty("osz_filesize_novideo")]
        public int OszFilesizeNovideo { get; set; }

        [JsonProperty("ratings")]
        public float Ratings { get; set; }

        [JsonProperty("favourites")]
        public float Favourites { get; set; }
    }

    public class BeatmapSetModel : BeatmapSetModelCompact
    {
        [JsonProperty("beatmaps")]
        public List<BeatmapModelCompact> Beatmaps { get; set; }
    }
}
