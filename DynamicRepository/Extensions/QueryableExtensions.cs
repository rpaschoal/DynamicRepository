using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Extensions
{
    internal static class QueryableExtensions
    {
        internal delegate IList OnSelectCallback(IList fetchedResult);

        /// <summary>
        /// Converts an IQueryable result to a paged result set and applies post query filters to it.
        /// </summary>
        /// <typeparam name="T">The entity type which is being queried.</typeparam>
        /// <param name="self">Self Iqueryable instance (Extension).</param>
        /// <param name="totalRecords">Total records in database for this entity based on previous paged query.</param>
        /// <param name="callback">Callback to execute code on memory projection selected by adapter.</param>
        /// <returns>WrapperEnumerator instance.</returns>
        internal static IPagedDataResult<T> BuildUpResult<T>(this IQueryable<T> self, int totalRecords, OnSelectCallback callback = null) where T : class
        {
            return new PagedDataResult<T>(totalRecords)
            {
                Result = (IList<T>)callback(self.ToList())
            };
        }
    }
}
