using ActiverWebAPI.Interfaces.Repository;
using System.Linq.Expressions;

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

    /// <summary>
    /// 非同步載入指定實體的導覽屬性。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <typeparam name="TProperty">導覽屬性型別。</typeparam>
    /// <param name="entity">指定要載入導覽屬性的實體。</param>
    /// <param name="navigationProperty">表示要載入的導覽屬性。</param>
    /// <returns>非同步操作。</returns>
    Task LoadCollectionAsync<TEntity, TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty) where TProperty : class;

    /// <summary>
    /// 非同步從資料庫中載入指定集合中所有實體的集合導覽屬性。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <typeparam name="TProperty">導覽屬性型別。</typeparam>
    /// <param name="entities">指定要載入導覽屬性的實體集合。</param>
    /// <param name="navigationProperty">指定要載入的導覽屬性。</param>
    /// <returns>表示異步作業的 Task。</returns>
    Task LoadCollectionAsync<TEntity, TProperty>(IEnumerable<TProperty> entities, Expression<Func<TProperty, IEnumerable<TEntity>>> navigationProperty) where TProperty : class;
}
