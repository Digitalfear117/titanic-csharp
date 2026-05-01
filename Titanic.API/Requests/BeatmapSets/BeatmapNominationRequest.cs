using System;
using System.Collections.Generic;
using System.Text;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class BeatmapNominationRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int SetId { get; set; }

        public BeatmapNominationRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.PostList<BeatmapNominationModel>($"/beatmapsets/{SetId}/nominations", null);
        }
    }
}
