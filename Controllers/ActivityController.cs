using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.TagServices;
using ActiverWebAPI.Services.UserServices;
using ActiverWebAPI.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : BaseController
{
    private readonly ActivityService _activityService;
    private readonly UserService _userService;
    private readonly TagService _tagService;
    private readonly IMapper _mapper;

    public ActivityController(
        ActivityService activityService,
        UserService userService,
        TagService tagService,
        IMapper mapper
    )
    {
        _activityService = activityService;
        _userService = userService;
        _tagService = tagService;
        _mapper = mapper;
    }

    /// <summary>
    /// 取得所有活動列表
    /// </summary>
    /// <returns>所有活動列表</returns>
    /// <response code="200">成功取得所有活動列表</response>
    /// <response code="401">未經授權的訪問</response>
    [AllowAnonymous]
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SegmentsResponseDTO<ActivityDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SegmentsResponseDTO<ActivityDTO>>?> GetAllActivities([FromQuery] SegmentsRequestDTO segmentRequest)
    {
        var activities = await _activityService.GetAllActivitiesIncludeAll().ToListAsync();

        var totalCount = activities.Count;
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        segmentRequest.SortBy ??= "CreateAt";
        segmentRequest.OrderBy ??= "descending";

        var orderedActivityList = DataHelper.GetSortedAndPagedData(activities, segmentRequest.SortBy, segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);

        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);

        // 把 activity 中加入 user 的 status
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);

            if (user != null)
            {
                var activityStatus = user.ActivityStatus?.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
                var activityStatusIds = activityStatus.Select(kv => kv.Key);
                // 根據使用者更改 Status
                activityDTOList.ForEach(x =>
                {
                    if (activityStatusIds.Contains(x.Id))
                    {
                        x.Status = activityStatus.GetValueOrDefault(x.Id, null);
                    }
                });
            }
        }

        var response = _mapper.Map<SegmentsResponseDTO<ActivityDTO>>(segmentRequest);
        response.SearchData = activityDTOList;
        response.TotalData = totalCount;
        response.TotalPage = totalPage;

        return response;
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

        // 新增活動熱度
        activity.ActivityClickedCount += 1;
        _activityService.Update(activity);
        await _activityService.SaveChangesAsync();

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

            var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);

            if (user != null)
            {
                var activityStatus = user.ActivityStatus?.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
                activityDTO.Status = activityStatus.GetValueOrDefault(activityDTO.Id, null);
            }
        }


        return activityDTO;
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
            u => u.ActivityStatus
        );

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var activityStatus = user.ActivityStatus?.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var activityStatusIds = activityStatus?.Select(kv => kv.Key).ToList();

        var activityList = _activityService.GetAllActivitiesIncludeAll(x => activityStatusIds.Contains(x.Id));
        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if(segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        segmentRequest.SortBy ??= "CreatedAt";
        segmentRequest.OrderBy ??= "Descending";

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
    [ProducesResponseType(typeof(SegmentsResponseBaseDTO<ActivityDTO>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<SegmentsResponseBaseDTO<ActivityDTO>>> GetTrendActivities([FromQuery] SegmentsRequestBaseDTO segmentRequest)
    {
        var activityList = _activityService.GetAllActivitiesIncludeAll();
        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        segmentRequest.OrderBy ??= "descending";

        var orderedActivityList = DataHelper.GetSortedAndPagedData(activityList, "ActivityClickedCount", segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);
        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);
        var SegmentResponse = _mapper.Map<SegmentsResponseBaseDTO<ActivityDTO>>(segmentRequest);

        // 把 activity 中加入 user 的 status
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);

            if (user != null)
            {
                var activityStatus = user.ActivityStatus?.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
                var activityStatusIds = activityStatus.Select(kv => kv.Key);
                // 根據使用者更改 Status
                activityDTOList.ForEach(x =>
                {
                    if (activityStatusIds.Contains(x.Id))
                    {
                        x.Status = activityStatus.GetValueOrDefault(x.Id, null);
                    }
                });
            }
        }

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
    public async Task<ActionResult> PostActivityStatus(Guid id, string status)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var activity = await _activityService.GetByIdAsync(id);

        // 確認 Branch 存在
        if (activity == null)
        {
            throw new NotFoundException("Branch not found.");
        }

        user.ActivityStatus ??= new List<ActivityStatus>() { };

        var userActivityStatusFind = user.ActivityStatus.Find(x => x.ActivityId == activity.Id);

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



