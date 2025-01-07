using System.Linq.Expressions;
using Core.Models;

namespace BLL.Interfaces
{
    public interface IGenericService<T> where T : class
    {
        Task<Result<T>> GetById(params object[] id);
        Task<Result<List<T>>> GetByPredicate(Expression<Func<T, bool>> filter = null,
            Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderBy = null);
    }
}
