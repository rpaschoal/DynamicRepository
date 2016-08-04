using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Mapping
{
    /// <summary>
    /// Use this on API endpoint or MVC controller to configure default sorting property for paged data source results.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PagedDataSourceDefaultSortingAttribute : Attribute
    {
        public PagedDataSourceDefaultSortingAttribute(string property, bool isAscending = false)
        {
            this.Property = property;
            this.IsAscending = isAscending;
        }

        /// <summary>
        /// The Entity property which will be used as default sorting.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// If true, means it will be sorted by ascending.
        /// </summary>
        /// <remarks>
        /// Default sorting order is Descending.
        /// </remarks>
        public bool IsAscending { get; set; }
    }
}
