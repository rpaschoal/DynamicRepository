using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Annotation
{
    /// <summary>
    /// Defines filter adapting rules of what comes of an API endpoint consumer and what gets translated to be filtered
    /// by the <see cref="DataPager{Key, Entity}"/> class.
    /// </summary>
    /// <remarks>
    /// This adapter helps translating contracts between consumers and whatever it is the application DB Schema Model.
    /// This ensure that any UI won't break if you apply changes to the DB Schema in the future (You only have to change the mapping here).
    /// </remarks>
    /// <example>
    /// FROM => TO
    /// 
    /// [PagedDataSourceAdapter("Id", "MyEntityIdProperty")]
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class DataPagerFilterAttribute : Attribute
    {
        public DataPagerFilterAttribute(string mapsFrom, string mapsTo, string postFilterPath = "", string inMemoryFilterExplict = "")
        {
            this.MapsFrom = mapsFrom;
            this.MapsTo = mapsTo;
            this.PostFilterPath = postFilterPath;
            this.PostFilterExplict = PostFilterExplict;
        }

        /// <summary>
        /// The identification of the property which the API consumer wants to filter.
        /// This may be for example what the UI sends to be filtered.
        /// </summary>
        public string MapsFrom { get; set; }

        /// <summary>
        /// This is the application schema identifier. 
        /// The <see cref="MapsFrom"/> will be translated and used on the <see cref="DataPager{Key, Entity}"/> with whatever is defined in here.  
        /// </summary>
        /// <example>
        /// MyModelEntity.MyProperty
        /// </example>
        public string MapsTo { get; set; }

        /// <summary>
        /// This is usefull for collection properties search/sorting after hitting database.
        /// Must be used with inner collection filtering.
        /// </summary>
        /// <remarks>
        /// This property applies both Sorting AND Filtering based on its value.
        /// </remarks>
        public string PostFilterPath { get; set; }

        /// <summary>
        /// Applies post query hit filtering to a resulting collection of a LINQ query.
        /// </summary>
        /// <remarks>
        /// Only use this configuration if you need the "Filter" path to be different than the sorting path.
        /// 
        /// * Some cases like Nullable Datetime Fields require this, as the search engine can't apply a 
        /// "ToString()" call in a nullable datetime in order to filter. This property was created to enable the sorting to be applied
        /// on a DTO property specified by <see cref="PostFilterPath"/>, and the filter to be explicit set to the value here. 
        /// 
        /// * If this is left as the class default value (String.Empty), filtering and sorting will be applied based on <see cref="PostFilterPath"/>.  
        /// </remarks>
        public string PostFilterExplict { get; set; }

        /// <summary>
        /// When set to true, this attribute values won't be applied for filtering.
        /// It will be applied only for sorting operations.
        /// </summary>
        public bool IgnoreOnSearch { get; set; }
    }
}
