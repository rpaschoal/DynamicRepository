using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using DynamicRepository.Filter;
using DynamicRepository.Extensions;
using System.Linq.Dynamic.Core;
using LinqKit;
using System.Globalization;
using LinqKit.Core;

namespace DynamicRepository
{
    /// <summary>
    /// Implementation of IQueryable result set paging and dynamic filtering/sorting.
    /// </summary>
    /// <typeparam name="Key">The type of the key of the Entity.</typeparam>
    /// <typeparam name="Entity">The Entity being paged.</typeparam>
    public class DataPager<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// This is the date time format that will be sent to the pager on datetime filters.
        /// If your UI has a different date formatt, you can override this by this class constructor.
        /// </summary>
        private string UI_DATE_FORMAT = "dd/MM/yyyy";

        /// <summary>
        /// Default class construcotr.
        /// </summary>
        public DataPager()
        {
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dateFormat">
        /// Set a DateTime mask format (Same as .NET patterns) to be used when filtering datetime properties. 
        /// This identifies how the value will come by your API, MVC Controller, etc.
        /// </param>
        public DataPager(string dateFormat)
        {
            this.UI_DATE_FORMAT = dateFormat;
        }

        /// <summary>
        /// Returns a collection of data results that can be paged.
        /// </summary>
        /// <param name="queryableToPage">IQueryable instance of <see cref="Entity"/> which will act as data source for the pagination.</param>
        /// <param name="settings">Settings for the search.</param>
        /// <param name="PreConditionsToPagedDataFilter">Pre condition Expression Filters.</param>
        /// <param name="ExtraPagedDataFilter">Extra conditions Expression Filters to be applied along with settings filters.</param>
        /// <returns>Filled PageData results instance.</returns>
        public IPagedDataResult<Entity> GetPagedData(IQueryable<Entity> queryableToPage,
                                                                 PagedDataSettings settings,
                                                                 Expression<Func<Entity, bool>> PreConditionsToPagedDataFilter = null,
                                                                 Expression<Func<Entity, bool>> ExtraPagedDataFilter = null)
        {
            try
            {
                IQueryable<Entity> pagedDataQuery = queryableToPage;

                // Applies pre conditioning filtering to the data source. (This is a pre-filter that executes before the filters instructed by PagedDataSettings).
                if (PreConditionsToPagedDataFilter != null)
                {
                    pagedDataQuery = pagedDataQuery.Where(PreConditionsToPagedDataFilter);
                }

                // Adds composed filter to the query here (This is the default filter inspector bult-in for the search).
                // This is a merge result from default query engine + customized queries from devs (ExtraPagedDataFilter method).
                pagedDataQuery = pagedDataQuery.Where(MergeFilters(settings, DefaultPagedDataFilter(settings), ExtraPagedDataFilter, settings.SearchInALL));

                // Adds sorting capabilities
                pagedDataQuery = this.AddSorting(pagedDataQuery, settings);

                // Total number of records regardless of paging.
                var totalRecordsInDB = pagedDataQuery.AsExpandable().Count();

                // Shapes final result model. Post query filters to inner collection data are applied at this moment.
                return pagedDataQuery.Skip((settings.Page - 1) * settings.TotalPerPage).Take(settings.TotalPerPage).AsExpandable().BuildUpResult(totalRecordsInDB, (p) => PostQueryCallbacksInvoker(p, settings));
            }
            catch (Exception ex)
            {
                throw new Exception($"There was an error paging the datasource for entity: {nameof(Entity)}. Exception Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds default filter mechanism to GetPagedData method.
        /// </summary>
        /// <remarks>
        /// This method allows multi-navigation property filter as long as they are not collections.
        /// It also supports collection BUT the collection needs to be the immediate first level of navigation property, and you can't use more than one depth.
        /// </remarks>
        /// <param name="settings">Current filter settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable filter instance.</returns>
        protected virtual Expression<Func<Entity, bool>> DefaultPagedDataFilter(PagedDataSettings settings)
        {
            bool firstExecution = true;
            var queryLinq = string.Empty;
            // Holds Parameters values per index of this list (@0, @1, @2, etc).
            var paramValues = new List<object>();

            if (settings.Filter != null && settings.Filter.Count > 0)
            {
                var validFilterSettings = settings.Filter.Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.Value)).GroupBy(x => x.Property).Select(y => y.FirstOrDefault());

                foreach (var pFilter in validFilterSettings)
                {
                    int collectionPathTotal = 0;
                    var propInfo = this.GetValidatedPropertyInfo(pFilter.Property, out collectionPathTotal);
                    string nullableValueOperator = string.Empty;

                    // Apparently String implements IEnumerable, since it is a collection of chars
                    if (propInfo != null && (propInfo.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                    {
                        if (Nullable.GetUnderlyingType(propInfo.PropertyType) != null)
                        {
                            nullableValueOperator = ".Value";
                        }

                        if (collectionPathTotal == 0)
                        {
                            if (propInfo.PropertyType.IsAssignableFrom(typeof(DateTime)))
                            {
                                // Applies filter do DateTime properties
                                DateTime castedDateTime;
                                if (DateTime.TryParseExact(pFilter.Value, UI_DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out castedDateTime))
                                {
                                    // Successfully casted the value to a datetime.
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + "DbFunctions.TruncateTime(" + pFilter.Property + nullableValueOperator + ") == @" + paramValues.Count;
                                    paramValues.Add(castedDateTime.Date);
                                }
                            }
                            else
                            {
                                // Applying filter to nullable entity's property.
                                if (pFilter.IsExactMatch)
                                {
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + pFilter.Property + nullableValueOperator + ".ToString().ToUpper() == @" + paramValues.Count;
                                }
                                else
                                {
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + pFilter.Property + nullableValueOperator + ".ToString().ToUpper().Contains(@" + paramValues.Count + ")";
                                }
                            }

                            paramValues.Add(pFilter.Value.ToUpper());
                            firstExecution = false;
                        }
                        else
                        {
                            // Only supports if it is immediately the first level. We checked this above =)
                            var navigationPropertyCollection = pFilter.Property.Split('.')[0];

                            // Sub collection filter LINQ
                            // Applies filter do DateTime properties
                            if (propInfo.PropertyType.IsAssignableFrom(typeof(DateTime)))
                            {
                                DateTime castedDateTime;
                                if (DateTime.TryParseExact(pFilter.Value, UI_DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out castedDateTime))
                                {
                                    // Successfully casted the value to a datetime.
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + navigationPropertyCollection + ".Where(DbFunctions.TruncateTime(" + pFilter.Property.Remove(0, navigationPropertyCollection.Length + 1) + nullableValueOperator + ") == @" + paramValues.Count + ").Count() > 0";
                                    paramValues.Add(castedDateTime.Date);
                                }
                            }
                            else
                            {
                                if (pFilter.IsExactMatch)
                                {
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + navigationPropertyCollection + ".Where(" + pFilter.Property.Remove(0, navigationPropertyCollection.Length + 1) + nullableValueOperator + ".ToString().ToUpper() == @" + paramValues.Count + ").Count() > 0";
                                }
                                else
                                {
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + navigationPropertyCollection + ".Where(" + pFilter.Property.Remove(0, navigationPropertyCollection.Length + 1) + nullableValueOperator + ".ToString().ToUpper().Contains(@" + paramValues.Count + ")).Count() > 0";
                                }

                                paramValues.Add(pFilter.Value.ToUpper());
                            }

                            firstExecution = false;
                        }
                    }
                }
            }

            // Returns current default query as expression.
            return queryLinq.ParseLambda<Entity>(paramValues.ToArray());
        }

        /// <summary>
        /// Adds default sorting mechanism to GetPagedData method.
        /// </summary>
        /// <remarks>
        /// This method allows multi-navigation property filter as long as they are not collections.
        /// It also supports collection BUT the collection needs to be the immediate first level of navigation property, and you can't use more than one depth.
        /// 
        /// - The input IQueryable is being returned. Seems if you try to apply changes by reference, you don't get it outside of this method. May be implicit LINQ behavior.
        /// </remarks>
        /// <param name="settings">Current sorting settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable instance.</returns>
        private IQueryable<Entity> AddSorting(IQueryable<Entity> pagedDataQuery, PagedDataSettings settings)
        {
            bool noFilterApplied = true;

            // Generates the order clause based on supplied parameters
            if (settings.Order != null && settings.Order.Count > 0)
            {
                var bufferedSortByClause = string.Empty;
                var validOrderSettings = settings.Order.Where(x => !String.IsNullOrEmpty(x.Property) && String.IsNullOrEmpty(x.PostQuerySortingPath)).GroupBy(x => x.Property).Select(y => y.FirstOrDefault());

                foreach (var o in validOrderSettings)
                {
                    int collectionPathTotal = 0;
                    var propInfo = this.GetValidatedPropertyInfo(o.Property, out collectionPathTotal);

                    // Apparently String implements IEnumerable, since it is a collection of chars
                    if (propInfo != null && (propInfo.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                    {
                        // Just applying DB filters to non collection related properties.
                        if (collectionPathTotal == 0)
                        {
                            if (noFilterApplied)
                                noFilterApplied = false;

                            bufferedSortByClause += o.Property + " " + o.Order.ToString() + ",";
                        }
                        else
                        {
                            throw new Exception($"Database translated queries for collection properties are not supported. Please use method: {nameof(this.PostQueryFilter)}.");
                        }
                    }
                }

                // Applies the buffered sort by
                if (!noFilterApplied)
                {
                    bufferedSortByClause = bufferedSortByClause.Substring(0, bufferedSortByClause.Length - 1); // Removes last comma
                    pagedDataQuery = pagedDataQuery.OrderBy(bufferedSortByClause);
                }
            }

            // If there is no sorting configured, we need to add a default fallback one, which we will use the first property. Without this can't use LINQ Skip/Take
            if (settings.Order == null || settings.Order.Count == 0 || noFilterApplied)
            {
                var propCollection = typeof(Entity).GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => !typeof(IEnumerable).IsAssignableFrom(x.PropertyType)).ToList();

                if (propCollection.Count() > 0)
                {
                    var firstFieldOfEntity = propCollection[0].Name;
                    pagedDataQuery = pagedDataQuery.OrderBy(firstFieldOfEntity + " " + SortOrderEnum.DESC.ToString());
                }
                else
                {
                    throw new Exception($"The supplied Entity {nameof(Entity)} has no public properties, therefore the method can't continue the sorting operation.");
                }
            }

            return pagedDataQuery;
        }

        /// <summary>
        /// Merges two filters into one final query.
        /// </summary>
        /// <param name="query">The IQueryable instance to be parsed.</param>
        /// <returns>The string lambda expression.</returns>
        private Expression<Func<Entity, bool>> MergeFilters(PagedDataSettings settings,
            Expression<Func<Entity, bool>> expressionLeft,
            Expression<Func<Entity, bool>> expressionRight,
            bool isAllSearch = false)
        {
            if (expressionLeft == null && expressionRight == null)
            {
                return x => 1 == 1;
            }
            else if (expressionLeft != null && expressionRight != null)
            {
                if (isAllSearch)
                {
                    return PredicateBuilder.Or(expressionLeft, expressionRight);
                }
                else
                {
                    return PredicateBuilder.And(expressionLeft, expressionRight);
                }
            }
            else if (expressionLeft == null)
            {
                return expressionRight;
            }
            else
            {
                return expressionLeft;
            }
        }

        /// <summary>
        /// Filters the final paged result AFTER the projection was executed in database by adapters.
        /// </summary>
        /// <remarks>
        /// This method is useful for children collection filter. The only way to accomplish through LINQ.
        /// </remarks>
        private IList PostQueryCallbacksInvoker(IList fetchedResult, PagedDataSettings settings)
        {
            fetchedResult = this.PostQueryFilter(fetchedResult, settings);
            fetchedResult = this.PostQuerySort(fetchedResult, settings);

            return fetchedResult;
        }

        /// <summary>
        /// Applies filter to inner collections of a query result set from database.
        /// This is applied as a memory LINQ To Objects filter.
        /// </summary>
        /// <param name="fetchedResult">This is the return from EF query after going to DB.</param>
        /// <param name="settings">Paged data source settings.</param>
        /// <returns>Filtered collection result.</returns>
        private IList PostQueryFilter(IList fetchedResult, PagedDataSettings settings)
        {
            if (settings.Filter != null && settings.Filter.Count > 0 && !settings.SearchInALL)
            {
                var validFilterSettings = settings.Filter
                                                  .Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.Value) && !String.IsNullOrEmpty(x.PostQueryFilterPath))
                                                  .GroupBy(x => x.Property)
                                                  .Select(y => y.FirstOrDefault());

                if (validFilterSettings.Count() > 0)
                {
                    foreach (var result in fetchedResult)
                    {
                        string bufferedNavigationProperty = string.Empty; // TODO: Check if this needs to be refactored.
                        bool firstExecution = true; // Identifies if it is the first filter added in order to apply or not conjunctions.
                        var queryLinq = string.Empty; // Final lambda query to be applied to resulting data source which is already Linq-To-Objects
                        var paramValues = new List<object>(); // Holds Parameters values per index of this list (@0, @1, @2, etc).

                        foreach (var pFilter in validFilterSettings)
                        {
                            // This allows piping for DTO in memory filtering paths.
                            var pipes = pFilter.PostQueryFilterPath.Split('|');
                            bool piped = false; // Set this to true in the end if we run more than one pipe.
                            string pipedQuery = "";

                            foreach (var pipe in pipes)
                            {
                                // Only supports if it is immediately the first level. We checked this above =)
                                var navigationPropertyCollection = pipe.Split('.')[0];

                                // We are buffering the query, but if the property has changed, then we will execute and replace the value inMemory and move on
                                if (!firstExecution && bufferedNavigationProperty != navigationPropertyCollection)
                                {
                                    result.ReplaceCollectionInstance(bufferedNavigationProperty, queryLinq);

                                    // Assign brand new values to move on as a new. Resetting here since seems the collection property CHANGED.
                                    queryLinq = string.Empty;
                                    firstExecution = true;
                                    pipedQuery = string.Empty;
                                }

                                bufferedNavigationProperty = navigationPropertyCollection;
                                int collectionPathTotal = 0;
                                var propInfo = result.GetNestedPropInfo(navigationPropertyCollection, out collectionPathTotal);

                                // Tests if the current property to be filtered is NULLABLE in order to add ".Value" to string lambda query.
                                var nullableValueOperator = string.Empty;
                                var pipeProperty = pipe.Remove(0, navigationPropertyCollection.Length + 1); // Clean pipe without collection prefix.

                                // Apparently String implements IEnumerable, since it is a collection of chars
                                if (propInfo != null && (propInfo.PropertyType != typeof(string) || typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                                {
                                    // Nullable subproperty field.
                                    if (Nullable.GetUnderlyingType(result.GetNestedPropInfo(pipe).PropertyType) != null)
                                    {
                                        nullableValueOperator = ".Value";
                                    }

                                    // Sub collection filter LINQ
                                    // Applies filter do DateTime properties
                                    if (result.GetNestedPropInfo(pipe).PropertyType.IsAssignableFrom(typeof(DateTime)))
                                    {
                                        // Applies filter do DateTime properties
                                        DateTime castedDateTime;
                                        if (DateTime.TryParseExact(pFilter.Value, UI_DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out castedDateTime))
                                        {
                                            // Successfully casted the value to a datetime.
                                            queryLinq += (!piped ? string.Empty : " OR ") + pipeProperty + nullableValueOperator + ".Date == @" + paramValues.Count;

                                            paramValues.Add(castedDateTime.Date);
                                        }
                                    }
                                    else
                                    {
                                        // Sub collection filter LINQ
                                        if (pFilter.IsExactMatch)
                                        {
                                            pipedQuery += (!piped ? string.Empty : " OR ") + pipeProperty + nullableValueOperator + ".ToString().ToUpper() == \"" + pFilter.Value.ToUpper() + "\"";
                                        }
                                        else
                                        {
                                            pipedQuery += (!piped ? string.Empty : " OR ") + pipeProperty + nullableValueOperator + ".ToString().ToUpper().Contains(\"" + pFilter.Value.ToUpper() + "\")";
                                        }
                                    }
                                }

                                piped = true;
                            }

                            if (!String.IsNullOrEmpty(pipedQuery))
                                queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + "(" + pipedQuery + ")";

                            firstExecution = false;
                        }

                        result.ReplaceCollectionInstance(bufferedNavigationProperty, queryLinq);
                    }
                }
            }

            return fetchedResult;
        }

        /// <summary>
        /// Applies in memory sorting to IList.
        /// </summary>
        /// <param name="fetchedResult">This is the return from EF query after going to DB.</param>
        /// <param name="settings">Paged data source settings.</param>
        /// <returns>Sorted collection result.</returns>
        private IList PostQuerySort(IList fetchedResult, PagedDataSettings settings)
        {
            // Generates the order clause based on supplied parameters
            if (settings.Order != null && settings.Order.Count > 0)
            {
                var validOrderSettings = settings.Order.Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.PostQuerySortingPath)).GroupBy(x => x.Property).Select(y => y.FirstOrDefault());

                foreach (var o in validOrderSettings)
                {
                    foreach (var result in fetchedResult)
                    {
                        // Only supports if it is immediately the first level. We checked this above =)
                        var navigationPropertyCollection = o.PostQuerySortingPath.Split('.')[0];

                        int collectionPathTotal = 0;
                        var propInfo = result.GetNestedPropInfo(navigationPropertyCollection, out collectionPathTotal);

                        // Apparently String implements IEnumerable, since it is a collection of chars
                        if (propInfo != null && (propInfo.PropertyType != typeof(string) || typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                        {
                            // Gets the property reference
                            var collectionProp = result.GetPropValue(navigationPropertyCollection);

                            if (typeof(IQueryable).IsAssignableFrom(collectionProp.GetType()))
                            {
                                // Applies filter to the IQueryable since it was inMemory Filtered.
                                collectionProp = ((IQueryable)collectionProp).OrderBy(o.PostQuerySortingPath.Substring(navigationPropertyCollection.Length + 1) + " " + o.Order.ToString());

                                // Filter in memory data here
                                result.SetPropValue(navigationPropertyCollection, collectionProp, true);
                            }
                            else
                            {
                                // Applies filter to the nested collection within the main entity.
                                collectionProp = ((IList)collectionProp).AsQueryable().OrderBy(o.PostQuerySortingPath.Substring(navigationPropertyCollection.Length + 1) + " " + o.Order.ToString());

                                // Filter in memory data here
                                result.SetPropValue(navigationPropertyCollection, collectionProp, true);
                            }
                        }
                    }
                }
            }

            return fetchedResult;
        }

        /// <summary>
        /// Gets <see cref="Entity"/> property validated.
        /// </summary>
        /// <param name="propertyPath">The property by dot notation path. Eg: Legs.Aircraft</param>
        /// <param name="collectionPathTotal">The number of collections found in depth on this path.</param>
        /// <returns>Reflection time property information.</returns>
        protected internal PropertyInfo GetValidatedPropertyInfo(string propertyPath, out int collectionPathTotal)
        {
            // Means we found that property in the entity model. Otherwise we should ignore or face with an Exception from IQueryable.
            // We are also checking to see if we are not querying directly to the collection holder.
            var propInfo = new Entity().GetNestedPropInfo(propertyPath, out collectionPathTotal);

            if (collectionPathTotal > 1)
            {
                throw new Exception($"{nameof(this.GetPagedData)} method does not support more than one nested collection depth filter. Please try using ExtraPagedDataFilters for more advanced queries.");
            }
            else if (collectionPathTotal == 1)
            {
                var firstLevelPropInfo = new Entity().GetNestedPropInfo(propertyPath.Split('.')[0]).PropertyType;

                // Checks if it is not the first level of depth that holds the collection
                if (firstLevelPropInfo == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(firstLevelPropInfo))
                {
                    // Seriously, this is too much innovation if it happens.
                    throw new Exception($"{nameof(this.GetPagedData)} method does not support to filter in collections which are not in the first level of depth. . Please try using ExtraPagedDataFilters for more advanced queries.");
                }
            }

            return propInfo;
        }
    }
}
