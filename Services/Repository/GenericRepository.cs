using ActiverWebAPI.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services.Repository;

public class GenericRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private DbContext Context { get; set; }

    /// <summary>
    /// 建構EF一個Entity的Repository，需傳入此Entity的Context。
    /// </summary>
    /// <param name="inContext">Entity所在的Context</param>
    public GenericRepository(DbContext inContext)
    {
        Context = inContext;
    }

    /// <inheritdoc />
    public void Add(TEntity entity)
    {
        Context.Set<TEntity>().Add(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
    {
        await Context.Set<TEntity>().AddAsync(entity);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await Context.Set<TEntity>().AddRangeAsync(entities);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
    }

    /// <inheritdoc />
    public void Update(TEntity entity, Expression<Func<TEntity, object>>[] updateProperties)
    {
        Context.Entry(entity).State = EntityState.Unchanged;

        if (updateProperties != null)
        {
            foreach (var property in updateProperties)
            {
                Context.Entry(entity).Property(property).IsModified = true;
            }
        }
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        Context.Entry(entity).State = EntityState.Deleted;
    }

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        Context.Set<TEntity>().RemoveRange(entities);
    }

    /// <inheritdoc />
    public IQueryable<TEntity> Query()
    {
        return Context.Set<TEntity>().AsQueryable();
    }

    /// <inheritdoc />
    public TEntity GetById(TKey id)
    {
        return Context.Set<TEntity>().Find(id);
    }

    /// <inheritdoc />
    public async Task<TEntity> GetByIdAsync(TKey id)
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    /// <inheritdoc />
    public IQueryable<TEntity> GetAll()
    {
        return Context.Set<TEntity>().AsQueryable();
    }

    /// <inheritdoc />
    public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate)
    {
        return Context.Set<TEntity>().Where(predicate).AsQueryable();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> CollectionAsync<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty)
    {
        var query = Context.Set<TEntity>().AsQueryable();
        var result = await query.Include(navigationProperty).ToListAsync();
        return result;
    }

    /// <inheritdoc />
    public void SetEntityState(TEntity entity, EntityState state)
    {
        Context.Entry(entity).State = state;
    }

    /// <inheritdoc />
    public IEnumerable<TEntity> GetLocal()
    {
        return Context.Set<TEntity>().Local.AsEnumerable();
    }
}
