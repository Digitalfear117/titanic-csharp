using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetBeatmapPacksRequest : APIRequest<List<BeatmapPackModel>>
    {
        public string Category { get; set; }

        public GetBeatmapPacksRequest(string category = null)
        {
            Category = category;
        }

        protected override List<BeatmapPackModel> Execute(TitanicAPI api)
        {
            string endpoint = string.IsNullOrEmpty(Category)
                ? "/beatmapsets/packs"
                : $"/beatmapsets/packs/{Category}";
            return api.GetList<BeatmapPackModel>(endpoint);
        }
    }
}
