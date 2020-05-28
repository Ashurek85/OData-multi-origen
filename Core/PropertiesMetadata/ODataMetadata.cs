using Core.Models;
using Core.Models.Filters;
using Core.Models.Order;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.PropertiesMetadata
{
    public class ODataMetadata
    {

        private readonly Dictionary<FilterBase, MetadataBase> filtersMetadata;
        private readonly Dictionary<OrderBy, MetadataBase> ordersByMetadata;

        private ODataMetadata()
        {
            filtersMetadata = new Dictionary<FilterBase, MetadataBase>();
            ordersByMetadata = new Dictionary<OrderBy, MetadataBase>();
        }

        public MetadataBase GetFilterMetadata(FilterBase filter)
        {
            if (filtersMetadata.ContainsKey(filter))
                return filtersMetadata[filter];
            return null;
        }

        public IEnumerable<MetadataBase> GetActionSources()
        {
            List<MetadataBase> actionSources = filtersMetadata.Values.Distinct().ToList();
            if (ordersByMetadata.Values.Any() && !actionSources.Contains(ordersByMetadata.Values.First()))
                actionSources.Add(ordersByMetadata.Values.First());
            return actionSources;
        }

        /// <summary>
        /// Indica si la referencia de metadatos tiene algún filtro vinculado
        /// </summary>
        /// <param name="metadata">Referencia de metadatos</param>
        /// <returns>True si hay algún filtro vinculado y false en el caso contrario</returns>
        public bool HasFilters(MetadataBase metadata)
        {
            return filtersMetadata.Values.Any(m => m == metadata);
        }

        public MetadataBase GetOrderBySource()
        {
            return ordersByMetadata.Values.FirstOrDefault();
        }

        public KeyValuePair<FilterBase, List<LogicalFilter>> GetFiltersByMetadata(ODataExpression oDataExpression, MetadataBase metadata)
        {
            IEnumerable<FilterBase> filters = filtersMetadata.Where(f => f.Value == metadata).Select(f => f.Key);
            switch(filters.Count())
            {
                case 0:
                    return new KeyValuePair<FilterBase, List<LogicalFilter>>();
                case 1:
                    return new KeyValuePair<FilterBase, List<LogicalFilter>>(filters.ElementAt(0), null);
                default:
                    IEnumerable<LogicalFilter> logicalFilters = filters.Except(new FilterBase[] { filters.ElementAt(0) }).Select(f =>
                    {
                        return oDataExpression.LogicalFilters.FirstOrDefault(l => l.Filter == f);
                    });
                    return new KeyValuePair<FilterBase, List<LogicalFilter>>(filters.ElementAt(0), logicalFilters.ToList());
            }
        }

        public static ODataMetadata Build(ODataExpression oDataExpression, MetadataBase guideMetadata, MetadataBase complementaryMetadata)
        {
            ODataMetadata instance = new ODataMetadata();

            // OrderBy
            List<OrderBy> ambiguousOrdersBy = new List<OrderBy>();
            foreach (OrderBy orderBy in oDataExpression.OrdersBy)
            {
                if (guideMetadata.GetProperty(orderBy.PropertyName) != null && complementaryMetadata.GetProperty(orderBy.PropertyName) != null)
                    ambiguousOrdersBy.Add(orderBy);
                else if (guideMetadata.GetProperty(orderBy.PropertyName) != null)
                    instance.ordersByMetadata.Add(orderBy, guideMetadata);
                else if (complementaryMetadata.GetProperty(orderBy.PropertyName) != null)
                    instance.ordersByMetadata.Add(orderBy, complementaryMetadata);
                else
                    throw new Exception($"The property {orderBy.PropertyName} does not exists on {guideMetadata.UnderlyingType.FullName} or {complementaryMetadata.UnderlyingType.FullName}. Metadata orderBy");
            }

            if (ambiguousOrdersBy.Any())
            {
                int differentOrderBySources = instance.ordersByMetadata.Values.Distinct().Count();
                if (differentOrderBySources > 1)
                    throw new Exception($"OrderBy clauses only can be apply over one query");
                else if (differentOrderBySources == 1) // Solo hay un origen, debe ser el mismo para todos
                {
                    // Se vuelcan los ambiguos
                    MetadataBase metadataBase = instance.ordersByMetadata.Values.First();
                    ambiguousOrdersBy.ForEach(o => instance.ordersByMetadata.Add(o, metadataBase));
                    ambiguousOrdersBy.Clear();
                }
            }

            // Filters
            List<FilterBase> ambiguousFilters = new List<FilterBase>();
            foreach (FilterBase filter in oDataExpression.GetAllFilters())
            {
                if (guideMetadata.GetProperty(filter.PropertyName) != null && complementaryMetadata.GetProperty(filter.PropertyName) != null)
                    ambiguousFilters.Add(filter);
                else if (guideMetadata.GetProperty(filter.PropertyName) != null)
                    instance.filtersMetadata.Add(filter, guideMetadata);
                else if (complementaryMetadata.GetProperty(filter.PropertyName) != null)
                    instance.filtersMetadata.Add(filter, complementaryMetadata);
                else
                    throw new Exception($"The property {filter.PropertyName} does not exists on {guideMetadata.UnderlyingType.FullName} or {complementaryMetadata.UnderlyingType.FullName}. Metadata filter");
            }

            // Resolución final de ambiguos
            if (ambiguousFilters.Any())
            {
                MetadataBase filterMetadata = instance.filtersMetadata.Values.FirstOrDefault();
                if (filterMetadata != null)
                {
                    // Todos van al primero de los origenes de los filtros
                    ambiguousFilters.ForEach(f => instance.filtersMetadata.Add(f, filterMetadata));
                }
                else
                {
                    // No hay, ¿y order by?
                    if (instance.ordersByMetadata.Any())
                    {
                        // Todos al mismo que el/los orderby
                        ambiguousFilters.ForEach(f => instance.filtersMetadata.Add(f, instance.ordersByMetadata.Values.First()));
                    }
                    else
                    {
                        // Hardcode a la guia. No hay criterio para elegir
                        ambiguousFilters.ForEach(f => instance.filtersMetadata.Add(f, guideMetadata));
                    }
                }
            }

            if (ambiguousOrdersBy.Any())
            {
                // No hay orders by no ambiguos, se coge como referencia los filtros
                MetadataBase filterMetadata = instance.filtersMetadata.Values.FirstOrDefault();
                if (filterMetadata != null)
                {
                    // Al primero de los filtros
                    ambiguousOrdersBy.ForEach(o => instance.ordersByMetadata.Add(o, filterMetadata));
                }
                else
                {
                    // Sin mucho criterio para elegir: a la guia
                    ambiguousOrdersBy.ForEach(o => instance.ordersByMetadata.Add(o, guideMetadata));
                }
            }

            return instance;
        }
    }
}
