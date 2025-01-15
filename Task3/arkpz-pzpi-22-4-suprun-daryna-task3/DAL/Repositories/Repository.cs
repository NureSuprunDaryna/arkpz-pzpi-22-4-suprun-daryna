using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DAL.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly ILogger<Repository<T>> _logger;

        public Repository(ApplicationContext context, ILogger<Repository<T>> logger)
        {
            _context = context;
            _dbSet = _context.Set<T>();
            _logger = logger;
        }

        public async Task AddAsync(T item)
        {
            try
            {
                _logger.LogInformation("Adding {entityName} to DbSet", typeof(T).Name);

                await _dbSet.AddAsync(item);

                _logger.LogInformation("{entityName} added to DbSet successfully!", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to add {entityName} to DbSet! Error: {errorMessage}",
                    typeof(T).Name, ex.Message);

                throw new Exception($"Exception message:{ex.Message}");
            }
        }

        public async Task DeleteAsync(T item)
        {
            try
            {
                _logger.LogInformation("Deleting {entityName} from DbSet", typeof(T).Name);

                if (_context.Entry(item).State == EntityState.Detached)
                {
                    _dbSet.Attach(item);
                }

                _dbSet.Remove(item);

                _logger.LogInformation("{entityName} deleted from DbSet successfully!", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete {entityName} from DbSet! Error: {errorMessage}",
                    typeof(T).Name, ex.Message);

                throw new Exception($"Exception message:{ex.Message}");
            }
        }

        public async Task UpdateAsync(T item)
        {
            try
            {
                _logger.LogInformation("Updating {entityName}", typeof(T).Name);

                _dbSet.Attach(item);
                _context.Entry(item).State = EntityState.Modified;

                _logger.LogInformation("{entityName} updated successfully!", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update {entityName}! Error: {errorMessage}",
                    typeof(T).Name, ex.Message);

                throw new Exception($"Exception message: {ex.Message}");
            }
        }

        public async Task<T> GetByIdAsync(params object[] ids)
        {
            try
            {
                _logger.LogInformation("Retrieving {entityName}", typeof(T).Name);

                return await _dbSet.FindAsync(ids);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve {entityName}! Error: {errorMessage}",
                    typeof(T).Name, ex.Message);

                throw new Exception($"Exception message:{ex.Message}");
            }
        }

        public virtual async Task<List<T>> GetAsync(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderBy = null)
        {
            try
            {
                _logger.LogInformation("Retrieving {entityName}s", typeof(T).Name);

                IQueryable<T> query = _dbSet;

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                if (orderBy != null)
                {
                    return await orderBy.Compile()(query).ToListAsync();
                }
                else
                {
                    return await query.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve {entityName}s! Error: {errorMessage}",
                    typeof(T).Name, ex.Message);

                throw new Exception($"Exception message:{ex.Message}");
            }
        }
    }
}
