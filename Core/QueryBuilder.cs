using Core.Models;
using Core.Models.Filters;
using Core.Models.Order;
using Core.PropertiesMetadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Core
{
    public static class QueryBuilder<T>
        where T : class
    {

        public static Expression<Func<T, bool>> BuildFiltersExp(MetadataBase metadata, FilterBase initialFilter, List<LogicalFilter> logicalFilters)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (initialFilter == null)
                throw new ArgumentNullException(nameof(initialFilter));

            ParameterExpression paramExp = Expression.Parameter(typeof(T), "parameter");

            Expression exp = initialFilter.GetExpression(paramExp, metadata);
            if (logicalFilters != null)
            {
                foreach (LogicalFilter logicalFilter in logicalFilters)
                    exp = logicalFilter.GetExpression(exp, paramExp, metadata);
            }

            var predicate = Expression.Lambda<Func<T, bool>>(exp, paramExp);
            return predicate;
        }

        public static IEnumerable<TProperty> SelectPropertyValues<TProperty>(IQueryable<T> items, string propertyName)
        {
            ParameterExpression paramExp = Expression.Parameter(typeof(T), "parameter");
            Expression propExpression = Expression.Property(paramExp, propertyName);

            return items.Select(Expression.Lambda<Func<T, TProperty>>(propExpression, paramExp));
        }

        public static Expression<Func<T, bool>> BuildContainsExp<TProperty>(IEnumerable<TProperty> containsValues, string propertyName)
        {
            ParameterExpression paramExp = Expression.Parameter(typeof(T), "parameter");

            Expression propExpression = Expression.Property(paramExp, propertyName);

            MethodInfo methodInfo = typeof(List<TProperty>).GetMethod("Contains", new Type[] { typeof(TProperty) });
            Expression bodyExp = Expression.Call(Expression.Constant(containsValues), methodInfo, propExpression);
            return Expression.Lambda<Func<T, bool>>(bodyExp, paramExp);
        }

        public static Expression<Func<T, bool>> BuildExp<TProperty>(string propertyName, TProperty value)
        {
            ParameterExpression paramExp = Expression.Parameter(typeof(T), "parameter");
            Expression propExpression = Expression.Property(paramExp, propertyName);
            return Expression.Lambda<Func<T, bool>>(Expression.Equal(propExpression, Expression.Constant(value)), paramExp);
        }

        public static IQueryable<T> ApplySkipTake(IQueryable<T> source, int? take, int? skip)
        {
            if (skip.HasValue)
                source = source.Skip(skip.Value);
            if (take.HasValue)
                source = source.Take(take.Value);
            return source;
        }

        public static IQueryable<T> ApplyOrderBy(IQueryable<T> source, IEnumerable<OrderBy> ordersBy)
        {
            if (ordersBy.Any())
            {
                source = ApplyOrderBy(source, ordersBy.First());
                for (int i = 1; i < ordersBy.Count(); i++)
                    source = ApplyOrderByThen(source, ordersBy.ElementAt(i));
            }
            return source;
        }

        private static IQueryable<T> ApplyOrderBy(IQueryable<T> source, OrderBy orderBy)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "p");
            Expression orderByProperty = Expression.Property(parameter, orderBy.PropertyName);

            LambdaExpression lambda = Expression.Lambda(orderByProperty, new[] { parameter });

            MethodInfo orderByMethod;
            if (orderBy.OrderType == OrderType.Ascending)
            {
                orderByMethod = typeof(Queryable).GetMethods().First(method => method.Name == nameof(Queryable.OrderBy) &&
                                                                               method.GetParameters().Length == 2);
            }
            else if (orderBy.OrderType == OrderType.Descending)
            {
                orderByMethod = typeof(Queryable).GetMethods().First(method => method.Name == nameof(Queryable.OrderByDescending) &&
                                                                               method.GetParameters().Length == 2);
            }
            else
                throw new Exception($"Orderby {orderBy.OrderType} not supported");

            MethodInfo genericMethod = orderByMethod.MakeGenericMethod(new[] { typeof(T), orderByProperty.Type });
            return (IQueryable<T>)genericMethod.Invoke(null, new object[] { source, lambda });
        }

        private static IQueryable<T> ApplyOrderByThen(IQueryable<T> source, OrderBy orderBy)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "p");
            Expression orderByProperty = Expression.Property(parameter, orderBy.PropertyName);

            LambdaExpression lambda = Expression.Lambda(orderByProperty, new[] { parameter });

            MethodInfo orderByMethod;
            if (orderBy.OrderType == OrderType.Ascending)
            {
                orderByMethod = typeof(Queryable).GetMethods().First(method => method.Name == nameof(Queryable.ThenBy) &&
                                                                               method.GetParameters().Length == 2);
            }
            else if (orderBy.OrderType == OrderType.Descending)
            {
                orderByMethod = typeof(Queryable).GetMethods().First(method => method.Name == nameof(Queryable.ThenByDescending) &&
                                                                               method.GetParameters().Length == 2);
            }
            else
                throw new Exception($"Orderby {orderBy.OrderType} not supported");

            MethodInfo genericMethod = orderByMethod.MakeGenericMethod(new[] { typeof(T), orderByProperty.Type });
            return (IQueryable<T>)genericMethod.Invoke(null, new object[] { source, lambda });
        }

        public static IQueryable<T> ApplyFiltersByMetadata(IQueryable<T> source, ODataMetadata oDataMetadata, ODataExpression oDataExpression, MetadataBase metadata)
        {
            KeyValuePair<FilterBase, List<LogicalFilter>> guideFilters = oDataMetadata.GetFiltersByMetadata(oDataExpression, metadata);
            return guideFilters.Key != null ?
                        source.Where(BuildFiltersExp(metadata, guideFilters.Key, guideFilters.Value)) :
                        source;
        }

        public static List<TJoinProperty> ApplyFiltersAndSelect<TJoinProperty>(IQueryable<T> source, ODataMetadata oDataMetadata, ODataExpression oDataExpression, MetadataBase metadata, string propertyName)
        {
            IQueryable<T> queryWithFilters = ApplyFiltersByMetadata(source, oDataMetadata, oDataExpression, metadata);
            return SelectPropertyValues<TJoinProperty>(queryWithFilters, propertyName).ToList();
        }
    }
}
