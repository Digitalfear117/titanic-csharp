using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetTitanicReleasesRequest : APIRequest<List<TitanicReleaseModel>>
    {
        protected override List<TitanicReleaseModel> Execute(TitanicAPI api)
        {
            return api.GetList<TitanicReleaseModel>("/releases/");
        }
    }
}
