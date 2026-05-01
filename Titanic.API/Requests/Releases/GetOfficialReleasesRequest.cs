using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetOfficialReleasesRequest : APIRequest<List<OsuReleaseModel>>
    {
        public int Offset { get; set; }
        public int Limit { get; set; }

        public GetOfficialReleasesRequest(int offset = 0, int limit = 10)
        {
            Offset = offset;
            Limit = limit;
        }

        protected override List<OsuReleaseModel> Execute(TitanicAPI api)
        {
            return api.GetList<OsuReleaseModel>($"/releases/official?offset={Offset}&limit={Limit}");
        }
    }
}
