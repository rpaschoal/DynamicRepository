using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository
{
    /// <summary>
    /// Response model for <see cref="DynamicRepository.IRepository{Key, Entity}.GetPagedData(Filter.PagedDataSettings)(Filter.PagedDataSettings)"/>.
    /// </summary>
    /// <typeparam name="Entity">Entity model type. This is your DbContext or POCO entity type.</typeparam>
    public interface IPagedDataResult<Entity> where Entity : class
    {
        /// <summary>
        /// Identifies how many records in paged data source query regardless of current page and page size.
        /// </summary>
        int TotalRecords { get; }

        /// <summary>
        /// Collection of items after filtered and paged.
        /// </summary>
        IList<Entity> Result { get; set; }
    }
}
