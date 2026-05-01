using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserActivityRequest : APIRequest<List<UserActivityModel>>
    {
        public int UserId { get; set; }
        public int? Mode { get; set; }

        public GetUserActivityRequest(int userId, int? mode = null)
        {
            UserId = userId;
            Mode = mode;
        }

        protected override List<UserActivityModel> Execute(TitanicAPI api)
        {
            string endpoint = Mode.HasValue
                ? $"/users/{UserId}/activity/{Mode.Value}"
                : $"/users/{UserId}/activity";
            return api.GetList<UserActivityModel>(endpoint);
        }
    }
}
