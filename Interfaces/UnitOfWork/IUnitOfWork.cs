using ActiverWebAPI.Interfaces.Repository;

namespace ActiverWebAPI.Interfaces.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 儲存所有異動
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// 非同步儲存所有異動
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// 取得某一個Entity的Repository。
    /// 如果沒有取過，會initialise一個
    /// 如果有就取得之前initialise的那個。
    /// </summary>
    /// <typeparam name="T">此Context裡面的Entity Type</typeparam>
    /// <returns>Entity的Repository</returns>
    IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class, IEntity<TKey>;
}
