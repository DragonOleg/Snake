using Snake.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snake.Core.Repositories.Command.Base
{
    public interface ICommandRepository<T> where T : BaseEntity
    {
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteAsync(long id);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task ClearAsync();
        void Refresh(T entity);
    }
}
