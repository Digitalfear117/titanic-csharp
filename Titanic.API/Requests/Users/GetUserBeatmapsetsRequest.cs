using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserBeatmapsetsRequest : APIRequest<List<BeatmapSetModel>>
    {
        public int UserId { get; set; }

        public GetUserBeatmapsetsRequest(int userId)
        {
            UserId = userId;
        }

        protected override List<BeatmapSetModel> Execute(TitanicAPI api)
        {
            return api.GetList<BeatmapSetModel>($"/users/{UserId}/beatmapsets");
        }
    }
}
