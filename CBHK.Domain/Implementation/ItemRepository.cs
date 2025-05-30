using CBHK.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CBHK.Domain.Implementation
{
    public class ItemRepository<Item>(DbContext context) : IRepository<Item> where Item : class
    {
        protected readonly DbContext context = context;
        protected readonly DbSet<Item> dbSet = context.Set<Item>();

        public async Task<Item> GetByIdAsync(int id) => await dbSet.FindAsync(id);

        public async Task<IEnumerable<Item>> GetAllAsync() => await dbSet.ToListAsync();

        public async Task AddAsync(Item entity) => await dbSet.AddAsync(entity);

        public void Update(Item entity) => dbSet.Update(entity);

        public void Remove(Item entity) => dbSet.Remove(entity);

        public async Task<bool> ExistsAsync(Expression<Func<Item, bool>> predicate)
            => await dbSet.AnyAsync(predicate);
    }
}