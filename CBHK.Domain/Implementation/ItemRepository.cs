using CBHK.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CBHK.Domain.Implementation
{
    public class ItemRepository<Item>(DbContext context) : IRepository<Item> where Item : class
    {
        public DbContext Context { get; } = context;
        public DbSet<Item> DbSet { get; } = context.Set<Item>();

        public async Task SaveAsync() => await context.SaveChangesAsync();

        public async Task<Item?> GetByIdAsync(params object[] keys) => await DbSet.FindAsync(keys);

        public async Task<IEnumerable<Item>> GetWhereAsync(Expression<Func<Item, bool>> predicate)
            => await DbSet.Where(predicate).ToListAsync();

        public async Task<(IEnumerable<Item> Results, int TotalCount)> GetPagedAsync(
            Expression<Func<Item, bool>>? predicate = null,
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<Item, object>>? orderBy = null,
            bool ascending = true)
        {
            IQueryable<Item> query = DbSet;

            // 条件筛选
            if (predicate != null)
                query = query.Where(predicate);

            // 获取总数
            int totalCount = await query.CountAsync();

            // 排序
            if (orderBy != null)
                query = ascending ?
                    query.OrderBy(orderBy) :
                    query.OrderByDescending(orderBy);

            // 分页
            var results = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (results, totalCount);
        }

        public async Task AddAsync(Item entity)
        {
            await DbSet.AddAsync(entity);
            await SaveAsync(); // 可选：根据需求决定是否立即保存
        }

        public async Task UpdateAsync(Item entity)
        {
            DbSet.Update(entity);
            await SaveAsync(); // 可选：根据需求决定是否立即保存
        }

        public async Task RemoveAsync(Item entity)
        {
            DbSet.Remove(entity);
            await SaveAsync(); // 可选：根据需求决定是否立即保存
        }

        public IQueryable<Item> AsQueryable(bool tracking = true)
            => tracking ? DbSet.AsTracking() : DbSet.AsNoTracking();

        public async Task<bool> ExistsAsync(Expression<Func<Item, bool>> predicate)
            => await DbSet.AnyAsync(predicate);

        public async Task<Item?> FirstOrDefaultAsync(Expression<Func<Item, bool>> predicate)
            => await DbSet.FirstOrDefaultAsync(predicate);

        public async Task<int> CountAsync(Expression<Func<Item, bool>>? predicate = null)
            => predicate == null ?
                await DbSet.CountAsync() :
                await DbSet.CountAsync(predicate);
    }
}