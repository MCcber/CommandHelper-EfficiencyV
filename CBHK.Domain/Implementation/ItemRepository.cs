using CBHK.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CBHK.Domain.Implementation
{
    public class ItemRepository<Item>(DbContext context) : IRepository<Item> where Item : class
    {
        protected readonly DbContext _context = context;
        protected readonly DbSet<Item> _dbSet = context.Set<Item>();

        public async Task<Item> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<Item>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task AddAsync(Item entity) => await _dbSet.AddAsync(entity);

        public void Update(Item entity) => _dbSet.Update(entity);

        public void Remove(Item entity) => _dbSet.Remove(entity);

        public async Task<bool> ExistsAsync(Expression<Func<Item, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);
    }
}
