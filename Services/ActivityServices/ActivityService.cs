using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services.ActivityServices;

public class ActivityService : GenericService<Activity, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Activity, Guid> _activityRepository;

    public ActivityService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _activityRepository = _unitOfWork.Repository<Activity, Guid>();
    }

    public IQueryable<Activity> GetAllActivitiesIncludeAll()
    {
        var activities = _activityRepository.Query()
            .Include(ac => ac.Images)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyStart)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyEnd)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyFee)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.DateStart)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.DateEnd)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Locations)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.BranchStatus)
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
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyStart)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyEnd)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyFee)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.DateStart)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.DateEnd)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Locations)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.BranchStatus)
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
            .Include(ac => ac.Sources)
            .Include(ac => ac.Connections)
            .Include(ac => ac.Holders)
            .Include(ac => ac.Objectives)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyStart)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyEnd)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.ApplyFee)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.DateStart)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.DateEnd)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.Locations)
            .Include(ac => ac.Branches)
                .ThenInclude(b => b.BranchStatus)
            .Include(ac => ac.UserVoteTagInActivity)
                .ThenInclude(u => u.Tag)
                    .ThenInclude(t => t.Activities)
            .FirstOrDefaultAsync();
        return activity;
    }

}
