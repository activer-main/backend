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
    /// 取得第一筆符合條件的內容。
    /// </summary>
    /// <param name="predicate">要取得的Where條件</param>
    /// <returns>取得第一筆符合條件的內容</returns>
    TEntity Get(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 取得 Entity 全部筆數的 IQueryable。
    /// </summary>
    /// <returns>Entity 全部比數的 IQueryable</returns>
    IQueryable<TEntity> GetAll();

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
}
