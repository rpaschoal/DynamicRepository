using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Annotation
{
    /// <summary>
    /// This attribute sets the default sorting property of the paged data search result.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class DataPagerSortingAttribute : Attribute
    {
        public DataPagerSortingAttribute(string property, bool isAscending = false)
        {
            this.Property = property;
            this.IsAscending = isAscending;
        }

        /// <summary>
        /// The entity model property which will be used as default sorting for the paged data source result set.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Set this to true to get ascending sort by the chosen <see cref="Property"/> .
        /// </summary>
        public bool IsAscending { get; set; }
    }
}
