using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DynamicRepository.Annotation;

namespace DynamicRepository
{
    /// <summary>
    /// Translates annotation filter settings from annotations to be given as input for the <see cref="DataPager{Key, Entity}"/> class. 
    /// </summary>
    /// <remarks>
    /// The <see cref="DataPager{Key, Entity}"/> class is case sensitive, so make sure you set the correct mappings FROM => TO.
    /// </remarks>
    public static class DataPagerAdapter
    {
        /// <summary>
        /// This method translates a <see cref="PagedDataSettings"/> payload to another configured instance based on
        /// any <see cref="PagedDataFilterAttribute"/> or <see cref="PagedDataDefaultSortingAttribute"/>
        /// that may have been applied to a controller method.
        /// </summary>
        /// <param name="settings">
        /// Payload received by the controller serialized by the ASP.NET default serializer or anyother type
        /// of configuration you may use.
        /// </param>
        /// <param name="callingMethodInfo">
        /// The method base information that contains all the attributes in order to perform the adapter operation.
        /// </param>
        /// <remarks>
        /// This method needs to be called directly in the Controller method. 
        /// It won't work if called inside another method called by the controller.
        /// It needs to be right after the controller method's execution call stack.
        /// </remarks>
        /// <returns>Updated/Adapted settings that are ready to be supplied to the <see cref="DataPager{Key, Entity}"/> class.</returns>
        public static PagedDataSettings TransformSettings(PagedDataSettings settings, MethodBase callingMethodInfo)
        {
            // TODO: Not yet supported on .NET Core, refer to this: https://github.com/dotnet/corefx/issues/1797
            // Gets previous calling method information to get whatever attribute that may have been applied.
            //StackTrace stackTrace = new StackTrace();
            //MethodBase methodBase = stackTrace.GetFrame(1).GetMethod();

            var props = callingMethodInfo.GetCustomAttributes<PagedDataFilterAttribute>();
            var sorting = callingMethodInfo.GetCustomAttribute<PagedDataDefaultSortingAttribute>();

            // Checks if the consumer of the search wants to filter in ALL fields.
            if (settings.Filter.Where(x => x.Property.ToUpper() == PagedDataSettings.FILTERALLIDENTIFIER).Any())
            {
                // Overriding all possible conjunctions set as OR since this is a "ALL" search.
                foreach (var filter in settings.Filter)
                {
                    filter.Conjunction = LogicalConjunctionEnum.AND;
                }

                TranslateALLSearchFilters(props.Where(x => x.IgnoreOnSearch == false), settings);
            }

            TranslateDefaultFilters(props.Where(x => x.IgnoreOnSearch == false), settings);

            foreach (var property in props)
            {
                if (settings.Sorting != null)
                {
                    // Searchs replacements for sort queries.
                    var item = settings.Sorting.Where(x => x.Property == property.MapsFrom).FirstOrDefault();
                    if (item != null)
                    {
                        item.Property = property.MapsTo;

                        // Only gets the first item if it is piped for sorting.
                        // TODO: Implement piped sorting for future releases.
                        item.PostQuerySortingPath = property.PostQueryFilterPath.Split('|').First();
                    }
                }
            }

            TranslateDefaultSorting(sorting, settings);

            return settings;
        }

        private static void TranslateDefaultFilters(IEnumerable<PagedDataFilterAttribute> props, PagedDataSettings settings)
        {
            // If it can find, then do the job. Otherwise we will fallback to whatever the UI sends directly to IQueryable. (No security issue since this is just for filter/ordering.
            foreach (var property in props)
            {
                // Translates a prom from => to relationship.
                var filterProp = settings.Filter.Where(x => x.Property.Equals(property.MapsFrom)).FirstOrDefault();
                if (filterProp != null)
                {
                    filterProp.Property = property.MapsTo;
                    filterProp.PostQueryFilterPath = String.IsNullOrEmpty(property.PostQueryFilterPathExplict) ? property.PostQueryFilterPath : property.PostQueryFilterPathExplict;
                }
            }
        }

        private static void TranslateALLSearchFilters(IEnumerable<PagedDataFilterAttribute> props, PagedDataSettings settings)
        {
            bool firstExecution = true;
            settings.SearchInALL = true;

            // Saves all previous filters (Some cases we need it, as in TabView case).
            var otherFilters = settings.Filter.Where(x => x.Property.ToUpper() != PagedDataSettings.FILTERALLIDENTIFIER).ToList();

            // Gets "All" supplied value
            var filterValue = settings.Filter.Where(x => x.Property.ToUpper() == PagedDataSettings.FILTERALLIDENTIFIER).FirstOrDefault().Value;

            // Resets all prior filter
            settings.Filter.Clear();

            foreach (var property in props)
            {
                var attrValue = property.MapsTo;

                settings.Filter.Add(new FilterSettings()
                {
                    PostQueryFilterPath = String.IsNullOrEmpty(property.PostQueryFilterPathExplict) ? property.PostQueryFilterPath : property.PostQueryFilterPathExplict,
                    Property = property.MapsTo,
                    IsExactMatch = false,
                    Value = filterValue ?? string.Empty,
                    Conjunction = firstExecution ? LogicalConjunctionEnum.AND : LogicalConjunctionEnum.OR
                });

                firstExecution = false;
            }

            // Removes all duplicates from pre existing filters.
            otherFilters.RemoveAll(x => settings.Filter.Where(y => y.Property == x.Property).Any());

            // Adds any other existing filters.
            settings.Filter = settings.Filter.Concat(otherFilters).ToList();
        }

        private static void TranslateDefaultSorting(PagedDataDefaultSortingAttribute sortingAttribute, PagedDataSettings settings)
        {
            if (settings.Sorting == null || settings.Sorting.Count == 0)
            {
                if (settings.Sorting == null)
                    settings.Sorting = new List<SortingSettings>();

                if (sortingAttribute != null)
                {
                    settings.Sorting.Add(new SortingSettings()
                    {
                        Property = sortingAttribute.Property,
                        Order = sortingAttribute.IsAscending ? SortOrderEnum.ASC : SortOrderEnum.DESC
                    });
                }
            }
        }
    }
}
