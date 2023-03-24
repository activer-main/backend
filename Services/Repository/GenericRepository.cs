using ActiverWebAPI.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services.Repository;

public class GenericRepository<TEntity> : IRepository<TEntity> 
    where TEntity : class
{
    private DbContext Context { get; set; }

    /// <summary>
    /// 建構EF一個Entity的Repository，需傳入此Entity的Context。
    /// </summary>
    /// <param name="inContext">Entity所在的Context</param>
    public GenericRepository(DbContext inContext)
    {
        Context = inContext;
    }

    /// <summary>
    /// 新增一筆資料到資料庫。
    /// </summary>
    /// <param name="entity">要新增到資料的庫的Entity</param>
    public void Add(TEntity entity)
    {
        Context.Set<TEntity>().Add(entity);
    }

    /// <summary>
    /// 以Id查找內容。
    /// </summary>
    /// <param name="id">要取得的Id</param>
    /// <returns>取得的內容。</returns>
    public TEntity GetById(object id)
    {
        return Context.Set<TEntity>().Find(id);
    }

    /// <summary>
    /// 取得Entity全部筆數的IQueryable。
    /// </summary>
    /// <param name="predicate">Where的表達式</param>
    /// <returns>Entity全部筆數的IQueryable。</returns>
    public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate)
    {
        return Context.Set<TEntity>().Where(predicate).AsQueryable();
    }

    /// <summary>
    /// 取得Entity全部筆數的IQueryable。
    /// </summary>
    /// <returns>Entity全部筆數的IQueryable。</returns>
    public IQueryable<TEntity> GetAll()
    {
        return Context.Set<TEntity>().AsQueryable();
    }

    /// <summary>
    /// 更新一筆Entity內容。
    /// </summary>
    /// <param name="entity">要更新的內容</param>
    public void Update(TEntity entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
    }

    /// <summary>
    /// 更新一筆Entity的內容。只更新有指定的Property。
    /// </summary>
    /// <param name="entity">要更新的內容。</param>
    /// <param name="updateProperties">需要更新的欄位。</param>
    public void Update(TEntity entity, Expression<Func<TEntity, object>>[] updateProperties)
    {
        Context.Entry(entity).State = EntityState.Unchanged;

        if (updateProperties != null)
        {
            foreach (var property in updateProperties)
            {
                Context.Entry(entity).Property(property).IsModified = true;
            }
        }
    }

    /// <summary>
    /// 刪除一筆資料內容。
    /// </summary>
    /// <param name="entity">要被刪除的Entity。</param>
    public void Delete(TEntity entity)
    {
        Context.Entry(entity).State = EntityState.Deleted;
    }


    /// <summary>
    /// 根據 id 取得 TEntity
    /// </summary>
    /// <param name="id">TEntity 的 id</param>
    /// <returns>符合 id 的 TEntity</returns>
    public async Task<TEntity> GetByIdAsync(object id)
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    /// <summary>
    /// 新增 TEntity
    /// </summary>
    /// <param name="entity">欲新增的 TEntity</param>
    public async Task AddAsync(TEntity entity)
    {
        await Context.Set<TEntity>().AddAsync(entity);
    }
}
