﻿using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.UserServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly ActivityService _activityService;
    private readonly UserService _userService;
    private readonly IMapper _mapper;

    public ActivityController(
        ActivityService activityService,
        UserService userService,
        IMapper mapper
    )
    {
        _activityService = activityService;
        _userService = userService;
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

    [Authorize]
    [HttpGet("manage")]
    public async Task<ActionResult<SegmentsResponseDTO<ActivityDTO>>> GetManageActivities([FromQuery] ManageActivitySegmentDTO segmentRequest)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userService.GetByIdAsync(userId,
            u => u.ActivityStatus,
            u => u.ActivityStatus.Select(a => a.Activity)
        );

        // 確認 User 存在
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var activityIDs = user.ActivityStatus.Select(a => a.ActivityId).ToList();

        var activityStatus = user.ActivityStatus.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var activityStatusIds = activityStatus.Select(kv => kv.Key).ToList();

        var activityList = _activityService.GetAllActivitiesIncludeAll(x => activityIDs.Contains(x.Id));

        // 排序
        var propInfo = typeof(Activity).GetProperty(segmentRequest.SortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (propInfo == null)
        {
            // 排序字段不存在，返回错误响应
            return BadRequest("Invalid sorting field.");
        }

        // 对 activityList 进行排序
        var orderedActivityList = segmentRequest.OrderBy.ToLower() == "descending"
            ? activityList.OrderByDescending(x => propInfo.GetValue(x, null)).ToList()
            : activityList.OrderBy(x => propInfo.GetValue(x, null)).ToList();
        orderedActivityList = orderedActivityList.Skip((segmentRequest.Page - 1) * segmentRequest.CountPerPage).Take(segmentRequest.CountPerPage).ToList();

        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);

        activityDTOList.ForEach(x =>
        {
            if (activityStatusIds.Contains(x.Id))
            {
                x.Status = activityStatus.GetValueOrDefault(x.Id, null);
            }
        });

        var SegmentResponse = _mapper.Map<SegmentsResponseDTO<ActivityDTO>>(segmentRequest);
        SegmentResponse.SearchData = activityDTOList;

        return SegmentResponse;
    }


    [Authorize]
    [HttpPost("activityStatus")]
    public async Task<ActionResult> PostActivityStatus(Guid activityId, string status)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userService.GetByIdAsync(userId);

        // 確認 User 存在
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var activity = await _activityService.GetByIdAsync(activityId);

        // 確認 Branch 存在
        if (activity == null)
        {
            return NotFound("Branch not found.");
        }

        var userActivityStatusFind = user.ActivityStatus?.Find(x => x.ActivityId == activity.Id);

        if (userActivityStatusFind == null)
        {
            user.ActivityStatus.Add(new ActivityStatus
            {
                Activity = activity,
                Status = status
            });
        }
        else
        {
            userActivityStatusFind.Activity = activity;
            userActivityStatusFind.Status = status;
            _activityService.UpdateActivityStatus(userActivityStatusFind);
        }
        await _activityService.SaveChangesAsync();

        return Ok();
    }
}



