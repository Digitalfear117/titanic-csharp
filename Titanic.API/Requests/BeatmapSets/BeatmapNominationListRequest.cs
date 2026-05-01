using System;
using System.Collections.Generic;
using System.Text;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class BeatmapNominationListRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int SetId { get; set; }

        public BeatmapNominationListRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.GetList<BeatmapNominationModel> ($"/beatmapsets/{SetId}/nominations");
        }
    }
}
