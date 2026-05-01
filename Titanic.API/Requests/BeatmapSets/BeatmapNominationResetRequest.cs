using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class BeatmapNominationResetRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int SetId { get; }

        public BeatmapNominationResetRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.DeleteList<BeatmapNominationModel>($"/beatmapsets/{SetId}/nominations");
        }
    }
}
