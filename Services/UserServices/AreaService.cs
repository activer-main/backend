using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Services.UserServices;

public class AreaService : GenericService<Area, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Area, int> _areaRepository;

    public AreaService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _areaRepository = _unitOfWork.Repository<Area, int>();
    }

    public async Task<IEnumerable<Area>?> GetByNameAsync(string name)
    {
        var query = _areaRepository.Query();
        return await query.Where(e => e.AreaName == name).ToListAsync();
    }

    public async Task<IEnumerable<Area>?> GetByEngNameAsync(string name)
    {
        var query = _areaRepository.Query();
        return await query.Where(e => e.AreaEngName == name).ToListAsync();
    }

    public async Task<Area?> GetByZipCode(string zipCode)
    {
        var query = _areaRepository.Query();
        return await query.FirstOrDefaultAsync(e => e.ZipCode == zipCode);
    }
}
