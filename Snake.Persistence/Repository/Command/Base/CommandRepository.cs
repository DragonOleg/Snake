using Microsoft.EntityFrameworkCore;
using Snake.Core.Repositories.Command.Base;
using Snake.Persistence.Data;
using Snake.Domain;
using Snake.Core;
using Snake.Persistence.Helpers;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Snake.Persistence.Repository.Command.Base
{
    public class CommandRepository<T> : ICommandRepository<T> where T : BaseEntity
    {
        private readonly SnakeDbContext _context;

        public CommandRepository(SnakeDbContext context)
        {
            _context = context;
        }
        
        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _context.Entry(entity).State = EntityState.Detached;
        }

        public virtual async Task DeleteAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
            _context.Entry(entity).State = EntityState.Detached;
        }

        public virtual async Task DeleteAsync(long id)
        {
            var entity = await _context.Set<T>().FindAsync(id);

            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            await DeleteAsync(entity);
        }

        public virtual void Refresh(T entity)
        {
            _context.Entry<T>(entity).Reload();
        }

        public virtual async Task ClearAsync()
        {
            _context.Set<T>().Clear();
            await _context.SaveChangesAsync();
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            _context.Set<T>().AddRange(entities);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
            await _context.SaveChangesAsync();

            foreach (var entity in entities)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }
        }
    }
}