using Microsoft.EntityFrameworkCore;
using KitchenProject.Models.Entities;
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
            var entry = _context.Entry(entity);
            entry.State = EntityState.Modified;
            if (entity is BaseEntity)
            {
                entry.Property(nameof(BaseEntity.Created)).IsModified = false;
            }
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
            IQueryable<T> query = table.AsNoTracking();
            foreach (Expression<Func<T, object>> include in includes)
            {
                if (TryGetNavigationPath(include, out string path))
                    query = query.Include(path);
                else
                    query = query.Include(include);
            }

            return query;
        }

        private static bool TryGetNavigationPath(Expression<Func<T, object>> include, out string path)
        {
            Expression? body = include.Body;
            if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
                body = unary.Operand;

            var parts = new Stack<string>();
            while (body is MemberExpression member)
            {
                parts.Push(member.Member.Name);
                body = member.Expression;
                if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } innerUnary)
                    body = innerUnary.Operand;
            }

            if (body is ParameterExpression && parts.Count > 1)
            {
                path = string.Join(".", parts);
                return true;
            }

            path = string.Empty;
            return false;
        }

    }

}
