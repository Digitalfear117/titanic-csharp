using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class NominateBeatmapSetRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int SetId { get; set; }

        public NominateBeatmapSetRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.PostList<BeatmapNominationModel>($"/beatmapsets/{SetId}/nominations", null);
        }
    }
}
