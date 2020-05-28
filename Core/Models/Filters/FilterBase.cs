using Core.PropertiesMetadata;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.Models.Filters
{
    public abstract class FilterBase
    {
        public string PropertyName { get; set; }

        public string Value { get; set; }

        public abstract Expression GetExpression(ParameterExpression paramExp, MetadataBase metadata);

        protected Expression GetConstantExp(MetadataBase metadata)
        {
            PropertyInfo propInfo = metadata.GetProperty(PropertyName);
            if (propInfo == null)
                throw new Exception($"Property {PropertyName} not found in metadata");

            if (propInfo.PropertyType == typeof(string))
                return Expression.Constant(Value);
            else if (propInfo.PropertyType == typeof(int))
            {
                if (!int.TryParse(Value, out int intValue))
                    throw new Exception($"Unable to cast {Value} to int");
                return Expression.Constant(intValue);
            }
            else if (propInfo.PropertyType == typeof(float))
            {
                if (!float.TryParse(Value, out float floatValue))
                    throw new Exception($"Unable to cast {Value} to float");
                return Expression.Constant(floatValue);
            }
            else if (propInfo.PropertyType == typeof(double))
            {
                if (!double.TryParse(Value, out double doubleValue))
                    throw new Exception($"Unable to cast {Value} to double");
                return Expression.Constant(doubleValue);
            }
            else if (propInfo.PropertyType == typeof(decimal))
            {
                if (!decimal.TryParse(Value, out decimal decimalValue))
                    throw new Exception($"Unable to cast {Value} to decimal");
                return Expression.Constant(decimalValue);
            }
            else if (propInfo.PropertyType == typeof(int?))
            {
                if (Value == null)
                    return Expression.Constant(null, typeof(int?));
                if (!int.TryParse(Value, out int intValue))
                    throw new Exception($"Unable to cast {Value} to int");
                return Expression.Constant(new int?(intValue), typeof(int?));
            }
            else if (propInfo.PropertyType == typeof(float?))
            {
                if (Value == null)
                    return Expression.Constant(null, typeof(float?));
                if (!float.TryParse(Value, out float floatValue))
                    throw new Exception($"Unable to cast {Value} to float");
                return Expression.Constant(new float?(floatValue), typeof(float?));
            }
            else if (propInfo.PropertyType == typeof(double?))
            {
                if (Value == null)
                    return Expression.Constant(null, typeof(double?));
                if (!double.TryParse(Value, out double doubleValue))
                    throw new Exception($"Unable to cast {Value} to double");
                return Expression.Constant(new double?(doubleValue), typeof(double?));
            }
            else if (propInfo.PropertyType == typeof(decimal?))
            {
                if (Value == null)
                    return Expression.Constant(null, typeof(decimal?));
                if (!decimal.TryParse(Value, out decimal decimalValue))
                    throw new Exception($"Unable to cast {Value} to decimal");
                return Expression.Constant(new decimal?(decimalValue), typeof(decimal?));
            }
            throw new Exception($"GetConstantExp: type {propInfo.PropertyType.FullName} not supported");
        }
    }
}
