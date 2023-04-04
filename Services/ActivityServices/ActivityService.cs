using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;

namespace ActiverWebAPI.Services.ActivityServices;

public class ActivityService : GenericService<Activity, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Activity, Guid> _activityRepository;
    private readonly IConfiguration _configuration;

    public ActivityService(IUnitOfWork unitOfWork, IConfiguration configuration) : base(unitOfWork)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _activityRepository = _unitOfWork.Repository<Activity, Guid>();
    }


}
