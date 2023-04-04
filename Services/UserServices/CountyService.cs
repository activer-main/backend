using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Services.UserServices;

public class CountyService : GenericService<County, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<County, int> _countyRepository;

    public CountyService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _countyRepository = _unitOfWork.Repository<County, int>();
    }

    public async Task<County?> GetByNameAsync(string name)
    {
        var query = _countyRepository.Query();
        return await query.FirstOrDefaultAsync(e => e.Content == name);
    }
}
