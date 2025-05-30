using CBHK.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CBHK.Domain.Implementation
{
    public class BlockRepository<Block>(DbContext context) : IRepository<Block> where Block : class
    {
        protected readonly DbContext context = context;
        protected readonly DbSet<Block> dbSet = context.Set<Block>();

        public Task AddAsync(Block entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Expression<Func<Block, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Block>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public void Remove(Block entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Block entity)
        {
            throw new NotImplementedException();
        }
    }
}
