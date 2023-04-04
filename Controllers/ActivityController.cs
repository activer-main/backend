using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Controllers;

public class ActivityController : ControllerBase
{
    private readonly ActivityService _activityService;
    private readonly IMapper _mapper;

    public ActivityController(
        ActivityService activityService,
        IMapper mapper
    )
    {
        _activityService = activityService;
        _mapper = mapper;
    }

    [Authorize(Roles = "Admin, InternalUser")]
    [HttpGet]
    public async Task<ActionResult<List<ActivityDTO>>> GetAllActivities()
    {
        var activities = await _activityService.GetAll(activity => activity.UserVoteTagInActivity).ToListAsync();

        var result = _mapper.Map<List<ActivityDTO>>(activities);
        return result;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ActivityDTO>> GetActivity(Guid activityId)
    {
        var activity = await _activityService.GetByIdAsync(activityId,
            activity => activity.Branches,
            activity => activity.Images,
            activity => activity.Sources,
            activity => activity.Connections,
            activity => activity.Holders,
            activity => activity.Objectives,
            activity => activity.UserVoteTagInActivity
            );
        if (activity == null)
            return NotFound("活動不存在");
        var activityDTO = _mapper.Map<ActivityDTO>(activity);
        return activityDTO;
    }
}
