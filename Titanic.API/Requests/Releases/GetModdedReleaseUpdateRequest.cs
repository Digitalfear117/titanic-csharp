using System;
using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetModdedReleaseUpdateRequest : APIRequest<ModdedReleaseUpdateModel>
    {
        public string Identifier { get; set; }
        public string Checksum { get; set; }
        public string Version { get; set; }
        public string Stream { get; set; }

        public GetModdedReleaseUpdateRequest(string identifier, string checksum = null, string version = null, string stream = null)
        {
            if (string.IsNullOrEmpty(checksum) && string.IsNullOrEmpty(version) && string.IsNullOrEmpty(stream))
                throw new ArgumentException("At least one of checksum, version, or stream must be provided.");

            Identifier = identifier;
            Checksum = checksum;
            Version = version;
            Stream = stream;
        }

        protected override ModdedReleaseUpdateModel Execute(TitanicAPI api)
        {
            List<string> query = new List<string>();

            if (!string.IsNullOrEmpty(Checksum))
                query.Add($"checksum={Uri.EscapeDataString(Checksum)}");

            if (!string.IsNullOrEmpty(Version))
                query.Add($"version={Uri.EscapeDataString(Version)}");

            if (!string.IsNullOrEmpty(Stream))
                query.Add($"stream={Uri.EscapeDataString(Stream)}");

            return api.Get<ModdedReleaseUpdateModel>($"/releases/modded/{Identifier}/update?{string.Join("&", query.ToArray())}");
        }
    }
}
