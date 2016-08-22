using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicRepository
{
    public class PagedDataResult<Entity> : IPagedDataResult<Entity> where Entity : class
    {
        public PagedDataResult(int totalRecords)
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
