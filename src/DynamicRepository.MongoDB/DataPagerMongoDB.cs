using DynamicRepository.Extensions;
using DynamicRepository.Filter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace DynamicRepository.MongoDB
{
    /// <summary>
    /// Custom behaviors of <see cref="DataPager{Key, Entity}"/> for MongoDB driver.
    /// </summary>
    /// <typeparam name="Key">
    /// The entity type of the key of this collection.
    /// </typeparam>
    /// <typeparam name="Entity">
    /// The Entity type mapped by the desired collection.
    /// </typeparam>
    public class DataPagerMongoDB<Key, Entity> : DataPager<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Filters paged data based on <see cref="PagedDataSourceSettings"/>. 
        /// </summary>
        /// <param name="settings">Custom settings to be dynamically converted and apply as filters to the result set.</param>
        /// <returns>
        /// Filtered result set from MongoDB.
        /// </returns>
        /// <remarks>
        /// This method was overriden here because MongoDB driver is not mature enough as EntityFramework, 
        /// so some filter settings from the default <see cref="QueryableDataSourcePager{Key, Entity}"/> where throwing runtime exceptions.
        /// </remarks>
        protected override Expression<Func<Entity, bool>> DefaultPagedDataFilter(PagedDataSettings settings)
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
                    string nullableValueOperator = "";

                    // Apparently String implements IEnumerable, since it is a collection of chars
                    if (propInfo != null && (propInfo.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                    {
                        if (Nullable.GetUnderlyingType(propInfo.PropertyType) != null)
                        {
                            nullableValueOperator = ".Value";
                        }

                        if (collectionPathTotal == 0)
                        {
                            if (propInfo.PropertyType == typeof(string))
                            {
                                // Applying filter to nullable entity's property.
                                if (pFilter.IsExactMatch)
                                {
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + pFilter.Property + nullableValueOperator + ".ToUpper() == @" + paramValues.Count;
                                }
                                else
                                {
                                    queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + pFilter.Property + nullableValueOperator + ".ToUpper().Contains(@" + paramValues.Count + ")";
                                }

                                paramValues.Add(pFilter.Value.ToUpper());
                                firstExecution = false;
                            }
                            else
                            {
                                throw new NotImplementedException($"{nameof(DataPagerMongoDB<Key, Entity>)} only supports string properties filtering at the moment.");
                            }
                        }
                        else
                        {
                            throw new NotImplementedException($"Inner collection filtering is not supported by {nameof(DataPagerMongoDB<Key, Entity>)}");
                        }
                    }
                }
            }

            // Returns current default query as expression.
            return queryLinq.ParseLambda<Entity>(paramValues.ToArray());
        }
    }
}
