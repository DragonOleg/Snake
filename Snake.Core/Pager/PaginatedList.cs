using System.Collections.Generic;
using System.Linq;

namespace Snake.Core
{
    public class PaginatedList<T> : List<T>
    {
        public int TotalRecords
        {
            get;
            private set;
        }

        public PaginatedList()
        {
        }

        public PaginatedList(IList<T> source, int pageIndex, int pageSize)
        {
            TotalRecords = source.Count;
            IList<T> collection = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            AddRange(collection);
        }        
    }
}
