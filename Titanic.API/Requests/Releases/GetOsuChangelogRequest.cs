using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetOsuChangelogRequest : APIRequest<List<OsuChangelogModel>>
    {
        public string Stream { get; set; }
        public int Limit { get; set; }

        public GetOsuChangelogRequest(string stream, int limit = 50)
        {
            Stream = stream;
            Limit = limit;
        }

        protected override List<OsuChangelogModel> Execute(TitanicAPI api)
        {
            return api.GetList<OsuChangelogModel>($"/releases/official/changelog?stream={Stream}&limit={Limit}");
        }
    }
}
