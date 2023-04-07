using ActiverWebAPI.Context;
using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Services.Repository;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ActiverDbContext _context;

    private bool _disposed;
    private Hashtable _repositories;

    /// <summary>
    /// 設定此Unit of work(UOF)的Context。
    /// </summary>
    /// <param name="context">設定UOF的context</param>
    public UnitOfWork(ActiverDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void SaveChanges()
    {
        _context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class, IEntity<TKey>
    {
        if (_repositories == null)
        {
            _repositories = new Hashtable();
        }

        var entityType = typeof(TEntity);
        if (!_repositories.ContainsKey(entityType))
        {
            var repositoryType = typeof(GenericRepository<,>).MakeGenericType(entityType, typeof(TKey));
            var repository = Activator.CreateInstance(repositoryType, _context);
            _repositories.Add(entityType, repository);
        }

        return (IRepository<TEntity, TKey>)_repositories[entityType];
    }

    /// <inheritdoc />
    public async Task LoadCollectionAsync<TEntity,TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty)
    where TProperty : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (navigationProperty == null) throw new ArgumentNullException(nameof(navigationProperty));

        var entityType = _context.Model.FindEntityType(typeof(TEntity));
        var navigation = entityType.GetNavigations().SingleOrDefault(n => n.Name == ((MemberExpression)navigationProperty.Body).Member.Name) ?? throw new ArgumentException($"Navigation property '{((MemberExpression)navigationProperty.Body).Member.Name}' not found for entity type '{typeof(TEntity)}'.");
        await _context.Entry(entity)
            .Collection(navigation.Name)
            .LoadAsync();
    }

    /// <inheritdoc />
    public async Task LoadCollectionAsync<TEntity,TProperty>(IEnumerable<TProperty> entities, Expression<Func<TProperty, IEnumerable<TEntity>>> navigationProperty)
    where TProperty : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (navigationProperty == null) throw new ArgumentNullException(nameof(navigationProperty));

        foreach (var entity in entities)
        {
            var entityType = _context.Model.FindEntityType(typeof(TEntity));
            var navigation = entityType.GetNavigations().SingleOrDefault(n => n.Name == ((MemberExpression)navigationProperty.Body).Member.Name) ?? throw new ArgumentException($"Navigation property '{((MemberExpression)navigationProperty.Body).Member.Name}' not found for entity type '{typeof(TEntity)}'.");
            await _context.Entry(entity)
                .Collection(navigation.Name)
                .LoadAsync();
        }
    }

    /// <summary>
    /// 釋放資源。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 釋放 <see cref="UnitOfWork{TContext}"/> 所持有的資源。
    /// </summary>
    /// <param name="disposing">是否正在釋放資源。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            foreach (var repository in _repositories.Values.OfType<IDisposable>())
            {
                repository.Dispose();
            }
            _context.Dispose();
        }

        _disposed = true;
    }
}