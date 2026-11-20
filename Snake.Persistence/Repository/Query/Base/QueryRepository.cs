using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Snake.Core.Repositories.Query.Base;
using Snake.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Snake.Persistence.Repository.Query.Base
{
    public class QueryRepository<T> : DbConnector, IQueryRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet;
        private readonly SnakeDbContext _context;

        public SnakeDbContext SnakeContext
        {
            get { return _context; }
        }

        public QueryRepository(IConfiguration configuration,
            SnakeDbContext context)
            : base(configuration)
        {
            _dbSet = context.Set<T>();
            _context = context;
        }

        public async Task<IEnumerable<T>> GetList()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public IEnumerable<T> GetList(Func<T, bool> predicate)
        {
            return _dbSet.AsNoTracking().AsEnumerable().Where(predicate).ToList();
        }

        public virtual async Task<T> GetById(long id)
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity == null)
            {
                return await Task.FromResult(default(T));
            }

            return entity;
        }

        public async Task<T> FindBy(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }
    }
}