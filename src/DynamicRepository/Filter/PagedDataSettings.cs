using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Filter
{
    /// <summary>
    /// Filter, sorting and paging settings for advanced searchs.
    /// </summary>
    public class PagedDataSettings
    {
        /// <summary>
        /// When the <see cref="FilterSettings"/> has this value, means it should be searched in all fields.
        /// </summary>
        public const string FILTERALLIDENTIFIER = "ALL";

        /// <summary>
        /// Desired filters to be applied to database.
        /// </summary>
        public IList<FilterSettings> Filter { get; set; } = new List<FilterSettings>();

        /// <summary>
        /// Desired sorting order of the data.
        /// </summary>
        public IList<SortingSettings> Order { get; set; } = new List<SortingSettings>();

        /// <summary> 
        /// Page index. Default is 1.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Total result of items to be returned on this batch.
        /// Default set to 20.
        /// </summary>
        public int TotalPerPage { get; set; } = 20;

        /// <summary>
        /// Identifies if user is search in "ALL" fields.
        /// </summary>
        public bool SearchInALL { get; set; }
    }
}
