using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Security;

namespace DynamicRepository.Mapping
{
    /// <summary>
    /// Translates API public filter settings to possible back-end properties.
    /// </summary>
    /// <remarks>
    /// By using these adapters it helps building an API that is less prompt to cause UI to break,
    /// since after it Entity properties can be renamed without affecting how UI name or interface with them.
    /// </remarks>
    public static class PagedDataAdapter
    {
        /// <summary>
        /// Adapts data that was sent by any consumer (Can be API or UI) to a DB persisted model.
        /// </summary>
        /// <param name="callerClassType">
        /// This is the caller class type. Just use "this" as a reference.
        /// </param>
        /// <param name="callingMethodName">
        /// This is the method of the calling name. Use it as "nameof(MyMethod)".
        /// </param>
        /// <param name="settings">
        /// The initial settings sent by any consumer of page data functionality.
        /// </param>
        /// <returns>
        /// Returns a new settings model that can be used to filter DB model entities.
        /// </returns>
        /// <example>
        /// PagedDataSettings.TransformSettings(this, nameof(MyMethod), MySettingsPayload);
        /// </example>
        public static PagedDataSettings TransformSettings(Type callerClassType, string callingMethodName, PagedDataSettings settings)
        {
            var transformedSettings = new PagedDataSettings(); // We take the input and transform it back to the user.

            // Gets previous calling method data with the decorated attributes.
            MethodBase methodBase = callerClassType.GetMethod(callingMethodName);

            var props = methodBase.GetCustomAttributes<PagedDataAdapterAttribute>();
            var sorting = methodBase.GetCustomAttribute<PagedDataDefaultSortingAttribute>();

            // Checks if caller wants to filter in ALL fields.
            if (settings.Filter.Where(x => x.Property.ToUpper() == PagedDataSettings.FILTERALLIDENTIFIER).Any())
            {
                // Overriding all possible conjunctions set as OR since this is an "ALL" search.
                foreach (var filter in settings.Filter)
                {
                    filter.Conjunction = LogicalConjunctionEnum.AND;
                }

                InspectForAllFilter(props, settings, transformedSettings);
            }
            else
            {
                // TODO: Implement "OR" in client side / consumer if needed/request by someone.
                if (settings.Filter.Where(x => x.Conjunction != LogicalConjunctionEnum.AND).Any())
                {
                    throw new NotImplementedException($"'Or' filtering not supported by: {nameof(PagedDataAdapter)} transformation class.");
                }

                InspectFilterSettings(props, settings, transformedSettings);
            }

            // Copies (or adds default) sorting configurations.
            InspectSorting(props, sorting, settings, transformedSettings);

            return settings;
        }

        /// <summary>
        /// Inspects if "ALL" option should be applied. This will gather all attributes decorated in the method and shape possible filter results.
        /// </summary>
        private static void InspectForAllFilter(IEnumerable<PagedDataAdapterAttribute> props, PagedDataSettings initialSettings, PagedDataSettings transformedSettings)
        {
            bool firstRun = true;
            initialSettings.SearchInALL = true;

            // Saves all added filters which are not ALL. This may be extra filters that are not mapped directly to DB Entities.
            var otherFilters = initialSettings.Filter.Where(x => x.Property.ToUpper() != PagedDataSettings.FILTERALLIDENTIFIER).ToList();

            // Gets the value in "All" search if supplied.
            var filterValue = initialSettings.Filter.Where(x => x.Property.ToUpper() == PagedDataSettings.FILTERALLIDENTIFIER).FirstOrDefault().Value;

            // Inspecting each property sent by the consumer of Paged Data.
            foreach (var property in props)
            {
                var attrValue = property.MapsTo;

                transformedSettings.Filter.Add(new FilterSettings()
                {
                    PostQueryFilterPath = property.InMemoryPath,
                    Property = property.MapsTo,
                    IsExactMatch = false,
                    Value = filterValue ?? string.Empty,
                    Conjunction = firstRun ? LogicalConjunctionEnum.AND : LogicalConjunctionEnum.OR
                });

                firstRun = false;
            }

            // Removes all duplicates from filters that could be mapped. We will leave just filters that were not applied here.
            otherFilters.RemoveAll(x => transformedSettings.Filter.Where(y => y.Property == x.Property).Any());

            // After we copied all filters that could be mapped by the adapter decorators, we are also pushing the ones that could not (Since they can be extra filters).
            transformedSettings.Filter = transformedSettings.Filter.Concat(otherFilters).ToList();
        }

        private static void InspectFilterSettings(IEnumerable<PagedDataAdapterAttribute> props, PagedDataSettings settings, PagedDataSettings transformedSettings)
        {
            // If it can find, then do the job. Otherwise we will fallback to whatever the UI sends directly to IQueryable. (No security issue since this is just for filter/ordering.
            foreach (var property in props)
            {
                // Searchs for filters that were decorated.
                var filterProp = settings.Filter.Where(x => x.Property.Equals(property.PropFrom)).FirstOrDefault();

                if (filterProp != null)
                {
                    transformedSettings.Filter.Add(
                        new FilterSettings()
                        {
                            Property = property.MapsTo,
                            PostQueryFilterPath = property.InMemoryPath
                        });
                }
            }
        }

        /// <summary>
        /// Configures settins for sorting if specified by user, or adds default sorting field that must be specified.
        /// </summary>
        private static void InspectSorting(IEnumerable<PagedDataAdapterAttribute> props, PagedDataDefaultSortingAttribute sortingAttribute, PagedDataSettings settings, PagedDataSettings transformedSettings)
        {
            foreach (var property in props)
            {
                if (settings.Order != null)
                {
                    // Searchs filter configurations sent by consumer.
                    var item = settings.Order.Where(x => x.Property == property.PropFrom).FirstOrDefault();

                    if (item != null)
                    {
                        transformedSettings.Order.Add(
                        new SortingSettings()
                        {
                            Property = property.MapsTo,
                            PostQuerySortingPath = property.InMemoryPath.Split('|').First() // Only gets the first if it is piped for sorting.
                        });
                    }
                }
            }

            // Only applies this default sorting if consumer specified none.
            if (transformedSettings.Order == null || transformedSettings.Order.Count == 0)
            {
                if (sortingAttribute != null)
                {
                    transformedSettings.Order.Add(new SortingSettings()
                    {
                        Property = sortingAttribute.Property,
                        Order = sortingAttribute.IsAscending ? SortOrderEnum.ASC : SortOrderEnum.DESC
                    });
                }
                else
                {
                    throw new MissingMemberException("No sorting was specified by the consumer and no default sorting property was configured among the adapters therefore it is not possible to excecute the paged data query.");
                }
            }
        }
    }
}
