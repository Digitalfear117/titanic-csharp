using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserAchievementsRequest : APIRequest<List<AchievementModel>>
    {
        public int UserId { get; set; }

        public GetUserAchievementsRequest(int userId)
        {
            UserId = userId;
        }

        protected override List<AchievementModel> Execute(TitanicAPI api)
        {
            return api.GetList<AchievementModel>($"/users/{UserId}/achievements");
        }
    }
}
