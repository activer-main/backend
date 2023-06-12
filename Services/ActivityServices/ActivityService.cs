using ActiverWebAPI.Exceptions;
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
    private readonly IRepository<RecommendedActivity, int> _recommendedActivityRepository;

    public ActivityService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _activityRepository = _unitOfWork.Repository<Activity, Guid>();
        _activityStatusRepository = _unitOfWork.Repository<ActivityStatus, int>();
        _recommendedActivityRepository = _unitOfWork.Repository<RecommendedActivity, int>();
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
            .Include(ac => ac.UserVoteActivities)
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
            .Include(ac => ac.UserVoteActivities)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Date)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Location)
            .Include(ac => ac.UserVoteTagInActivity)
                .ThenInclude(u => u.Tag)
                    .ThenInclude(t => t.Activities);
        return activities;
    }

    public async Task<Activity?> GetActivityIncludeAllByIdAsync(Guid id)
    {
        var activity = await _activityRepository.Query()
            .Where(x => x.Id.Equals(id))
            .Include(ac => ac.Images)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Tags)
            .Include(ac => ac.UserVoteActivities)
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

    public Activity? GetActivityIncludeAllById(Guid id)
    {
        var activity = _activityRepository.Query()
            .Where(x => x.Id.Equals(id))
            .Include(ac => ac.Images)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Tags)
            .Include(ac => ac.UserVoteActivities)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Date)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Location)
            .Include(ac => ac.UserVoteTagInActivity)
                .ThenInclude(u => u.Tag)
                    .ThenInclude(t => t.Activities)
            .FirstOrDefault();
        return activity;
    }

    public void UpdateActivityStatus(ActivityStatus activityStatusToUpdate)
    {
        _activityStatusRepository.Update(activityStatusToUpdate);
    }

    public async Task<Activity> GetActivityIncludeCommentsAsync(Guid id)
    {
        var activity = await _activityRepository.Query()
            .Where(x => x.Id.Equals(id))
            .Include(ac => ac.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync();
        
        return activity;
    }

    public async Task<IEnumerable<Activity>?> GetRecommendActivitiesIncludeAllAsync()
    {
        var recommendRecord = await _recommendedActivityRepository.Query()
            .Include(x => x.Activities)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync();

        if (recommendRecord == null)
        {
            return null;
        }

        IEnumerable<Activity> activities = recommendRecord.Activities.Select(x => GetActivityIncludeAllById(x.Id)).Where(x => x != null).AsEnumerable();
        return activities;
    }

    public async Task AddRecommendRecordAsync(IEnumerable<Guid> activityIds)
    {
        var activities = activityIds.Select(x => GetById(x)).Where(x => x != null).ToList();
        if (activities == null)
        {
            throw new BadRequestException("活動 Id 錯誤，沒有任何活動存在");
        }

        await _recommendedActivityRepository.AddAsync(new RecommendedActivity
        {
            Activities = activities
        });
    }
}
