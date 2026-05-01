using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class ResetBeatmapSetNominationsRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int SetId { get; set; }

        public ResetBeatmapSetNominationsRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.DeleteList<BeatmapNominationModel>($"/beatmapsets/{SetId}/nominations");
        }
    }
}
