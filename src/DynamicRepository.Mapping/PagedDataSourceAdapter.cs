using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository.Mapping
{
    /// <summary>
    /// Translates API public filter settings to possible back-end properties.
    /// </summary>
    /// <remarks>
    /// By using these adapters it helps building an API that is less prompt to cause UI to break,
    /// since after it Entity properties can be renamed without affecting how UI name or interface with them.
    /// </remarks>
    public static class PagedDataSourceAdapter
    {
    }
}
