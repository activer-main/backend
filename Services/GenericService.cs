using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.Service;
using ActiverWebAPI.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services;

/// <summary>
/// 泛型的 Service 實作類別，提供基本的 CRUD 操作
/// </summary>
/// <typeparam name="TEntity">實體類別</typeparam>
public class GenericService<TEntity, TKey> : IGenericService<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    private readonly IUnitOfWork _unitOfWork;

    public GenericService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _unitOfWork.Repository<TEntity, TKey>().Query();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query.Where(predicate);
    }

    /// <inheritdoc />
    public IQueryable<TEntity> GetAll(params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _unitOfWork.Repository<TEntity, TKey>().Query();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    /// <inheritdoc />
    public TEntity? GetById(TKey id, params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _unitOfWork.Repository<TEntity, TKey>().Query();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query.FirstOrDefault(e => e.Id.Equals(id));
    }

    /// <inheritdoc />
    public async Task<TEntity>? GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _unitOfWork.Repository<TEntity, TKey>().Query();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id));
    }

    /// <inheritdoc />
    public void Add(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Add(entity);
        SaveChanges();
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Delete(entity);
        SaveChanges();
    }

    /// <inheritdoc />
    public void SaveChanges()
    {
        _unitOfWork.SaveChanges();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Update(entity);
        SaveChanges();
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Add(entity);
        await SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Update(entity);
        await SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Delete(entity);
        await SaveChangesAsync();
    }
}