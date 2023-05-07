using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.Service;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public virtual void Add(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Add(entity);
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity)
    {
        await _unitOfWork.Repository<TEntity, TKey>().AddAsync(entity);
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _unitOfWork.Repository<TEntity, TKey>().AddRangeAsync(entities);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Update(entity);
    }

    /// <inheritdoc />
    public virtual void Delete(TEntity entity)
    {
        _unitOfWork.Repository<TEntity, TKey>().Delete(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        _unitOfWork.Repository<TEntity, TKey>().RemoveRange(entities);
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

        var entity = await query.FirstOrDefaultAsync(e => e.Id.Equals(id));

        return entity;
    }


    /// <inheritdoc />
    public async Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty)
    where TProperty : class
    {
        await _unitOfWork.LoadCollectionAsync(entity, navigationProperty);
    }

    /// <inheritdoc />
    public async Task LoadCollectionAsync<TProperty>(IEnumerable<TProperty> entities, Expression<Func<TProperty, IEnumerable<TEntity>>> navigationProperty)
    where TProperty : class
    {
        await _unitOfWork.LoadCollectionAsync(entities, navigationProperty);
    }

    /// <inheritdoc />
    public void SetEntityState(TEntity entity, EntityState state)
    {
        _unitOfWork.Repository<TEntity, TKey>().SetEntityState(entity, state);
    }

    /// <inheritdoc />
    public IEnumerable<TEntity> GetLocal()
    {
        return _unitOfWork.Repository<TEntity, TKey>().GetLocal();
    }
}