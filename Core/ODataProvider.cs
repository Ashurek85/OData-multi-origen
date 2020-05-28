using Core.Models;
using Core.Models.Filters;
using Core.Models.Order;
using Core.PropertiesMetadata;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Core
{
    public class ODataProvider<Guide, Complementary, TJoinProperty>
        where Guide : class
        where Complementary : class, new()
    {
        private readonly ODataExpression oDataExpression;
        private readonly string guideJoinProperty;
        private readonly string complementaryJoinProperty;

        private readonly Metadata<Guide> guideMetadata;
        private readonly Metadata<Complementary> complementaryMetadata;

        private readonly ODataMetadata oDataMetadata;

        private readonly IQueryable<Guide> guideQuery;
        private readonly IQueryable<Complementary> complementaryQuery;

        public ODataProvider(IEnumerable<KeyValuePair<string, StringValues>> oDataQueryValues,
                             IQueryable<Guide> guideQuery,
                             IQueryable<Complementary> complementaryQuery,
                             string guideJoinProperty,
                             string complementaryJoinProperty)
        {
            if (guideQuery == null)
                throw new ArgumentNullException(nameof(guideQuery));
            if (complementaryQuery == null)
                throw new ArgumentNullException(nameof(complementaryQuery));
            if (string.IsNullOrEmpty(guideJoinProperty))
                throw new ArgumentNullException(nameof(guideJoinProperty));
            if (string.IsNullOrEmpty(complementaryJoinProperty))
                throw new ArgumentNullException(nameof(complementaryJoinProperty));

            // Check JoinProperties
            this.guideJoinProperty = guideJoinProperty;
            this.complementaryJoinProperty = complementaryJoinProperty;

            this.guideQuery = guideQuery;
            this.complementaryQuery = complementaryQuery;

            guideMetadata = new Metadata<Guide>();
            complementaryMetadata = new Metadata<Complementary>();

            PropertyInfo propT1 = guideMetadata.GetProperty(guideJoinProperty);
            if (propT1 == null)
                throw new ArgumentException($"The property {guideJoinProperty} is not present in type {typeof(Guide).FullName}", nameof(guideJoinProperty));
            if (propT1.PropertyType != typeof(TJoinProperty))
                throw new Exception($"The join property {guideJoinProperty} in type {typeof(Guide).FullName} must be {typeof(TJoinProperty).FullName}");
            PropertyInfo propT2 = complementaryMetadata.GetProperty(complementaryJoinProperty);
            if (propT2 == null)
                throw new ArgumentException($"The property {complementaryJoinProperty} is not present in type {typeof(Complementary).FullName}", nameof(complementaryJoinProperty));
            if (propT2.PropertyType != typeof(TJoinProperty))
                throw new Exception($"The join property {complementaryJoinProperty} in type {typeof(Complementary).FullName} must be {typeof(TJoinProperty).FullName}");

            if (propT1.PropertyType != propT2.PropertyType)
                throw new ArgumentException($"The property {guideJoinProperty} ({propT1.PropertyType.FullName}) of {typeof(Guide).FullName} has diferent type than " +
                                            $"property {complementaryJoinProperty} ({propT2.PropertyType.FullName}) of {typeof(Complementary).FullName}");


            // Se extrae la operación a realizar del QueryString
            oDataExpression = Extractor.Parse(oDataQueryValues);

            // Se construyen los metadatos
            oDataMetadata = ODataMetadata.Build(oDataExpression, guideMetadata, complementaryMetadata);
        }

        #region Execute

        public QueryResponse<TResult> Execute<TResult>(Func<Guide, Complementary, TResult> buildResultFunc)
            where TResult : class
        {
            // Aplicar los filtros
            // Se comprueba sobre que conjunto hay filtros de búsqueda
            ExecuteResult<Guide, Complementary> results = new ExecuteResult<Guide, Complementary>();

            QueryResponse<TResult> response = new QueryResponse<TResult>();

            IEnumerable<MetadataBase> actionsSources = oDataMetadata.GetActionSources();
            if (actionsSources.Count() < 2)
            {
                if (actionsSources.Count() == 0 || actionsSources.ElementAt(0) == guideMetadata)
                {
                    // GUIA Filtrado (opcional) y ordenado (opcional)    
                    KeyValuePair<IQueryable<Guide>, int?> guideData = ApplyAllMetadataAndCount(guideQuery, guideMetadata);
                    results.GuideItems = guideData.Key.ToList();
                    results.TotalCount = guideData.Value;
                    results.ComplementaryItems = GetExtradata(results.GuideItems.AsQueryable(), guideJoinProperty,
                                                              complementaryQuery, complementaryJoinProperty);
                }
                else
                {
                    // COMPLEMENTARIO
                    if (oDataMetadata.HasFilters(complementaryMetadata))
                    {
                        // COMPLEMENTARIO filtrado y ordenado (opcional)
                        KeyValuePair<IQueryable<Complementary>, int?> complementaryData = ApplyAllMetadataAndCount(complementaryQuery, complementaryMetadata);
                        results.ComplementaryItems = complementaryData.Key.ToList();
                        results.TotalCount = complementaryData.Value;
                        results.GuideItems = GetExtradata(results.ComplementaryItems.AsQueryable(), complementaryJoinProperty,
                                                          guideQuery, guideJoinProperty);
                    }
                    else
                    {
                        // COMPLEMENTARIO sin filtros y ordenado -> Generar Shadows
                        // Se obtienen los identificadores de la guia
                        IEnumerable<TJoinProperty> guideJoinValues = QueryBuilder<Guide>.SelectPropertyValues<TJoinProperty>(guideQuery, guideJoinProperty);
                        ExecuteResult<Guide, Complementary> tempResults = ExecuteWithComplementaryOnlyOrdered(guideJoinValues);
                        results.GuideItems = tempResults.GuideItems;
                        results.ComplementaryItems = tempResults.ComplementaryItems;
                        results.TotalCount = tempResults.TotalCount;
                    }
                }
            }
            else
            {
                // Sobre los dos
                bool orderByComplementary = oDataMetadata.GetOrderBySource() == complementaryMetadata;
                bool complementaryFiltered = oDataMetadata.GetFiltersByMetadata(oDataExpression, complementaryMetadata).Key != null;

                // COMPLEMENTARIO Ordenado y no filtrado. GUIA filtrada. Se generan shadows
                if (orderByComplementary)
                {
                    if (!complementaryFiltered)
                    {
                        // Escenario similar a complementario sin filtros y ordenado pero con la guia filtrada
                        // Se obtienen los identificadores de la guia
                        IEnumerable<TJoinProperty> guideJoinValues = QueryBuilder<Guide>.ApplyFiltersAndSelect<TJoinProperty>(guideQuery, oDataMetadata, oDataExpression, guideMetadata, guideJoinProperty);
                        ExecuteResult<Guide, Complementary> tempResults = ExecuteWithComplementaryOnlyOrdered(guideJoinValues);
                        results.GuideItems = tempResults.GuideItems;
                        results.ComplementaryItems = tempResults.ComplementaryItems;
                        results.TotalCount = tempResults.TotalCount;
                    }
                    else
                    {
                        // COMPLEMENTARIO Ordenado y filtrado. GUIA filtrada.
                        KeyValuePair<IQueryable<Complementary>, int?> complementaryValues = ApplyMetadataAndCount(complementaryQuery, complementaryMetadata, complementaryJoinProperty,
                                                                                                                  guideQuery, guideMetadata, guideJoinProperty);

                        results.TotalCount = complementaryValues.Value;
                        results.ComplementaryItems = complementaryValues.Key.ToList();
                        results.GuideItems = GetExtradata(results.ComplementaryItems.AsQueryable(), complementaryJoinProperty,
                                                          guideQuery, guideJoinProperty);
                    }
                }
                else
                {
                    // Escenario compartido para GUIA Ordenada o no. Si no está ordenado por la guia se devuelven en orden de devolución de la guia.
                    // GUIA Ordenada (opcional) y filtrada (opcional). COMPLEMENTARIO Filtrado
                    KeyValuePair<IQueryable<Guide>, int?> guideValues = ApplyMetadataAndCount(guideQuery, guideMetadata, guideJoinProperty,
                                                                                              complementaryQuery, complementaryMetadata, complementaryJoinProperty);

                    results.TotalCount = guideValues.Value;
                    results.GuideItems = guideValues.Key.ToList();
                    results.ComplementaryItems = GetExtradata(results.GuideItems.AsQueryable(), guideJoinProperty,
                                                              complementaryQuery, complementaryJoinProperty);
                }                
            }

            // Count
            response.TotalCount = results.TotalCount;

            // Para cada pareja de datos que se extraigan se aplica el Func            
            MetadataBase orderByMetadata = oDataMetadata.GetOrderBySource();
            if (orderByMetadata == null || orderByMetadata == guideMetadata)
                response.Results = InvokeResultsFuncByGuide(results.GuideItems, results.ComplementaryItems, buildResultFunc);
            else
                response.Results = InvokeResultsFuncByComplementary(results.GuideItems, results.ComplementaryItems, buildResultFunc);

            return response;
        }

        /// <summary>
        /// Extrae los valores cuando la lista complementaria sólo está ordenada. Sin filtros en la complementaria
        /// </summary>
        /// <param name="guideJoinValues">Identificadores de la Guia</param>
        /// <returns></returns>
        private ExecuteResult<Guide, Complementary> ExecuteWithComplementaryOnlyOrdered(IEnumerable<TJoinProperty> guideJoinValues)
        {
            // Sólo con ordenación
            ExecuteResult<Guide, Complementary> results = new ExecuteResult<Guide, Complementary>();

            // Se recuperan todos los identificadores de la complementaria ordenados
            List<TJoinProperty> complementaryJoinValues = QueryBuilder<Complementary>.SelectPropertyValues<TJoinProperty>(
                                                                        QueryBuilder<Complementary>.ApplyOrderBy(complementaryQuery, oDataExpression.OrdersBy),
                                                                        complementaryJoinProperty).ToList();

            // Se elimina de la complementaria posibles elementos "zombie" que no existan en la guia
            List<TJoinProperty> complementaryZombies = complementaryJoinValues.Except(guideJoinValues).ToList();
            complementaryZombies.ForEach(z => complementaryJoinValues.Remove(z));

            // Se construye la lista de PropertyReference con los valores del join
            List<PropertyReference<TJoinProperty>> joinValues = complementaryJoinValues.Select(c => PropertyReference<TJoinProperty>.Build(c)).ToList();

            // Se obtienen los postizos a añadir
            IEnumerable<TJoinProperty> propertiesToAdd = guideJoinValues.Except(complementaryJoinValues);

            // En función del tipo de ordenación se añaden por el inicio o por el final
            switch (oDataExpression.OrdersBy[0].OrderType)
            {
                case OrderType.Ascending:
                    // Al inicio
                    joinValues.InsertRange(0, propertiesToAdd.Select(p => PropertyReference<TJoinProperty>.BuildShadowReference(p)));
                    break;
                case OrderType.Descending:
                    // Al final
                    joinValues.AddRange(propertiesToAdd.Select(p => PropertyReference<TJoinProperty>.BuildShadowReference(p)));
                    break;
            }

            results.TotalCount = joinValues.Count;

            // Se aplica take y skip si corresponde
            if (oDataExpression.Skip.HasValue && !oDataExpression.Top.HasValue)
                joinValues = joinValues.Skip(oDataExpression.Skip.Value).ToList();
            else if (!oDataExpression.Skip.HasValue && oDataExpression.Top.HasValue)
                joinValues = joinValues.Take(oDataExpression.Top.Value).ToList();
            else if (oDataExpression.Skip.HasValue && oDataExpression.Top.HasValue)
                joinValues = joinValues.Skip(oDataExpression.Skip.Value).Take(oDataExpression.Top.Value).ToList();

            // Recuperación completa: COMPLEMENTARIOS
            List<Complementary> complementaryItems = QueryBuilder<Complementary>.ApplyOrderBy(
                                                            complementaryQuery.Where(QueryBuilder<Complementary>.BuildContainsExp(joinValues.Where(j => !j.IsShadowReference)
                                                                                                                                            .Select(j => j.Value).ToList(),
                                                                                                                                  complementaryJoinProperty)),
                                                            oDataExpression.OrdersBy)
                                                                                .ToList();
            // Se añaden los nulos
            for (int i = 0; i < joinValues.Count; i++)
            {
                // Si es un ShadowRefence no existe en los complementarios, se añade un postizo con la referencia
                if (joinValues[i].IsShadowReference)
                    complementaryItems.Insert(i, CreateComplementary(joinValues[i].Value));
            }

            results.ComplementaryItems = complementaryItems;

            // Recuperación completa: GUIA
            results.GuideItems = guideQuery.Where(QueryBuilder<Guide>.BuildContainsExp(joinValues.Select(j => j.Value).ToList(), guideJoinProperty)).ToList();
            return results;
        }

        private Complementary CreateComplementary(TJoinProperty joinPropertyValue)
        {
            Complementary complementary = new Complementary();
            complementaryMetadata.GetProperty(complementaryJoinProperty).SetValue(complementary, joinPropertyValue);
            return complementary;
        }

        private KeyValuePair<IQueryable<TSource>, int?> ApplyMetadataAndCount<TSource, TDestination>(IQueryable<TSource> sourceQuery, MetadataBase sourceMetadata, string sourceJoinProperty,
                                                                                                     IQueryable<TDestination> destinationQuery, MetadataBase destinationMetadata, string destinationJoinProperty)
            where TSource : class
            where TDestination : class
        {
            // Se recuperan los ids de la complementaria que cumplen los filtros
            IEnumerable<TJoinProperty> destinationJoinValues = QueryBuilder<TDestination>.ApplyFiltersAndSelect<TJoinProperty>(destinationQuery, oDataMetadata, oDataExpression, destinationMetadata, destinationJoinProperty);

            // Si las hubiera se aplican las condiciones de la GUIA
            IQueryable<TSource> sourceQueryModified = QueryBuilder<TSource>.ApplyFiltersByMetadata(sourceQuery, oDataMetadata, oDataExpression, sourceMetadata);

            // Se recuperan los ids de la guia que cumplen tambien las condiciones de la complementaria. Si no rinde bien contra SQL habrá que traerlos y aplicarlos en memoria
            sourceQueryModified = sourceQueryModified.Where(QueryBuilder<TSource>.BuildContainsExp(destinationJoinValues, sourceJoinProperty));

            int? count = null;

            // Se calcula el totalcount si corresponde
            if (oDataExpression.Count)
                count = sourceQueryModified.Count();

            // Se ordena
            sourceQueryModified = QueryBuilder<TSource>.ApplyOrderBy(sourceQueryModified, oDataExpression.OrdersBy);

            // Se aplica take y skip
            IQueryable<TSource> sourceItems = QueryBuilder<TSource>.ApplySkipTake(sourceQueryModified, oDataExpression.Top, oDataExpression.Skip);
            return new KeyValuePair<IQueryable<TSource>, int?>(sourceItems, count);
        }

        private KeyValuePair<IQueryable<TSource>, int?> ApplyAllMetadataAndCount<TSource>(IQueryable<TSource> query, MetadataBase metadata)
            where TSource : class
        {
            // Se aplican los filtros
            IQueryable<TSource> queryModified = QueryBuilder<TSource>.ApplyOrderBy(query, oDataExpression.OrdersBy);
            if (oDataExpression.InitialFilter != null)
                queryModified = queryModified.Where(GetFiltersExp<TSource>(metadata));

            // ¿Count?
            int? count = null;
            if (oDataExpression.Count)
                count = queryModified.Count();

            queryModified = QueryBuilder<TSource>.ApplySkipTake(queryModified, oDataExpression.Top, oDataExpression.Skip);

            return new KeyValuePair<IQueryable<TSource>, int?>(queryModified, count);
        }

        private List<TDestination> GetExtradata<TSource, TDestination>(IQueryable<TSource> sourceQuery, string sourcejoinProperty,
                                                                       IQueryable<TDestination> destinationQuery, string destinationJoinProperty)
            where TSource : class
            where TDestination : class
        {
            // Datos extra. Sólo una segunda query a para los complementarios
            List<TJoinProperty> joinPropertiesValues = QueryBuilder<TSource>.SelectPropertyValues<TJoinProperty>(sourceQuery, sourcejoinProperty)
                                                                            .ToList();
            return joinPropertiesValues.Any() ?
                        destinationQuery.Where(QueryBuilder<TDestination>.BuildContainsExp(joinPropertiesValues, destinationJoinProperty))
                                        .ToList() :
                        new List<TDestination>();

        }

        #endregion

        #region InvokeResultsFunc

        private IEnumerable<TResult> InvokeResultsFuncByGuide<TResult>(List<Guide> guideItems, List<Complementary> complementaryItems,
                                                                       Func<Guide, Complementary, TResult> buildResultFunc)
        {
            List<TResult> results = new List<TResult>();
            // Se genera la correspondencia leyendo los valores de las claves
            PropertyInfo guideKeyProperty = guideMetadata.GetProperty(guideJoinProperty);
            foreach (Guide guide in guideItems)
            {
                TJoinProperty guideKeyValue = (TJoinProperty)guideKeyProperty.GetValue(guide);

                // Linq en memoria para encontrar la corresponencia en los complementarios
                Complementary complementary = complementaryItems.AsQueryable().FirstOrDefault(
                                    QueryBuilder<Complementary>.BuildExp(complementaryJoinProperty, guideKeyValue));

                results.Add(buildResultFunc.Invoke(guide, complementary));
            }
            return results;
        }

        private IEnumerable<TResult> InvokeResultsFuncByComplementary<TResult>(List<Guide> guideItems, List<Complementary> complementaryItems,
                                                                               Func<Guide, Complementary, TResult> buildResultFunc)
        {
            List<TResult> results = new List<TResult>();
            // Se genera la correspondencia leyendo los valores de las claves
            PropertyInfo complementaryKeyProperty = complementaryMetadata.GetProperty(complementaryJoinProperty);
            foreach (Complementary complementary in complementaryItems)
            {
                TJoinProperty complementaryKeyValue = (TJoinProperty)complementaryKeyProperty.GetValue(complementary);

                // Linq en memoria para encontrar la corresponencia en los complementarios
                Guide guide = guideItems.AsQueryable().FirstOrDefault(
                                    QueryBuilder<Guide>.BuildExp(guideJoinProperty, complementaryKeyValue));

                results.Add(buildResultFunc.Invoke(guide, complementary));
            }
            return results;
        }

        #endregion

        private Expression<Func<T, bool>> GetFiltersExp<T>(MetadataBase metadata)
            where T : class
        {
            // Se determina el primer filtro
            FilterBase initialFilter;
            if (oDataMetadata.GetFilterMetadata(oDataExpression.InitialFilter) == metadata)
                initialFilter = oDataExpression.InitialFilter;
            else
            {
                // Se busca el primero. Se desechará el operador
                initialFilter = oDataExpression.LogicalFilters.Where(l => oDataMetadata.GetFilterMetadata(l.Filter) == metadata)
                                                              .Select(l => l.Filter)
                                                              .FirstOrDefault();
            }

            // Los siguientes con los operadores
            List<LogicalFilter> logicalFilters = oDataExpression.LogicalFilters.Where(l => oDataMetadata.GetFilterMetadata(l.Filter) == metadata &&
                                                                                           l.Filter != initialFilter)
                                                                               .ToList();
            return QueryBuilder<T>.BuildFiltersExp(metadata, initialFilter, logicalFilters);
        }

    }
}
