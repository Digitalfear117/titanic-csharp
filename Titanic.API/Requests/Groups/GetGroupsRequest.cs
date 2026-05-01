using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetGroupsRequest : APIRequest<List<GroupModel>>
    {
        protected override List<GroupModel> Execute(TitanicAPI api)
        {
            return api.GetList<GroupModel>("/groups/");
        }
    }
}
