using System;
using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserRankHistoryRequest : APIRequest<List<UserRankHistoryModel>>
    {
        public int UserId { get; set; }
        public int Mode { get; set; }
        public DateTime? Until { get; set; }

        public GetUserRankHistoryRequest(int userId, int mode, DateTime? until = null)
        {
            UserId = userId;
            Mode = mode;
            Until = until;
        }

        protected override List<UserRankHistoryModel> Execute(TitanicAPI api)
        {
            string endpoint = $"/users/{UserId}/history/rank/{Mode}";
            if (Until.HasValue)
                endpoint += $"?until={Until.Value:o}";
            return api.GetList<UserRankHistoryModel>(endpoint);
        }
    }
}
