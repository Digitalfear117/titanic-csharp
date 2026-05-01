using System.Collections.Generic;
using Titanic.API.Models;

namespace Titanic.API.Requests
{
    public class BenchmarkScoresRequest : APIRequest<List<BenchmarkModel>>
    {
        private readonly int page;

        public BenchmarkScoresRequest(int page = 1)
        {
            this.page = page;
        }

        protected override List<BenchmarkModel> Execute(TitanicAPI api)
        {
            return api.GetList<BenchmarkModel>($"/benchmarks/?page={page}");
        }
    }
}
