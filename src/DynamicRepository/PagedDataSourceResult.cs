using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository
{
    public class PagedDataSourceResult<Entity> : IPagedDataSourceResult<Entity> where Entity : class
    {
        public PagedDataSourceResult(int totalRecords)
        {
            this.TotalRecords = totalRecords;
        }

        public IList<Entity> Result
        {
            get; set;
        }

        public int TotalRecords
        {
            get; internal set;
        }
    }
}
