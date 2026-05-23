using System;
using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetModdedReleaseEntriesRequest : APIRequest<List<ModdedReleaseEntryModel>>
    {
        public string Identifier { get; set; }
        public string Version { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }

        public GetModdedReleaseEntriesRequest(string identifier, int offset = 0, int limit = 10, string version = null)
        {
            Identifier = identifier;
            Version = version;
            Offset = offset;
            Limit = limit;
        }

        protected override List<ModdedReleaseEntryModel> Execute(TitanicAPI api)
        {
            List<string> query = new List<string>
            {
                $"offset={Offset}",
                $"limit={Limit}"
            };

            if (!string.IsNullOrEmpty(Version))
                query.Add($"version={Uri.EscapeDataString(Version)}");

            return api.GetList<ModdedReleaseEntryModel>($"/releases/modded/{Identifier}/entries?{string.Join("&", query.ToArray())}");
        }
    }
}
