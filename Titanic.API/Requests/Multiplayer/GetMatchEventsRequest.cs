using System;
using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class GetMatchEventsRequest : APIRequest<List<MultiplayerMatchEventModel>>
    {
        public int MatchId { get; set; }
        public DateTime? After { get; set; }

        public GetMatchEventsRequest(int matchId, DateTime? after = null)
        {
            MatchId = matchId;
            After = after;
        }

        protected override List<MultiplayerMatchEventModel> Execute(TitanicAPI api)
        {
            string endpoint = $"/multiplayer/{MatchId}/events";
            if (After.HasValue)
                endpoint += $"?after={After.Value:o}";
            return api.GetList<MultiplayerMatchEventModel>(endpoint);
        }
    }
}
