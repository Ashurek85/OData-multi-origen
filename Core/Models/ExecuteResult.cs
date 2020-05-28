using System.Collections.Generic;


namespace Core.Models
{
    public class ExecuteResult<Guide, Complementary>
        where Guide : class
        where Complementary : class
    {
        public List<Guide> GuideItems { get; set; }

        public List<Complementary> ComplementaryItems { get; set; }

        public int? TotalCount { get; set; }
    }
}
