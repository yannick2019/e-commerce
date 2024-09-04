using Microsoft.EntityFrameworkCore;
using YanikoRestaurant.Data;
using YanikoRestaurant.Models;

namespace YanikoRestaurant.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); ;
            _dbSet = context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id, QueryOptions<T> options)
        {
            if (_dbSet == null)
            {
                throw new InvalidOperationException($"DbSet for {typeof(T).Name} is not initialized. Check your DbContext setup.");
            }

            IQueryable<T> query = _dbSet ?? throw new InvalidOperationException("DbSet is not initialized.");

            if (options?.HasWhere == true && options.Where != null)
            {
                query = query.Where(options.Where);
            }

            if (options?.HasOrderBy == true && options.OrderBy != null)
            {
                query = query.OrderBy(options.OrderBy!);
            }

            if (options?.GetIncludes() != null)
            {
                foreach (string include in options.GetIncludes())
                {
                    if (!string.IsNullOrEmpty(include))
                    {
                        query = query.Include(include);
                    }
                }
            }

            var key = _context.Model?.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties.FirstOrDefault() ?? throw new InvalidOperationException("Primary key not found for the entity.");

            string primaryKeyName = key!.Name;

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, primaryKeyName) == id) ?? throw new InvalidOperationException("Entity not found.");
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Update(entity);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);

            _dbSet.Remove(entity!);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAllByIdAsync<TKey>(TKey id, string propertyName, QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;

            if (options.HasWhere)
            {
                query = query.Where(options.Where!);
            }


            if (options.HasOrderBy)
            {
                query = query.OrderBy(options.OrderBy!);
            }

            foreach (string include in options.GetIncludes())
            {
                query = query.Include(include);
            }
            // Filter by the specified property name and id
            query = query.Where(e => EF.Property<TKey>(e, propertyName)!.Equals(id));

            return await query.ToListAsync();

        }
    }
}