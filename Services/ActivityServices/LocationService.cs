using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;

namespace ActiverWebAPI.Services.ActivityServices;

public class LocationService : GenericService<Location, int>
{

    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Location, int> _locationRepository;

    public LocationService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _locationRepository = _unitOfWork.Repository<Location, int>();
    }

    public Location? GetByContent(string content)
    {
        var location = _locationRepository.GetAll(x => x.Content == content).FirstOrDefault();
        return location;
    }

}
