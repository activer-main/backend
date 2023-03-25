using ActiverWebAPI.Interfaces.Repository;
using System.Linq.Expressions;

namespace ActiverWebAPI.Interfaces.Service;

/// <summary>
/// 泛型的 Service 介面，定義基本的 CRUD 操作
/// </summary>
/// <typeparam name="TEntity">實體類別</typeparam>
public interface IGenericService<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// 取得所有 TEntity
    /// </summary>
    /// <returns>所有 TEntity 的 List</returns>
    IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate) ;

    /// <summary>
    /// 取得所有 TEntity
    /// </summary>
    /// <returns>所有 TEntity 的 List</returns>
    IQueryable<TEntity> GetAll();

    /// <summary>
    /// 根據主鍵 ID 取得實體資料。
    /// </summary>
    /// <typeparam name="TKey">主鍵類型。</typeparam>
    /// <param name="id">主鍵值。</param>
    /// <param name="includes">包含導覽屬性的表達式。</param>
    /// <returns>符合指定主鍵 ID 的實體資料。</returns>
    TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 新增 TEntity
    /// </summary>
    /// <param name="entity">欲新增的 TEntity</param>
    void Add(TEntity entity);

    /// <summary>
    /// 更新 TEntity
    /// </summary>
    /// <param name="entity">欲更新的 TEntity</param>
    void Update(TEntity entity);

    /// <summary>
    /// 刪除 TEntity
    /// </summary>
    /// <param name="entity">欲刪除的 TEntity</param>
    void Delete(TEntity entity);

    /// <summary>
    /// 根據傳入的 ID 取得實體異步方法
    /// </summary>
    /// <typeparam name="TKey">實體 ID 的類型</typeparam>
    /// <param name="id">實體 ID</param>
    /// <returns>包含指定實體的 Task 物件</returns>
    Task<TEntity> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 新增 TEntity
    /// </summary>
    /// <param name="entity">欲新增的 TEntity</param>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// 更新 TEntity
    /// </summary>
    /// <param name="entity">欲更新的 TEntity</param>
    Task UpdateAsync(TEntity entity);

    /// <summary>
    /// 刪除 TEntity
    /// </summary>
    /// <param name="entity">欲刪除的 TEntity</param>
    Task DeleteAsync(TEntity entity);
}
