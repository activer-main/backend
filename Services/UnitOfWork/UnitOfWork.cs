using ActiverWebAPI.Context;
using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Services.Repository;
using Microsoft.EntityFrameworkCore;
using System.Collections;

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

    /// <summary>
    /// 取得某一個Entity的Repository。
    /// 如果沒有取過，會initialise一個
    /// 如果有就取得之前initialise的那個。
    /// </summary>
    /// <typeparam name="TEntity">此Context裡面的Entity Type</typeparam>
    /// <typeparam name="TKey">Entity的key type</typeparam>
    /// <returns>Entity的Repository</returns>
    public IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class, IEntity<TKey>
    {
        if (_repositories.ContainsKey(typeof(TEntity)))
        {
            return (IRepository<TEntity, TKey>)_repositories[typeof(TEntity)];
        }

        var repository = new GenericRepository<TEntity, TKey>(_context);
        _repositories.Add(typeof(TEntity), repository);
        return repository;
    }

    /// <summary>
    /// 儲存所有異動。
    /// </summary>
    public void SaveChanges()
    {
        _context.SaveChanges();
    }

    /// <summary>
    /// 非同步儲存所有異動。
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 清除此Class的資源。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 清除此Class的資源。
    /// </summary>
    /// <param name="disposing">是否在清理中？</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }

        _disposed = true;
    }

    
}