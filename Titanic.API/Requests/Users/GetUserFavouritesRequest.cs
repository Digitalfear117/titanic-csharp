using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetUserFavouritesRequest : APIRequest<List<FavouriteModel>>
    {
        public int UserId { get; set; }

        public GetUserFavouritesRequest(int userId)
        {
            UserId = userId;
        }

        protected override List<FavouriteModel> Execute(TitanicAPI api)
        {
            return api.GetList<FavouriteModel>($"/users/{UserId}/favourites");
        }
    }
}
