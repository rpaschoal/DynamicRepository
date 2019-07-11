using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Filter
{
    public class FilterSettings
    {
        /// <summary>
        /// The property to be filtered.
        /// Can be used with navigation properties (EG: myNavigationProperty.MyField)
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The value to be filtered.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// If true it will match the value exactly (==).
        /// </summary>
        public bool IsExactMatch { get; set; }

        /// <summary>
        /// How this filter is aggregated among to other existing filters. Defaults to "AND" conjunction.
        /// </summary>
        public LogicalConjunctionEnum Conjunction { get; set; } = LogicalConjunctionEnum.AND;

        /// <summary>
        /// Sorting/filter settings will be applied in memory after database execution.
        /// </summary>
        public string PostQueryFilterPath { get; set; }
    }
}
