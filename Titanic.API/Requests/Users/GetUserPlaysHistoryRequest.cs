using System;
using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserPlaysHistoryRequest : APIRequest<List<UserPlaysHistoryModel>>
    {
        public int UserId { get; set; }
        public int Mode { get; set; }
        public DateTime? Until { get; set; }

        public GetUserPlaysHistoryRequest(int userId, int mode, DateTime? until = null)
        {
            UserId = userId;
            Mode = mode;
            Until = until;
        }

        protected override List<UserPlaysHistoryModel> Execute(TitanicAPI api)
        {
            string endpoint = $"/users/{UserId}/history/plays/{Mode}";
            if (Until.HasValue)
                endpoint += $"?until={Until.Value:o}";
            return api.GetList<UserPlaysHistoryModel>(endpoint);
        }
    }
}
