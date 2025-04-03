using CBHK.Domain.Interface;
using CBHKShared.ContextModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CBHK.Domain.Implementation
{
    public class BlockRepository<Block>(DbContext context) : IRepository<Block> where Block : class
    {
        protected readonly DbContext _context = context;
        protected readonly DbSet<Block> _dbSet = context.Set<Block>();

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
