using ActiverWebAPI.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ActiverWebAPI.Interfaces.Service;

/// <summary>
/// 泛型的 Service 介面，定義基本的 CRUD 操作
/// </summary>
/// <typeparam name="TEntity">實體類別</typeparam>
public interface IGenericService<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// 新增 TEntity
    /// </summary>
    /// <param name="entity">欲新增的 TEntity</param>
    void Add(TEntity entity);

    /// <summary>
    /// 新增 TEntity 異步方法
    /// </summary>
    /// <param name="entity">欲新增的 TEntity</param>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// 加入實體的集合
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

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
    /// 儲存異動
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// 非同步儲存異動
    /// </summary>
    /// <returns></returns>
    Task SaveChangesAsync();

    /// <summary>
    /// 獲取所有 <typeparamref name="TEntity"/> 實體，可選擇性載入關聯實體。
    /// </summary>
    /// <param name="includes">包含需要載入的關聯實體的 <see cref="Expression{TDelegate}"/> 陣列。</param>
    /// <returns>包含所有 <typeparamref name="TEntity"/> 實體的 <see cref="IQueryable{T}"/> 物件。</returns>
    IQueryable<TEntity> GetAll(params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 取得符合條件的所有 <typeparamref name="TEntity"/> 資料，並且包含指定的關聯屬性
    /// </summary>
    /// <param name="predicate">篩選條件</param>
    /// <param name="includes">要包含的關聯屬性</param>
    /// <returns>符合條件的所有 TEntity 資料</returns>
    IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 根據主鍵 ID 取得實體資料異步方法。
    /// </summary>
    /// <typeparam name="TKey">主鍵類型。</typeparam>
    /// <param name="id">主鍵值。</param>
    /// <param name="includes">包含導覽屬性的表達式。</param>
    /// <returns>符合指定主鍵 ID 的實體資料。</returns>
    TEntity? GetById(TKey id, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 根據傳入的 ID 取得實體異步方法
    /// </summary>
    /// <typeparam name="TKey">實體 ID 的類型</typeparam>
    /// <param name="id">實體 ID</param>
    /// <returns>包含指定實體的 Task 物件</returns>
    Task<TEntity>? GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 非同步載入指定實體的集合屬性。
    /// </summary>
    /// <typeparam name="TProperty">集合屬性的型別。</typeparam>
    /// <param name="entity">要載入集合屬性的實體。</param>
    /// <param name="navigationProperty">指定要載入的集合屬性。</param>
    /// <returns>表示非同步載入操作的 <see cref="Task"/> 物件。</returns>
    Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty) where TProperty : class;

    /// <summary>
    /// 非同步加載多個實體對象集合的導覽屬性，透過提供的導覽屬性表達式和一個實體對象集合。
    /// </summary>
    /// <typeparam name="TProperty">導覽屬性類型。</typeparam>
    /// <param name="entities">實體對象集合。</param>
    /// <param name="navigationProperty">導覽屬性表達式。</param>
    /// <returns>異步操作任務。</returns>
    /// <exception cref="ArgumentNullException">當 entities 或 navigationProperty 為 null 時拋出。</exception>
    /// <exception cref="ArgumentException">當 navigationProperty 表達式不是有效的導覽屬性表達式時拋出。</exception>
    Task LoadCollectionAsync<TProperty>(IEnumerable<TProperty> entities, Expression<Func<TProperty, IEnumerable<TEntity>>> navigationProperty) where TProperty : class;

    /// <summary>
    /// 設定實體狀態
    /// </summary>
    /// <param name="entity">實體</param>
    /// <param name="state">狀態</param>
    void SetEntityState(TEntity entity, EntityState state);

    /// <summary>
    /// 取得已在當前 DbContext 中追蹤的 TEntity 實體集合。
    /// </summary>
    /// <returns>已在當前 DbContext 中追蹤的 TEntity 實體集合。</returns>
    IEnumerable<TEntity> GetLocal();
}
