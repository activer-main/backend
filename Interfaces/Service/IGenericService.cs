using System.Linq.Expressions;

namespace ActiverWebAPI.Interfaces.Service;

/// <summary>
/// 泛型的 Service 介面，定義基本的 CRUD 操作
/// </summary>
/// <typeparam name="TEntity">實體類別</typeparam>
public interface IGenericService<TEntity> where TEntity : class
{
    /// <summary>
    /// 取得所有 TEntity
    /// </summary>
    /// <returns>所有 TEntity 的 List</returns>
    IEnumerable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 根據 id 取得 TEntity
    /// </summary>
    /// <param name="id">TEntity 的 id</param>
    /// <returns>符合 id 的 TEntity</returns>
    TEntity GetById(object id);

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
    /// 根據 id 取得 TEntity
    /// </summary>
    /// <param name="id">TEntity 的 id</param>
    /// <returns>符合 id 的 TEntity</returns>
    Task<TEntity> GetByIdAsync(object id);

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
