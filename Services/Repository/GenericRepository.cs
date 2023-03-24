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
    /// 取得第一筆符合條件的內容。如果符合條件有多筆，也只取得第一筆。
    /// </summary>
    /// <param name="predicate">要取得的Where條件。</param>
    /// <returns>取得第一筆符合條件的內容。</returns>
    public TEntity Get(Expression<Func<TEntity, bool>> predicate)
    {
        return Context.Set<TEntity>().Where(predicate).FirstOrDefault();
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
}
