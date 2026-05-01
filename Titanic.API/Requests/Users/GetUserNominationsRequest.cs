using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserNominationsRequest : APIRequest<List<BeatmapNominationModel>>
    {
        public int UserId { get; set; }

        public GetUserNominationsRequest(int userId)
        {
            UserId = userId;
        }

        protected override List<BeatmapNominationModel> Execute(TitanicAPI api)
        {
            return api.GetList<BeatmapNominationModel>($"/users/{UserId}/nominations");
        }
    }
}
