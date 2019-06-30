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
        /// When the <see cref="FilterSettings"/> contains a setting with this value, means it should be searched in all fields.
        /// </summary>
        internal const string FILTERALLIDENTIFIER = "ALL";

        /// <summary>
        /// Desired filters to be applied to database.
        /// </summary>
        /// <remarks>
        /// Use a <see cref="FilterSettings.Property"/> string value with <see cref="FILTERALLIDENTIFIER"/> value 
        /// to search in all fields definied by one or many <see cref="DynamicRepository.Annotation.PagedDataFilterAttribute"/>.
        /// </remarks>
        public IList<FilterSettings> Filter { get; set; } = new List<FilterSettings>();

        /// <summary>
        /// Desired sorting order of the data.
        /// </summary>
        public IList<SortingSettings> Sorting { get; set; } = new List<SortingSettings>();

        /// <summary> 
        /// Page index. Default is 1. Index starts on 1.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Total result of items to be returned on this batch. Defaults to 20 items.
        /// </summary>
        public int TotalPerPage { get; set; } = 20;

        /// <summary>
        /// Identifies if user wants to search in all fields definied by one or many <see cref="DynamicRepository.Annotation.PagedDataFilterAttribute"/>.
        /// </summary>
        public bool SearchInALL { get; set; }
    }
}
