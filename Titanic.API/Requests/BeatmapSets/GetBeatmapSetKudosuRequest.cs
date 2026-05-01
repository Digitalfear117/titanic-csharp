using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetBeatmapSetKudosuRequest : APIRequest<List<KudosuModel>>
    {
        public int SetId { get; set; }

        public GetBeatmapSetKudosuRequest(int setId)
        {
            SetId = setId;
        }

        protected override List<KudosuModel> Execute(TitanicAPI api)
        {
            return api.GetList<KudosuModel>($"/beatmapsets/{SetId}/kudosu");
        }
    }
}
