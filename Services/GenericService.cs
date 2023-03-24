using ActiverWebAPI.Interfaces.Service;
using ActiverWebAPI.Interfaces.UnitOfWork;

namespace ActiverWebAPI.Services;

/// <summary>
/// 泛型的 Service 實作類別，提供基本的 CRUD 操作
/// </summary>
/// <typeparam name="TEntity">實體類別</typeparam>
public class GenericService<TEntity> : IGenericService<TEntity> where TEntity : class
{
    private readonly IUnitOfWork _unitOfWork;

    public GenericService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IEnumerable<TEntity> GetAll()
    {
        return _unitOfWork.Repository<TEntity>().GetAll();
    }

    public TEntity GetById(object id)
    {
        return _unitOfWork.Repository<TEntity>().GetById(id);
    }

    public void Add(TEntity entity)
    {
        _unitOfWork.Repository<TEntity>().Add(entity);
        SaveChanges();
    }

    public void Delete(TEntity entity)
    {
        _unitOfWork.Repository<TEntity>().Delete(entity);
        SaveChanges();
    }
    
    public void SaveChanges()
    {
        _unitOfWork.SaveChanges();
    }

    public void Update(TEntity entity)
    {
        _unitOfWork.Repository<TEntity>().Update(entity);
        SaveChanges();
    }
}