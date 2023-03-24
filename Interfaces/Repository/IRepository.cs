using System.Linq.Expressions;

namespace ActiverWebAPI.Interfaces.Repository;

public interface IRepository<TEntity>
{
    /// <summary>
    /// 新增一筆資料。
    /// </summary>
    /// <param name="entity">要新增的Entity</param>
    void Add(TEntity entity);

    /// <summary>
    /// 以Id查找內容。
    /// </summary>
    /// <param name="id">要取得的Id</param>
    /// <returns>取得的內容。</returns>
    TEntity GetById(object id);
    
    /// <summary>
    /// 取得 Entity 全部筆數的 IQueryable。
    /// </summary>
    /// <returns>Entity 全部比數的 IQueryable</returns>
    IQueryable<TEntity> GetAll();

    /// <summary>
    /// 取得 Entity 全部筆數的 IQueryable。
    /// </summary>
    /// <returns>Entity 全部比數的 IQueryable</returns>
    IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 更新一筆資料的內容。
    /// </summary>
    /// <param name="entity">要更新的內容</param>
    void Update(TEntity entity);

    /// <summary>
    /// 刪除一筆資料內容。
    /// </summary>
    /// <param name="entity">要被刪除的Entity。</param>
    void Delete(TEntity entity);

    /// <summary>
    /// 以Id查找內容 Async。
    /// </summary>
    /// <param name="id">要取得的Id</param>
    /// <returns>取得的內容。</returns>
    Task<TEntity> GetByIdAsync(object id);
}
