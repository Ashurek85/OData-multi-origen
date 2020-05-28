using Core.Models.Filters;
using Core.Models.Order;
using System.Collections.Generic;
using System.Linq;

namespace Core.Models
{
    public class ODataExpression
    {

        public FilterBase InitialFilter { get; set; }

        public List<LogicalFilter> LogicalFilters { get; set; }

        public int? Skip { get; set; }
        public int? Top { get; set; }
        public bool Count { get; set; }

        public List<OrderBy> OrdersBy { get; set; }

        public ODataExpression()
        {
            LogicalFilters = new List<LogicalFilter>();
            OrdersBy = new List<OrderBy>();
        }

        public List<FilterBase> GetAllFilters()
        {
            List<FilterBase> allFilters = new List<FilterBase>();
            if (InitialFilter != null)
                allFilters.Add(InitialFilter);
            if (LogicalFilters != null && LogicalFilters.Any())
                LogicalFilters.ForEach(l => allFilters.Add(l.Filter));
            return allFilters;
        }
    }
}
