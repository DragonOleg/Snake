using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Snake.Core.Repositories.Query.Base
{
    public interface IQueryRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetList();
        IEnumerable<T> GetList(Func<T, bool> predicate);
        Task<T> GetById(long id);
        Task<T> FindBy(Expression<Func<T, bool>> predicate);
    }
}
