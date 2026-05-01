using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserMostPlayedRequest : APIRequest<List<UserBeatmapPlaysModel>>
    {
        public int UserId { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }

        public GetUserMostPlayedRequest(int userId, int offset = 0, int limit = 15)
        {
            UserId = userId;
            Offset = offset;
            Limit = limit;
        }

        protected override List<UserBeatmapPlaysModel> Execute(TitanicAPI api)
        {
            return api.GetList<UserBeatmapPlaysModel>($"/users/{UserId}/plays?offset={Offset}&limit={Limit}");
        }
    }
}
