using Microsoft.EntityFrameworkCore;
using KitchenProject.Models.Interfaces;
using System.Linq.Expressions;

namespace KitchenProject.Models.Repositories
{
    public class GRepository<T> : IGRepository<T> where T : class
    {
        
        private readonly ApplicationDbContext _context;
        private DbSet<T> table = null;
        public GRepository(ApplicationDbContext context)
        {
            _context = context;
            table = _context.Set<T>();
        }
        public void Insert(T entity)
        {
            table.Add(entity);
        }
        public void Delete(T entity)
        {
            table.Remove(entity);
        }
        public void Update(T entity)
        {
            table.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
        public IQueryable<T> GetAll()
        {
            return table.AsQueryable().AsNoTracking();
        }

        public IQueryable<T> GetWhere(Expression<Func<T, bool>> filter)
        {
            return table.Where(filter).AsQueryable().AsNoTracking();
        }

        public async Task<T> GetByIdAsync(object id)
        {
            return await table.FindAsync(id);
        }

        public IQueryable<T> Include(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = table;
            foreach (Expression<Func<T, object>> include in includes)
                query = query.Include(include).AsNoTracking();

            return query;
        }

    }

}
