using System.Collections.Generic;

namespace Core.Models
{
    public class QueryResponse<TResult>
        where TResult : class
    {
        public IEnumerable<TResult> Results { get; set; }
        public int? TotalCount { get; set; }
    }
}
