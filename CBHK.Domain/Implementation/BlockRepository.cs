using CBHK.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CBHK.Domain.Implementation
{
    public class BlockRepository<Block>(DbContext context) : IRepository<Block> where Block : class
    {
        public DbContext Context { get; set; } = context;
        public DbSet<Block> DbSet { get; set; } = context.Set<Block>();

        public async Task SaveAsync() => await context.SaveChangesAsync();

        public async Task<Block?> GetByIdAsync(params object[] keys) => await DbSet.FindAsync(keys);

        public async Task<IEnumerable<Block>> GetWhereAsync(Expression<Func<Block, bool>> predicate)
            => await DbSet.Where(predicate).ToListAsync();

        public async Task<(IEnumerable<Block> Results, int TotalCount)> GetPagedAsync(
            Expression<Func<Block, bool>>? predicate = null,
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<Block, object>>? orderBy = null,
            bool ascending = true)
        {
            IQueryable<Block> query = DbSet;

            // 条件筛选
            if (predicate is not null)
                query = query.Where(predicate);

            // 获取总数
            int totalCount = await query.CountAsync();

            // 排序
            if (orderBy is not null)
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

        public async Task AddAsync(Block entity)
        {
            await DbSet.AddAsync(entity);
            await SaveAsync(); // 可选：根据需求决定是否立即保存
        }

        public async Task UpdateAsync(Block entity)
        {
            DbSet.Update(entity);
            await SaveAsync(); // 可选：根据需求决定是否立即保存
        }

        public async Task RemoveAsync(Block entity)
        {
            DbSet.Remove(entity);
            await SaveAsync(); // 可选：根据需求决定是否立即保存
        }

        public IQueryable<Block> AsQueryable(bool tracking = true)
            => tracking ? DbSet.AsTracking() : DbSet.AsNoTracking();

        public async Task<bool> ExistsAsync(Expression<Func<Block, bool>> predicate)
            => await DbSet.AnyAsync(predicate);

        public async Task<Block?> FirstOrDefaultAsync(Expression<Func<Block, bool>> predicate)
            => await DbSet.FirstOrDefaultAsync(predicate);

        public async Task<int> CountAsync(Expression<Func<Block, bool>>? predicate = null)
            => predicate == null ?
                await DbSet.CountAsync() :
                await DbSet.CountAsync(predicate);
    }
}
