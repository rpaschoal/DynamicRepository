using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Filter
{
    /// <summary>
    /// Desired ordering of the data in a paged data source result set.
    /// </summary>
    public sealed class SortingSettings
    {
        /// <summary>
        /// The filter to which property the sorting needs to be applied to.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The order which you want to apply to this sort property.
        /// Available values are:
        /// * <see cref="SortOrderEnum.ASC"/>
        /// * <see cref="SortOrderEnum.DESC"/>
        /// </summary>
        public SortOrderEnum Order { get; set; }
    }
}
