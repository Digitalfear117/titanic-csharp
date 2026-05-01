using System;
using System.Collections.Generic;
using Titanic.CDN.Models;

namespace Titanic.CDN.Requests
{
    public class ListFilesRequest : CDNRequest<AdminListResponseModel>
    {
        public string Prefix { get; set; }
        public int? Limit { get; set; }
        public string? Cursor { get; set; }

        public ListFilesRequest(string prefix, int? limit = null, string? cursor = null)
        {
            if (prefix == null)
                throw new ArgumentNullException(nameof(prefix));

            if (limit.HasValue && (limit.Value < 1 || limit.Value > 1000))
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");

            Prefix = prefix;
            Limit = limit;
            Cursor = cursor;
        }

        protected override AdminListResponseModel Execute(TitanicCDN cdn)
        {
            cdn.EnsureAccessKey();

            List<string> query = new List<string>
            {
                $"prefix={TitanicCDN.EscapeQueryParameter(Prefix)}"
            };

            if (Limit.HasValue)
                query.Add($"limit={Limit.Value}");

            if (!string.IsNullOrEmpty(Cursor))
                query.Add($"cursor={TitanicCDN.EscapeQueryParameter(Cursor!)}");

            return cdn.Get<AdminListResponseModel>($"/admin/files?{string.Join("&", query.ToArray())}");
        }
    }
}
