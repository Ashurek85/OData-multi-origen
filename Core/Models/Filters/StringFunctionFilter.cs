using Core.Models.Functions;
using Core.PropertiesMetadata;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.Models.Filters
{
    public class StringFunctionFilter : FilterBase
    {
        public StringFunction Function { get; set; }

        public override Expression GetExpression(ParameterExpression paramExp, MetadataBase metadata)
        {
            Expression expProperty = Expression.Property(paramExp, PropertyName);
            Expression expConstant = Expression.Constant(Value, typeof(string));
            switch (Function)
            {
                case StringFunction.Contains:
                    MethodInfo containsMethod = typeof(string).GetMethod(nameof(string.Contains), new Type[] { typeof(string) });
                    return Expression.Call(expProperty, containsMethod, expConstant);
                case StringFunction.StartsWith:
                    MethodInfo startsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), new Type[] { typeof(string) });
                    return Expression.Call(expProperty, startsWithMethod, expConstant);
                case StringFunction.EndsWith:
                    MethodInfo endsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), new Type[] { typeof(string) });
                    return Expression.Call(expProperty, endsWithMethod, expConstant);
            }
            throw new Exception($"Function {Function} not supported");
        }
    }
}
