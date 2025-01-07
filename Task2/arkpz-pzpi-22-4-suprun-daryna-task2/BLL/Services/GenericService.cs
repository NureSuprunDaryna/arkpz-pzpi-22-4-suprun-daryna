using System.Linq.Expressions;
using BLL.Interfaces;
using Core.Models;
using DAL.Repositories;

namespace BLL.Services
{
    public class GenericService<T> : IGenericService<T> where T : class
    {
        protected readonly UnitOfWork _unitOfWork;
        protected readonly IRepository<T> _repository;
        public GenericService(UnitOfWork unitOfWork, IRepository<T> repository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
        }

        public async Task<Result<T>> GetById(params object[] id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);

                if (entity == null)
                {
                    return new Result<T>(false);
                }

                return new Result<T>(true, entity);
            }
            catch (Exception)
            {
                return new Result<T>(false);
            }
        }

        public async Task<Result<List<T>>> GetByPredicate(Expression<Func<T, bool>> filter = null,
        Expression<Func<IQueryable<T>, IOrderedQueryable<T>>> orderBy = null)
        {
            try
            {
                var entityList = await _repository.GetAsync(filter, orderBy);

                if (entityList == null)
                {
                    return new Result<List<T>>(false);
                }

                return new Result<List<T>>(true, entityList);
            }
            catch (Exception)
            {
                return new Result<List<T>>(false);
            }
        }
    }
}
