using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Services.TagServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services.ActivityServices;

public class ActivityService : GenericService<Activity, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Activity, Guid> _activityRepository;
    private readonly IRepository<ActivityStatus, int> _activityStatusRepository;

    public ActivityService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _activityRepository = _unitOfWork.Repository<Activity, Guid>();
        _activityStatusRepository = _unitOfWork.Repository<ActivityStatus, int>();
    }

    public IQueryable<Activity> GetAllActivitiesIncludeAll()
    {
        var activities = _activityRepository.Query()
            .Include(ac => ac.Images)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Tags)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Date)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Location)
            .Include(ac => ac.UserVoteTagInActivity)
                .ThenInclude(u => u.Tag)
                    .ThenInclude(t => t.Activities);
        return activities;
    }

    public IQueryable<Activity> GetAllActivitiesIncludeAll(Expression<Func<Activity, bool>> predicate)
    {
        var activities = _activityRepository.Query()
            .Where(predicate)
            .Include(ac => ac.Images)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Tags)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Date)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Location)
            .Include(ac => ac.UserVoteTagInActivity)
                .ThenInclude(u => u.Tag)
                    .ThenInclude(t => t.Activities);
        return activities;
    }

    public async Task<Activity?> GetActivityIncludeAllByIdAsync(Guid Id)
    {
        var activity = await _activityRepository.Query()
            .Where(x => x.Id.Equals(Id))
            .Include(ac => ac.Images)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Tags)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Date)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Location)
            .Include(ac => ac.UserVoteTagInActivity)
                .ThenInclude(u => u.Tag)
                    .ThenInclude(t => t.Activities)
            .FirstOrDefaultAsync();
        return activity;
    }

    public void UpdateActivityStatus(ActivityStatus activityStatusToUpdate)
    {
        _activityStatusRepository.Update(activityStatusToUpdate);
    }
}
