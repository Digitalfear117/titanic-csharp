using System;
using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserReplayHistoryRequest : APIRequest<List<UserReplayHistoryModel>>
    {
        public int UserId { get; set; }
        public int Mode { get; set; }
        public DateTime? Until { get; set; }

        public GetUserReplayHistoryRequest(int userId, int mode, DateTime? until = null)
        {
            UserId = userId;
            Mode = mode;
            Until = until;
        }

        protected override List<UserReplayHistoryModel> Execute(TitanicAPI api)
        {
            string endpoint = $"/users/{UserId}/history/views/{Mode}";
            if (Until.HasValue)
                endpoint += $"?until={Until.Value:o}";
            return api.GetList<UserReplayHistoryModel>(endpoint);
        }
    }
}
