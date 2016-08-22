using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Mapping
{
    /// <summary>
    /// Use this attribute to decorate an API (Or MVC) endpoint to create a contract between UI filter settings and API (Backend) filter settings.
    /// </summary>
    /// <example>
    /// [PagedDataSourceAdapter("MyProperty", "CustomFilterForProperty", InMemoryPath = "Items.PropertyOne|Items.PropertyTwo")]
    /// 
    /// * Supports multiple decorators to one single endpoint;
    /// * First parameters indicates the property name value that UI or API consumer will send to the endpoint;
    /// * Second parameter indicates how API names the property (Can be the name of the property on the entity, or a custom name setting IF using ExtraPagedDataSourceFilters);
    /// * Third parameter applies filter on post query result. 
    ///   Useful to filter a subcollection within the result, since EF can't remove a subset of data even if you filter based on it. 
    ///   Allows piping, which means you can filter by many properties of the mapped result set from DB only by using the char '|'
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class PagedDataSourceAdapterAttribute : Attribute
    {
        public PagedDataSourceAdapterAttribute(string propFrom, string mapsTo, string inMemoryPath = "")
        {
            this.PropFrom = propFrom;
            this.MapsTo = mapsTo;
            this.InMemoryPath = inMemoryPath;
        }

        /// <summary>
        /// The supplied UI value to filter.
        /// </summary>
        public string PropFrom { get; set; }

        /// <summary>
        /// The identifier property to be mapped to.
        /// </summary>
        /// <example>
        /// MyDbNavigationProperty.MyValueProperty
        /// </example>
        public string MapsTo { get; set; }

        /// <summary>
        /// Use this to filter a child collection within the final result set.
        /// </summary>
        /// <remarks>
        /// This property applies both Sorting AND Filtering based on its value.
        /// </remarks>
        public string InMemoryPath { get; set; }
    }
}
