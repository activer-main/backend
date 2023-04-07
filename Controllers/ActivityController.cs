using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<ActionResult<List<ActivityDTO>>?> GetAllActivities()
    {
        var activities = await _activityService.GetAllActivitiesIncludeAll().ToListAsync();
        if (activities == null)
            return null;
        var result = _mapper.Map<List<ActivityDTO>>(activities);
        return result;
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<ActivityDTO>> GetActivity(Guid id)
    {
        var activity = await _activityService.GetActivityIncludeAllByIdAsync(id);
        if (activity == null)
            return NotFound("活動不存在");

        // 封裝活動
        var activityDTO = _mapper.Map<ActivityDTO>(activity);

        // 如果使用者有驗證，查看已投票的 Tag
        if (User.Identity.IsAuthenticated)
        {
            var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userVotedTags = activity.UserVoteTagInActivity.Where(x => x.UserId.Equals(userId)).Select(x => x.Tag.Id).ToList();
            activityDTO.Tags.ForEach(x =>
            {
                if (userVotedTags.Contains(x.Id))
                {
                    x.UserVoted = true;
                }
            });
        }

        return activityDTO;
    }

    [Authorize(Roles = "Admin, InternalUser")]
    [HttpPost]
    public async Task<ActionResult<List<ActivityDTO>>> PostActivities(List<ActivityPostDTO> activityPostDTOs)
    {
        var activities = _mapper.Map<List<Models.DBEntity.Activity>>(activityPostDTOs);
        await _activityService.AddRangeAsync(activities);
        var activityDTOs = _mapper.Map<List<ActivityDTO>>(activities);
        return activityDTOs;
    }
}
