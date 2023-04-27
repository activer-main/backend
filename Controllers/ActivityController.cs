using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.UserServices;
using ActiverWebAPI.Utils;
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
public class ActivityController : BaseController
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

    /// <summary>
    /// 取得所有活動列表
    /// </summary>
    /// <returns>所有活動列表</returns>
    /// <response code="200">成功取得所有活動列表</response>
    /// <response code="401">未經授權的訪問</response>
    [Authorize(Roles = "Admin, InternalUser")]
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<ActivityDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ActivityDTO>>?> GetAllActivities()
    {
        var activities = await _activityService.GetAllActivitiesIncludeAll().ToListAsync();
        var result = _mapper.Map<List<ActivityDTO>>(activities);
        return result;
    }

    /// <summary>
    /// 取得活動資訊
    /// </summary>
    /// <param name="id">活動 ID</param>
    /// <returns>活動 DTO</returns>
    [AllowAnonymous]
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ActivityDTO))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDTO>> GetActivity(Guid id)
    {
        var activity = await _activityService.GetActivityIncludeAllByIdAsync(id);
        if (activity == null)
            throw new NotFoundException("活動不存在");

        // 封裝活動
        var activityDTO = _mapper.Map<ActivityDTO>(activity);

        // 如果使用者有驗證，查看已投票的 Tag
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
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

    /// <summary>
    /// 新增活動
    /// </summary>
    /// <param name="activityPostDTOs">欲新增的活動資料</param>
    /// <returns>新增成功的活動資料</returns>
    /// <response code="200">成功回傳新增成功的活動資料</response>
    /// <response code="401">使用者未登入，無法新增活動</response>
    [Authorize(Roles = "Admin, InternalUser")]
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ActivityDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ActivityDTO>>> PostActivities(List<ActivityPostDTO> activityPostDTOs)
    {
        var activities = _mapper.Map<List<Activity>>(activityPostDTOs);
        await _activityService.AddRangeAsync(activities);
        await _activityService.SaveChangesAsync();
        var activityDTOs = _mapper.Map<List<ActivityDTO>>(activities);
        return activityDTOs;
    }

    /// <summary>
    /// 取得使用者管理的活動
    /// </summary>
    /// <param name="segmentRequest">分頁請求參數</param>
    /// <returns>活動列表</returns>
    /// <response code="200">成功取得活動列表</response>
    /// <response code="400">請求參數錯誤</response>
    /// <response code="401">使用者未驗證或無權限</response>
    /// <response code="404">找不到使用者</response>
    [Authorize]
    [HttpGet("manage")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SegmentsResponseDTO<ActivityDTO>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SegmentsResponseDTO<ActivityDTO>>> GetManageActivities([FromQuery] ManageActivitySegmentDTO segmentRequest)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId,
            u => u.ActivityStatus,
            u => u.ActivityStatus.Select(a => a.Activity)
        );

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var activityIDs = user.ActivityStatus.Select(a => a.ActivityId).ToList();

        var activityStatus = user.ActivityStatus.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var activityStatusIds = activityStatus.Select(kv => kv.Key).ToList();

        var activityList = _activityService.GetAllActivitiesIncludeAll(x => activityIDs.Contains(x.Id));
        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if(segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        var orderedActivityList = DataHelper.GetSortedAndPagedData(activityList, segmentRequest.SortBy, segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);

        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);

        // 根據使用者更改 Status
        activityDTOList.ForEach(x =>
        {
            if (activityStatusIds.Contains(x.Id))
            {
                x.Status = activityStatus.GetValueOrDefault(x.Id, null);
            }
        });

        var SegmentResponse = _mapper.Map<SegmentsResponseDTO<ActivityDTO>>(segmentRequest);

        SegmentResponse.SearchData = activityDTOList;
        SegmentResponse.TotalPage = (totalCount / segmentRequest.CountPerPage) + 1;
        SegmentResponse.TotalData = totalCount;

        return Ok(SegmentResponse);
    }

    /// <summary>
    /// 取得熱門活動清單
    /// </summary>
    /// <param name="segmentRequest">分頁、排序、搜尋參數</param>
    /// <returns>熱門活動清單</returns>
    [AllowAnonymous]
    [HttpGet("trend")]
    [ProducesResponseType(typeof(SegmentsResponseDTO<ActivityDTO>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<SegmentsResponseDTO<ActivityDTO>>> GetTrendActivities([FromQuery] SegmentsRequestBaseDTO segmentRequest)
    {
        var activityList = _activityService.GetAllActivitiesIncludeAll();
        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        var orderedActivityList = DataHelper.GetSortedAndPagedData(activityList, "ActivityClickedCount", segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);
        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);
        var SegmentResponse = _mapper.Map<SegmentsResponseBaseDTO<ActivityDTO>>(segmentRequest);

        SegmentResponse.SearchData = activityDTOList;
        SegmentResponse.TotalPage = totalPage;
        SegmentResponse.TotalData = totalCount;

        return Ok(SegmentResponse);
    }

    /// <summary>
    /// 設定使用者對於活動的狀態
    /// </summary>
    /// <param name="activityId">活動ID</param>
    /// <param name="status">狀態</param>
    /// <returns>操作結果</returns>
    [Authorize]
    [HttpPost("activityStatus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PostActivityStatus(Guid activityId, string status)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId);

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var activity = await _activityService.GetByIdAsync(activityId);

        // 確認 Branch 存在
        if (activity == null)
        {
            throw new NotFoundException("Branch not found.");
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



