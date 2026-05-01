using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetCountryRankingsRequest : APIRequest<List<CountryEntryModel>>
    {
        public int Mode { get; set; }

        public GetCountryRankingsRequest(int mode)
        {
            Mode = mode;
        }

        protected override List<CountryEntryModel> Execute(TitanicAPI api)
        {
            return api.GetList<CountryEntryModel>($"/rankings/country/{Mode}");
        }
    }
}
