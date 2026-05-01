using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetBeatmapSetNominationsRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int SetId { get; set; }

        public GetBeatmapSetNominationsRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.GetList<BeatmapNominationModel>($"/beatmapsets/{SetId}/nominations");
        }
    }
}
