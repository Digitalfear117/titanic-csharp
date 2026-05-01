using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class SearchBeatmapsetsRequest : APIRequest<List<BeatmapSetModel>>
    {
        public BeatmapSearchRequest SearchParams { get; set; }

        public SearchBeatmapsetsRequest(BeatmapSearchRequest searchParams)
        {
            SearchParams = searchParams;
        }

        protected override List<BeatmapSetModel> Execute(TitanicAPI api)
        {
            return api.PostList<BeatmapSetModel>("/beatmapsets/search", SearchParams);
        }
    }
}
