using Core.Models.Operators;
using Core.PropertiesMetadata;
using System;
using System.Linq.Expressions;

namespace Core.Models.Filters
{
    public class ComparisonFilter : FilterBase
    {
        public ComparisonOperator Operator { get; set; }

        public override Expression GetExpression(ParameterExpression paramExp, MetadataBase metadata)
        {
            Expression left = Expression.Property(paramExp, PropertyName);
            Expression right = GetConstantExp(metadata);

            switch (Operator)
            {
                case ComparisonOperator.Equal:
                    return Expression.Equal(left, right);
                case ComparisonOperator.NotEqual:
                    return Expression.NotEqual(left, right);
                case ComparisonOperator.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case ComparisonOperator.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case ComparisonOperator.LessThan:
                    return Expression.LessThan(left, right);
                case ComparisonOperator.LessThanOrEqual:
                    return Expression.LessThanOrEqual(left, right);
            }
            throw new Exception($"Operator {Operator} not supported");
        }
    }
}
