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
    IEnumerable<TEntity> GetAll();

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
    void Create(TEntity entity);

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
}
