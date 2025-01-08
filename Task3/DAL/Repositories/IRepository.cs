using System.Linq.Expressions;

namespace DAL.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(params object[] ids);
        Task<List<T>> GetAsync(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderBy = null);
        Task AddAsync(T entity);
        Task DeleteAsync(T entity);
        Task UpdateAsync(T entity);
    }
}
