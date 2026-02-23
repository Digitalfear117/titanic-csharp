using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class BeatmapScoresRequest : APIRequest<ScoreCollectionResponseCompact>
    {
        public int BeatmapId { get; set; }

        public BeatmapScoresRequest(int beatmapId)
        {
            BeatmapId = beatmapId;
        }
        
        protected override ScoreCollectionResponseCompact Execute(TitanicAPI api)
        {
            return api.Get<ScoreCollectionResponseCompact>($"/beatmaps/{BeatmapId}/scores");
        }
    }
}
