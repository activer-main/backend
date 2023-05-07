using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ActiverWebAPI.Interfaces.Repository;

public interface IRepository<TEntity, Tkey> where TEntity : IEntity<Tkey>
{
    /// <summary>
    /// 新增一筆資料。
    /// </summary>
    /// <param name="entity">要新增的Entity</param>
    void Add(TEntity entity);

    /// <summary>
    /// 非同步方式新增實體到資料庫
    /// </summary>
    /// <param name="entity">欲新增的實體</param>
    /// <returns>代表非同步新增作業的工作 (Task) 物件</returns>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// 新增多筆實體到資料庫。
    /// </summary>
    /// <param name="entities">要新增到資料庫的實體集合。</param>
    /// <returns>表示執行新增作業的非同步作業。</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// 更新一筆資料的內容。
    /// </summary>
    /// <param name="entity">要更新的內容</param>
    void Update(TEntity entity);

    /// <summary>
    /// 更新一筆Entity的內容。只更新有指定的Property。
    /// </summary>
    /// <param name="entity">要更新的內容。</param>
    /// <param name="updateProperties">需要更新的欄位。</param>
    void Update(TEntity entity, Expression<Func<TEntity, object>>[] updateProperties);

    /// <summary>
    /// 刪除一筆資料內容。
    /// </summary>
    /// <param name="entity">要被刪除的Entity。</param>
    void Delete(TEntity entity);

    /// <summary>
    /// 刪除一個實體集合。
    /// </summary>
    /// <param name="entities">要刪除的實體集合。</param>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 取得可查詢的資料集合。
    /// </summary>
    /// <returns>可查詢的資料集合。</returns>
    IQueryable<TEntity> Query();

    /// <summary>
    /// 以Id查找內容
    /// </summary>
    /// <param name="id">要取得的Id</param>
    /// <returns>取得的內容</returns>
    TEntity GetById(Tkey id);

    /// <summary>
    /// 以Id查找內容 Async
    /// </summary>
    /// <param name="id">要取得的Id</param>
    /// <returns>取得的內容</returns>
    Task<TEntity> GetByIdAsync(Tkey id);

    /// <summary>
    /// 取得 Entity 全部筆數的 IQueryable。
    /// </summary>
    /// <returns>Entity 全部比數的 IQueryable</returns>
    IQueryable<TEntity> GetAll();

    /// <summary>
    /// 取得符合指定條件的所有實體資料
    /// </summary>
    /// <param name="predicate">指定條件的表達式</param>
    /// <returns>符合指定條件的所有實體資料</returns>
    IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 取得包含指定導覽屬性的所有實體集合。
    /// </summary>
    /// <param name="includes">導覽屬性的表達式</param>
    /// <returns>包含指定導覽屬性的所有實體集合</returns>
    Task<IEnumerable<TEntity>> CollectionAsync<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty);

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
