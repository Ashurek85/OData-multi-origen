using Core.Models;
using Core.Models.Filters;
using Core.Models.Functions;
using Core.Models.Operators;
using Core.Models.Order;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public static class Extractor
    {
        public static ODataExpression Parse(IEnumerable<KeyValuePair<string, StringValues>> oDataQueryValues)
        {
            ODataExpression exp = new ODataExpression();
            foreach (KeyValuePair<string, StringValues> oDataQueryValue in oDataQueryValues)
            {
                switch (oDataQueryValue.Key)
                {
                    case "$filter":
                        KeyValuePair<FilterBase, List<LogicalFilter>> filters = ParseFilters(oDataQueryValue.Value);
                        exp.InitialFilter = filters.Key;
                        exp.LogicalFilters = filters.Value;
                        break;
                    case "$top":
                        int? top = ParseIntExp(oDataQueryValue.Value);
                        if (top.HasValue)
                            exp.Top = top.Value;
                        break;
                    case "$skip":
                        int? skip = ParseIntExp(oDataQueryValue.Value);
                        if (skip.HasValue)
                            exp.Skip = skip.Value;
                        break;
                    case "$count":
                        bool? count = ParseBoolExp(oDataQueryValue.Value);
                        if (count.HasValue)
                            exp.Count = count.Value;
                        break;
                    case "$orderby":
                        exp.OrdersBy = ParseOrderBy(oDataQueryValue.Value);
                        break;
                }
            }
            return exp;
        }

        #region Filters

        private static KeyValuePair<FilterBase, List<LogicalFilter>> ParseFilters(IEnumerable<string> filterValues)
        {
            FilterBase firstFilter = null;
            List<LogicalFilter> logicalFilters = new List<LogicalFilter>();

            if (filterValues != null && filterValues.Any())
            {
                // Solo se inspecciona el primer registro, es donde está la información
                string filterValue = filterValues.ElementAt(0);
                // Split con todos los filtros lógicos soportados
                IEnumerable<string> subFilters = filterValue.Split(new string[] { " and ", " or " }, System.StringSplitOptions.RemoveEmptyEntries);

                firstFilter = ParseFilter(subFilters.ElementAt(0));

                if (subFilters.Count() > 1)
                {
                    string restFilterValue = filterValue;
                    // Se determina el orden de aparición de los operadores logicos
                    for (int i = 1; i < subFilters.Count(); i++)
                    {
                        int andIndex = restFilterValue.IndexOf(" and ");
                        if (andIndex == -1)
                            andIndex = int.MaxValue;
                        int orIndex = restFilterValue.IndexOf(" or ");
                        if (orIndex == -1)
                            orIndex = int.MaxValue;

                        if (andIndex < orIndex)
                        {
                            // AND
                            logicalFilters.Add(new LogicalFilter()
                            {
                                Operator = LogicalOperator.And,
                                Filter = ParseFilter(subFilters.ElementAt(i))
                            });
                            restFilterValue = restFilterValue.Substring(andIndex + " and ".Length);
                        }
                        else
                        {
                            // OR
                            logicalFilters.Add(new LogicalFilter()
                            {
                                Operator = LogicalOperator.Or,
                                Filter = ParseFilter(subFilters.ElementAt(i))
                            });
                            restFilterValue = restFilterValue.Substring(orIndex + " or ".Length);
                        }
                    }
                }
            }

            return new KeyValuePair<FilterBase, List<LogicalFilter>>(firstFilter, logicalFilters);
        }

        private static FilterBase ParseFilter(string oDataFilter)
        {
            // ¿StringFunctionFilter?
            FilterBase filter = ParseStringFunctionFilter(oDataFilter);
            if (filter != null)
                return filter;

            // ¿ComparisonFilter?
            filter = ParseComparisonFilter(oDataFilter);

            if (filter == null)
                throw new Exception($"Unsupported filter: {oDataFilter}");

            return filter;
        }

        private static ComparisonFilter ParseComparisonFilter(string oDataFilter)
        {
            ComparisonFilter filter = TryParseComparisonFilter(oDataFilter, " eq ", ComparisonOperator.Equal);
            if (filter == null)
                filter = TryParseComparisonFilter(oDataFilter, " ne ", ComparisonOperator.NotEqual);
            if (filter == null)
                filter = TryParseComparisonFilter(oDataFilter, " gt ", ComparisonOperator.GreaterThan);
            if (filter == null)
                filter = TryParseComparisonFilter(oDataFilter, " ge ", ComparisonOperator.GreaterThanOrEqual);
            if (filter == null)
                filter = TryParseComparisonFilter(oDataFilter, " lt ", ComparisonOperator.LessThan);
            if (filter == null)
                filter = TryParseComparisonFilter(oDataFilter, " le ", ComparisonOperator.LessThanOrEqual);

            return filter;
        }

        private static ComparisonFilter TryParseComparisonFilter(string oDataFilter, string filterRepresentation, ComparisonOperator comparisonOperator)
        {
            string[] contentParts = oDataFilter.Split(filterRepresentation, StringSplitOptions.RemoveEmptyEntries);
            if (contentParts.Length == 2)
            {
                if (contentParts[1].StartsWith('\'') && contentParts[1].EndsWith('\''))
                    contentParts[1] = contentParts[1].Substring(1, contentParts[1].Length - 2);
                return new ComparisonFilter()
                {
                    Operator = comparisonOperator,
                    PropertyName = ToCamelCase(contentParts[0]),
                    Value = contentParts[1],
                };
            }
            return null;
        }

        #region StringFunction

        private static StringFunctionFilter ParseStringFunctionFilter(string oDataFilter)
        {
            StringFunctionFilter filter = null;
            // Se comprueba si se corresponde con alguna de las funciones soportadas
            if (oDataFilter.StartsWith("contains("))
                return ParseStringFunctionFilter(oDataFilter, "contains(", StringFunction.Contains);
            else if (oDataFilter.StartsWith("startswith("))
                return ParseStringFunctionFilter(oDataFilter, "startswith(", StringFunction.StartsWith);
            else if (oDataFilter.StartsWith("endswith("))
                return ParseStringFunctionFilter(oDataFilter, "endswith(", StringFunction.EndsWith);

            return filter;
        }

        private static StringFunctionFilter ParseStringFunctionFilter(string oDataFilter, string filterToFind, StringFunction function)
        {
            string[] contentParts = oDataFilter.Substring(filterToFind.Length).Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (contentParts.Length == 2)
            {
                string[] constantParts = contentParts[1].Trim().Split('\'', StringSplitOptions.RemoveEmptyEntries);
                return new StringFunctionFilter()
                {
                    Function = function,
                    PropertyName = ToCamelCase(contentParts[0]),
                    Value = constantParts[0],
                };
            }
            return null;
        }
        #endregion

        #endregion

        private static int? ParseIntExp(string intODataExp)
        {
            if (!string.IsNullOrEmpty(intODataExp) && int.TryParse(intODataExp, out int intValue))
                return intValue;
            return null;
        }

        private static bool? ParseBoolExp(string boolODataExp)
        {
            if (!string.IsNullOrEmpty(boolODataExp) && bool.TryParse(boolODataExp, out bool boolValue))
                return boolValue;
            return null;
        }

        private static List<OrderBy> ParseOrderBy(string orderByExp)
        {
            List<OrderBy> orderBy = new List<OrderBy>();
            if (!string.IsNullOrEmpty(orderByExp))
            {
                string[] orderByParts = orderByExp.Split(",", StringSplitOptions.RemoveEmptyEntries);
                foreach (string orderByPart in orderByParts.Select(o => o.Trim()))
                {
                    string[] orderByElements = orderByPart.Split(" ");
                    if (orderByElements.Length == 1)
                    {
                        orderBy.Add(new OrderBy()
                        {
                            PropertyName = ToCamelCase(orderByElements[0]),
                            OrderType = OrderType.Ascending // Por defecto es asc
                        });
                    }
                    else if (orderByElements.Length == 2)
                    {
                        OrderBy newOrderBy = new OrderBy() { PropertyName = ToCamelCase(orderByElements[0]) };
                        if (orderByElements[1] == "asc")
                        {
                            newOrderBy.OrderType = OrderType.Ascending;
                            orderBy.Add(newOrderBy);
                        }
                        else if (orderByElements[1] == "desc")
                        {
                            newOrderBy.OrderType = OrderType.Descending;
                            orderBy.Add(newOrderBy);
                        }
                    }
                }
            }

            return orderBy;
        }

        private static string ToCamelCase(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName) && !char.IsUpper(propertyName[0]))
                return string.Concat(char.ToUpper(propertyName[0]), propertyName.Substring(1));
            return propertyName;
        }
    }
}
