using Core.Models.Operators;
using Core.PropertiesMetadata;
using System.Linq.Expressions;

namespace Core.Models.Filters
{
    public class LogicalFilter
    {
        public LogicalOperator Operator { get; set; }
        public FilterBase Filter { get; set; }

        public Expression GetExpression(Expression leftExp, ParameterExpression paramExp, MetadataBase metadata)
        {
            Expression rightExp = Filter.GetExpression(paramExp, metadata);

            switch (Operator)
            {
                case LogicalOperator.And:
                    return Expression.AndAlso(leftExp, rightExp);
                case LogicalOperator.Or:
                    return Expression.OrElse(leftExp, rightExp);
            }
            return null;
        }
    }
}
