using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetModdedReleaseEntriesRequest : APIRequest<List<ModdedReleaseEntryModel>>
    {
        public string Identifier { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }

        public GetModdedReleaseEntriesRequest(string identifier, int offset = 0, int limit = 10)
        {
            Identifier = identifier;
            Offset = offset;
            Limit = limit;
        }

        protected override List<ModdedReleaseEntryModel> Execute(TitanicAPI api)
        {
            return api.GetList<ModdedReleaseEntryModel>($"/releases/modded/{Identifier}/entries?offset={Offset}&limit={Limit}");
        }
    }
}
