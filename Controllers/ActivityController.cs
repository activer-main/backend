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
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Linq.Expressions;

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
    public async Task<ActionResult<ActivitySegmentResponseDTO>?> GetAllActivities([FromQuery] ActivitySegmentDTO segmentRequest)
    {
        // 如果需要存取 status Filter 直接導向 managedActivity
        if (!segmentRequest.Status.IsNullOrEmpty())
        {
            if (!User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedException("使用者未登入");
            }
            return await GetManageActivities(segmentRequest);
        }

        // 獲取所有活動
        var activities = _activityService.GetAllActivitiesIncludeAll().AsEnumerable();

        // 給 SortBy 與 OrderBy 預設值
        segmentRequest.SortBy ??= "CreateAt";
        segmentRequest.OrderBy ??= "descending";

        // 確認 SortBy 為可以接受的值
        var allowSortBySet = new HashSet<string> { "Trend", "CreatedAt", "AddTime" };
        if (!allowSortBySet.Contains(segmentRequest.SortBy))
        {
            throw new BadRequestException($"排序: '{segmentRequest.SortBy}' 不在可接受的排序列表: '{string.Join(", ", allowSortBySet)}'");
        }

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Activity, object>>>() { };
        var sortBy = segmentRequest.SortBy;

        // 加入 sortBy 列表
        switch (sortBy)
        {
            case "Trend":
                properties.Add(a => a.ActivityClickedCount);
                break;
            case "AddTime":
                properties.Add(a => a.CreatedAt);
                break;
            default:
                var parameter = Expression.Parameter(typeof(Activity), "a");
                var property = Expression.Property(parameter, sortBy);
                var cast = Expression.Convert(property, typeof(object));
                var lambda = Expression.Lambda<Func<Activity, object>>(cast, parameter);
                properties.Add(lambda);
                break;
        }

        // Tag filter
        if (!segmentRequest.Tags.IsNullOrEmpty())
        {
            // 獲取所有 tag Id
            var tagIds = segmentRequest.Tags
                .Select(_tagService.GetTagByText)
                .Where(x => x != null)
                .Select(t => t.Id)
                .ToList();

            // Tag Filter (至少要有一個 tag 符合)
            activities = activities
                .Where(a => !a.Tags.IsNullOrEmpty())
                .Where(a => tagIds == null || a.Tags.Any(t => tagIds.Contains(t.Id)));

            // 加入 Tag 排序
            properties.Add(a => a.Tags.Count(t => tagIds.Contains(t.Id)));
        }

        // 計算總頁數
        var totalCount = activities.Count();
        var totalPage = totalCount / segmentRequest.CountPerPage + (totalCount % segmentRequest.CountPerPage > 0 ? 1 : 0);

        // 檢查 請求頁數 < 總頁數
        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activities.AsQueryable(), properties, segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);

        // 轉換型態 Activity => ActivityDTO 
        var activityDTOList = _mapper.Map<IEnumerable<ActivityDTO>>(orderedActivityList);

        // 在 ActivityDTO 中加入使用者的活動狀態
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            var activityStatus = await _userService.GetUserActivityStatusAsync(userId);
            if (activityStatus != null)
            {
                // 根據使用者更改 Status
                foreach (var activity in activityDTOList.Where(x => activityStatus.ContainsKey(x.Id)))
                {
                    activity.Status = activityStatus[activity.Id];
                }
            }
        }

        // 轉換型態
        var response = _mapper.Map<ActivitySegmentResponseDTO>(segmentRequest);
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
    public async Task<ActionResult<ActivitySegmentResponseDTO>> GetManageActivities([FromQuery] ActivitySegmentDTO segmentRequest)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId,
            u => u.ActivityStatus
        );

        // 確認 SortBy 為可以接受的值
        var allowSortBySet = new HashSet<string> { "Trend", "CreatedAt", "AddTime" };
        if (!segmentRequest.SortBy.IsNullOrEmpty() && !allowSortBySet.Contains(segmentRequest.SortBy))
        {
            throw new BadRequestException($"排序: '{segmentRequest.SortBy}' 不在可接受的排序列表: '{string.Join(", ", allowSortBySet)}'");
        }

        // 確認 Status 為可以接受的值
        var allowStatusSet = new HashSet<string> { "願望", "已註冊", "已完成" };
        segmentRequest.Status?.ForEach(s =>
        {
            if (!allowStatusSet.Contains(s))
            {
                throw new BadRequestException($"活動狀態: '{s}' 不在可接受的狀態列表: '{string.Join(", ", allowStatusSet)}'");
            }
        });

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        // 找出每個 activity 的 status
        var activityStatus = await _userService.GetUserActivityStatusAsync(user.Id);

        // 找出所有活動 並 filter status 
        var activityList = activityStatus.Keys.Select(_activityService.GetActivityIncludeAllById)
            .AsQueryable()
            .Where(x => x != null)
            .Where(a =>
                segmentRequest.Status.IsNullOrEmpty() || segmentRequest.Status.Contains(activityStatus[a.Id])
            );

        var tagIds = segmentRequest.Tags?.Select(_tagService.GetTagByText).Where(x => x != null).Select(t => t.Id).ToList();

        // 如果有 tag filter list
        if (!tagIds.IsNullOrEmpty())
        {
            // Tag Filter
            activityList = activityList
                .OrderBy(a => !a.Tags.IsNullOrEmpty() ? a.Tags.Count(t => tagIds.Contains(t.Id)) : 0)
                .Where(a => !a.Tags.IsNullOrEmpty() && a.Tags.Any(t => tagIds.Contains(t.Id)));
        }

        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        // 解決 null 的問題
        segmentRequest.SortBy ??= "CreatedAt";
        segmentRequest.OrderBy ??= "Descending";

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Activity, object>>>() { };
        var sortBy = segmentRequest.SortBy;

        // 加入 sortBy 列表
        switch (sortBy)
        {
            case "Trend":
                properties.Add(a => a.ActivityClickedCount);
                break;
            case "AddTime":
                properties.Add(a => a.CreatedAt);
                break;
            default:
                var parameter = Expression.Parameter(typeof(Activity), "a");
                var property = Expression.Property(parameter, sortBy);
                var cast = Expression.Convert(property, typeof(object));
                var lambda = Expression.Lambda<Func<Activity, object>>(cast, parameter);
                properties.Add(lambda);
                break;
        }

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activityList, properties, segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);

        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);

        // 根據使用者更改 Status
        activityDTOList.ForEach(x =>
        {
            if (activityStatus.ContainsKey(x.Id))
            {
                x.Status = activityStatus.GetValueOrDefault(x.Id, null);
            }
        });

        var SegmentResponse = _mapper.Map<ActivitySegmentResponseDTO>(segmentRequest);

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

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Activity, object>>>() { };
        properties.Add(a => a.ActivityClickedCount);

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activityList, properties, segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);

        var activityDTOList = _mapper.Map<IEnumerable<ActivityDTO>>(orderedActivityList);
        var SegmentResponse = _mapper.Map<SegmentsResponseBaseDTO<ActivityDTO>>(segmentRequest);

        // 把 activity 中加入 user 的 status
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            var activityStatus = await _userService.GetUserActivityStatusAsync(userId);
            if (activityStatus != null)
            {
                // 根據使用者更改 Status
                foreach (var activity in activityDTOList.Where(x => activityStatus.ContainsKey(x.Id)))
                {
                    activity.Status = activityStatus[activity.Id];
                }
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

        var statusList = new List<string> { "願望", "已註冊", "已完成" };
        if (!statusList.Contains(status))
        {
            throw new BadRequestException($"活動狀態: {status} 不在可接受的狀態列表: \"{string.Join(", ", statusList)}\"");
        }

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

    [Authorize]
    [HttpDelete("activityStatus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteActivitiesStatus(IEnumerable<Guid> ids)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        user.ActivityStatus = user.ActivityStatus?.Where(ac => !ids.Contains(ac.ActivityId)).ToList();
        _userService.Update(user);
        await _userService.SaveChangesAsync();

        return Ok();
    }
}



