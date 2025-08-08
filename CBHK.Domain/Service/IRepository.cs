using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CBHK.Domain.Interface
{
    public interface IRepository<TEntity> where TEntity : class
    {
        public DbContext Context { get; }
        public DbSet<TEntity> DbSet { get; }

        public Task SaveAsync();

        public Task<TEntity?> GetByIdAsync(params object[] keys);

        public Task<IEnumerable<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate);

        public Task<(IEnumerable<TEntity> Results, int TotalCount)> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true);

        public Task AddAsync(TEntity entity);

        public Task UpdateAsync(TEntity entity);

        public Task RemoveAsync(TEntity entity);

        public IQueryable<TEntity> AsQueryable(bool tracking = true)
            => tracking ? DbSet.AsTracking() : DbSet.AsNoTracking();

        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    }
}
