﻿using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.Filters;
using ActiverWebAPI.Services.TagServices;
using ActiverWebAPI.Services.UserServices;
using ActiverWebAPI.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace ActiverWebAPI.Controllers;

[Authorize]
[ApiController]
[TypeFilter(typeof(PasswordChangedAuthorizationFilter))]
[Route("api/[controller]")]
public class ActivityController : BaseController
{
    private readonly ActivityService _activityService;
    private readonly UserService _userService;
    private readonly TagService _tagService;
    private readonly IMapper _mapper;
    private readonly ActivityFilterValidationService _activityFilterValidationService;

    public ActivityController(
        ActivityService activityService,
        UserService userService,
        TagService tagService,
        IMapper mapper,
        ActivityFilterValidationService activityFilterValidationService
    )
    {
        _activityService = activityService;
        _userService = userService;
        _tagService = tagService;
        _mapper = mapper;
        _activityFilterValidationService = activityFilterValidationService;
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
    public async Task<ActionResult<ActivitySegmentResponseDTO>?> GetAllActivities([FromQuery] ActivitySegmentDTO request)
    {
        // 檢查 OrderBy
        CheckOrderByValue(request.OrderBy);

        // 如果需要存取 status Filter 直接導向 managedActivity
        if (!request.Status.IsNullOrEmpty())
        {
            if (!User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedException("使用者未登入");
            }
            return await GetManageActivities(request);
        }

        // 獲取所有活動
        var activities = _activityService.GetAllActivitiesIncludeAll();

        // 給 SortBy 與 OrderBy 預設值
        request.SortBy ??= "CreateAt";
        request.OrderBy ??= "descending";

        // 確認 SortBy 為可以接受的值
        _activityFilterValidationService.ValidateSortBy(request.SortBy);

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Activity, object>>>() { };
        var sortBy = request.SortBy;

        // Tag filter
        if (!request.Tags.IsNullOrEmpty())
        {
            // 獲取所有 tag Id
            var tagIds = request.Tags
                .Select(_tagService.GetTagByText)
                .Where(x => x != null)
                .Select(t => t.Id)
                .AsEnumerable();

            // Tag Filter (至少要有一個 tag 符合)
            if (tagIds != null)
            {
                activities = activities
                    .Where(a => a.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            // 加入 Tag 排序
            properties.Add(a => a.Tags.Count(t => tagIds.Contains(t.Id)));
        }

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

        // 計算總頁數
        var totalCount = activities.Count();
        var totalPage = totalCount / request.CountPerPage + 1;

        // 檢查 請求頁數 < 總頁數
        if (request.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({request.Page})大於總頁數({totalPage})");
        }

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activities, properties, request.OrderBy, request.Page, request.CountPerPage);

        // 轉換型態 Activity => ActivityDTO 
        var activityDTOList = _mapper.Map<IEnumerable<ActivityDTO>>(orderedActivityList);

        // 在 ActivityDTO 中加入使用者的活動狀態
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            await AddUserStatusValueAsync(activityDTOList, userId);
            AddUserVoteValue(activityDTOList, userId);
        }

        // 轉換型態
        var response = _mapper.Map<ActivitySegmentResponseDTO>(request);
        response.SearchData = activityDTOList;
        response.TotalData = totalCount;
        response.TotalPage = totalPage;

        return response;
    }

    [AllowAnonymous]
    [HttpGet("recommend")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SegmentsResponseDTO<ActivityDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ActivitySegmentResponseDTO>?> GetAllRecommendActivities([FromQuery] ActivitySegmentDTO request)
    {
        // 檢查 OrderBy
        CheckOrderByValue(request.OrderBy);

        // 如果需要存取 status Filter 直接導向 managedActivity
        if (!request.Status.IsNullOrEmpty())
        {
            if (!User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedException("使用者未登入");
            }
            return await GetManageActivities(request);
        }

        // 獲取所有活動
        var activities = await _activityService.GetRecommendActivitiesIncludeAllAsync();

        // 給 SortBy 與 OrderBy 預設值
        request.SortBy ??= "CreateAt";
        request.OrderBy ??= "descending";

        // 確認 SortBy 為可以接受的值
        _activityFilterValidationService.ValidateSortBy(request.SortBy);

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Activity, object>>>() { };
        var sortBy = request.SortBy;

        // Tag filter
        if (!request.Tags.IsNullOrEmpty())
        {
            // 獲取所有 tag Id
            var tagIds = request.Tags
                .Select(_tagService.GetTagByText)
                .Where(x => x != null)
                .Select(t => t.Id)
                .AsEnumerable();

            // Tag Filter (至少要有一個 tag 符合)
            if (tagIds != null)
            {
                activities = activities
                    .Where(a => a.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            // 加入 Tag 排序
            properties.Add(a => a.Tags.Count(t => tagIds.Contains(t.Id)));
        }

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

        // 計算總頁數
        var totalCount = activities.Count();
        var totalPage = totalCount / request.CountPerPage + 1;

        // 檢查 請求頁數 < 總頁數
        if (request.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({request.Page})大於總頁數({totalPage})");
        }

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activities.AsQueryable(), properties, request.OrderBy, request.Page, request.CountPerPage);

        // 轉換型態 Activity => ActivityDTO 
        var activityDTOList = _mapper.Map<IEnumerable<ActivityDTO>>(orderedActivityList);

        // 在 ActivityDTO 中加入使用者的活動狀態
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            await AddUserStatusValueAsync(activityDTOList, userId);
            AddUserVoteValue(activityDTOList, userId);
        }

        // 轉換型態
        var response = _mapper.Map<ActivitySegmentResponseDTO>(request);
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
            await AddUserVotedTagValueAsync(activity, userId, activityDTO);
            AddUserVoteValue(activityDTO, userId);
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

        // 檢查 OrderBy
        CheckOrderByValue(segmentRequest.OrderBy);

        // 確認 SortBy 為可以接受的值
        _activityFilterValidationService.ValidateSortBy(segmentRequest.SortBy);

        // 確認 Status 為可以接受的值
        _activityFilterValidationService.ValidateStatus(segmentRequest.Status);

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
                segmentRequest.Status.IsNullOrEmpty() || segmentRequest.Status.Contains(activityStatus[a.Id].Key)
            );

        // 解決 null 的問題
        segmentRequest.SortBy ??= "CreatedAt";
        segmentRequest.OrderBy ??= "Descending";

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Activity, object>>>() { };
        var sortBy = segmentRequest.SortBy;

        // Tag filter
        if (!segmentRequest.Tags.IsNullOrEmpty())
        {
            // 獲取所有 tag Id
            var tagIds = segmentRequest.Tags
                .Select(_tagService.GetTagByText)
                .Where(x => x != null)
                .Select(t => t.Id)
                .AsEnumerable();

            // Tag Filter (至少要有一個 tag 符合)
            if (tagIds != null)
            {
                activityList = activityList
                    .Where(a => a.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            // 加入 Tag 排序
            properties.Add(a => a.Tags.Count(t => tagIds.Contains(t.Id)));
        }

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

        // 計算總頁數
        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        // 檢查 頁數 < 總頁數
        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({segmentRequest.Page})大於總頁數({totalPage})");
        }

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activityList, properties, segmentRequest.OrderBy, segmentRequest.Page, segmentRequest.CountPerPage);

        // 轉換型態
        var activityDTOList = _mapper.Map<List<ActivityDTO>>(orderedActivityList);

        // 根據使用者更改 Status
        activityDTOList.ForEach(x =>
        {
            if (activityStatus.ContainsKey(x.Id))
            {
                x.Status = activityStatus[x.Id].Key;
                x.AddTime = activityStatus[x.Id].Value;
            }
        });

        // 加入使用者投票狀態
        AddUserVoteValue(activityDTOList, userId);

        var SegmentResponse = _mapper.Map<ActivitySegmentResponseDTO>(segmentRequest);

        SegmentResponse.SearchData = activityDTOList;
        SegmentResponse.TotalPage = (totalCount / segmentRequest.CountPerPage) + 1;
        SegmentResponse.TotalData = totalCount;

        return Ok(SegmentResponse);
    }

    [Authorize]
    [HttpGet("manageFilterValue")]
    [Produces("application/json")]
    public async Task<ActionResult<ActivityFilterDTO>> GetManagedActivityFilterValue()
    {
        // 獲得使用者資訊
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId,
            u => u.ActivityStatus
        );

        if (user == null)
        {
            throw new UserNotFoundException();
        }

        // 找出每個 activity 的 status
        var activityStatus = await _userService.GetUserActivityStatusAsync(user.Id);

        // 找出所有活動 並 filter status 
        var tagIds = activityStatus.Keys.Select(id => _activityService.GetById(id, a => a.Tags))
            .AsQueryable()
            .Where(x => x != null)
            .SelectMany(a => a.Tags)
            .Distinct()
            .Select(t => t.Id)
            .ToList();

        var tags = _tagService.GetAll(t => t.UserVoteTagInActivity, t => t.Activities).Where(t => tagIds.Contains(t.Id)).ToList();
        var tagsDTO = _mapper.Map<IEnumerable<TagDTO>>(tags);

        return new ActivityFilterDTO
        {
            Status = _activityFilterValidationService.GetAllowStatusSet(),
            Tags = tagsDTO
        };
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
        CheckOrderByValue(segmentRequest.OrderBy);

        var activityList = _activityService.GetAllActivitiesIncludeAll();
        var totalCount = activityList.Count();
        var totalPage = (totalCount / segmentRequest.CountPerPage) + 1;

        if (segmentRequest.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數 {segmentRequest.Page} 大於總頁數 {totalPage} ");
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
            await AddUserStatusValueAsync(activityDTOList, userId);
            AddUserVoteValue(activityDTOList, userId);
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
    [HttpPost("activityStatus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PostActivityStatus(Guid id, string status)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);

        _activityFilterValidationService.ValidateStatus(status);

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

    [AllowAnonymous]
    [HttpGet("activityFilterValue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActivityFilterDTO> GetActivityFilterValue()
    {
        var tags = _tagService.GetAll(t => t.UserVoteTagInActivity, t => t.Activities);
        var tagsDTO = _mapper.Map<IEnumerable<TagDTO>>(tags);

        return new ActivityFilterDTO
        {
            Status = _activityFilterValidationService.GetAllowStatusSet(),
            Tags = tagsDTO
        };
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<ActionResult<ActivitySearchResponseDTO>> GetSearchActivity([FromQuery] ActivitySearchRequestDTO request)
    {
        // 檢查 OrderBy
        CheckOrderByValue(request.OrderBy);

        var activities = _activityService.GetAllActivitiesIncludeAll();

        if (request.Keyword.IsNullOrEmpty() && request.Date.IsNullOrEmpty() && request.Tags.IsNullOrEmpty())
        {
            throw new BadRequestException("搜尋參數不得全部為空");
        }

        if (!request.Date.IsNullOrEmpty())
        {
            DateTime requestDate;
            if (DateTime.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out requestDate))
            {
                activities.Where(a => a.CreatedAt.Date == requestDate.Date);
            }
            else
            {
                throw new Exception("Date Parse 錯誤，請使用格式 yyyy-MM-dd");
            }
        }

        if (!request.Keyword.IsNullOrEmpty())
        {
            var keywords = request.Keyword.Trim().ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyword in keywords)
            {
                activities = activities.Where(a => EF.Functions.Like(a.Title, $"%{keyword}%") || EF.Functions.Like(a.Content, $"%{keyword}%") || EF.Functions.Like(a.Subtitle, $"%{keyword}%"));
            }
        }

        var properties = new List<Expression<Func<Activity, object>>>() { };

        var tags = request.Tags?.Select(_tagService.GetTagByText).Where(x => x != null);

        // Tag filter
        if (tags != null)
        {
            // 獲取所有 tag Id
            var tagIds = tags
                .Select(t => t.Id)
                .AsEnumerable();

            // 增加 Tag Trend
            foreach (var tag in tags)
            {
                _tagService.AddTagTrendCount(tag);
                _tagService.Update(tag);
            }
            await _tagService.SaveChangesAsync();

            // Tag Filter (至少要有一個 tag 符合)
            if (tagIds != null)
            {
                activities = activities
                    .Where(a => a.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            // 加入 Tag 排序
            properties.Add(a => a.Tags.Count(t => tagIds.Contains(t.Id)));
        }

        // 計算總頁數
        var totalCount = activities.Count();
        var totalPage = totalCount / request.CountPerPage + 1;

        // 檢查 請求頁數 < 總頁數
        if (request.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({request.Page})大於總頁數({totalPage})");
        }

        // 分頁 & 排序
        var orderedActivityList = DataHelper.GetSortedAndPagedData(activities, properties, request.OrderBy, request.Page, request.CountPerPage);

        // 轉換型態 Activity => ActivityDTO 
        var activityDTOList = _mapper.Map<IEnumerable<ActivityDTO>>(orderedActivityList);

        // 轉換型態
        var response = _mapper.Map<ActivitySearchResponseDTO>(request);
        var tagBaseDTOs = _mapper.Map<IEnumerable<TagBaseDTO>>(tags);
        response.Tags = tagBaseDTOs;
        response.SearchData = activityDTOList;
        response.TotalData = totalCount;
        response.TotalPage = totalPage;

        // 保存搜尋紀錄
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                // 轉換型態
                var searchHistory = _mapper.Map<SearchHistory>(request);
                searchHistory.Tags = tags?.ToList();
                _userService.SaveSearchHistory(user, searchHistory);
            }
            await _userService.SaveChangesAsync();
            await AddUserStatusValueAsync(activityDTOList, userId);
            AddUserVoteValue(activityDTOList, userId);
        }

        return response;
    }

    [HttpPost("comment")]
    public async Task<IActionResult> PostComment([FromBody] CommentPostDTO request)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, u => u.Comments);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var activityId = request.ActivityId;

        var activity = await _activityService.GetByIdAsync(activityId, ac => ac.Comments);
        if (activity == null)
            return BadRequest("活動不存在");

        var existComment = user.Comments?.FirstOrDefault(x => x.ActivityId == activityId);

        // 留言已存在 => 修改
        if (existComment != null)
        {
            var newComment = _mapper.Map<Comment>(request);
            existComment.Rate = newComment.Rate;
            existComment.ModifiedAt = DateTime.UtcNow;
            existComment.Content = newComment.Content;
            _userService.Update(user);
            await _userService.SaveChangesAsync();
            return Ok();
        }

        // 轉換型態
        var comment = _mapper.Map<Comment>(request);

        // 找出留言的 Sequence
        var sequence = 0;
        if (!activity.Comments.IsNullOrEmpty())
        {
            sequence = activity.Comments.Select(x => x.Sequence).Max();
        }

        comment.Activity = activity;
        comment.Sequence = sequence;
        user.Comments ??= new List<Comment> { };
        user.Comments.Add(comment);

        _userService.Update(user);
        await _userService.SaveChangesAsync();

        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("comment")]
    public async Task<ActionResult<ActivityCommentResponseDTO>> GetComments([FromQuery] ActivityCommentRequestDTO request)
    {
        var activity = await _activityService.GetActivityIncludeCommentsAsync(request.ActivityId);

        if (activity == null)
        {
            throw new BadRequestException("活動不存在");
        }

        var comments = activity.Comments;
        comments ??= new List<Comment> { };

        // 給 SortBy 與 OrderBy 預設值
        request.SortBy ??= "CreatedAt";
        request.OrderBy ??= "Descending";

        // 初始化 SortBy 列表
        var properties = new List<Expression<Func<Comment, object>>>() { };
        var sortBy = request.SortBy;

        // 加入 sortBy 列表
        switch (sortBy)
        {
            case "AddTime":
                properties.Add(a => a.CreatedAt);
                break;
            case "ModifiedTime":
                properties.Add(a => a.ModifiedAt);
                break;
            default:
                try
                {
                    var parameter = Expression.Parameter(typeof(Comment), "a");
                    var property = Expression.Property(parameter, sortBy);
                    var cast = Expression.Convert(property, typeof(object));
                    var lambda = Expression.Lambda<Func<Comment, object>>(cast, parameter);
                    properties.Add(lambda);
                }
                catch (ArgumentException ex)
                {
                    throw new BadRequestException("SortBy 參數不存在");
                }
                break;
        }

        // 計算總頁數
        var totalCount = comments.Count();
        var totalPage = totalCount / request.CountPerPage + 1;

        // 檢查 請求頁數 < 總頁數
        if (request.Page > totalPage)
        {
            throw new BadRequestException($"請求的頁數({request.Page})大於總頁數({totalPage})");
        }

        // 分頁 & 排序
        var orderedCommentsList = DataHelper.GetSortedAndPagedData(comments.AsQueryable(), properties, request.OrderBy, request.Page, request.CountPerPage);

        // 轉換型態
        var commentDTOList = _mapper.Map<IEnumerable<CommentDTO>>(orderedCommentsList);

        var response = _mapper.Map<ActivityCommentResponseDTO>(request);
        response.SearchData = commentDTOList;
        response.TotalPage = (totalCount / request.CountPerPage) + 1;
        response.TotalData = totalCount;

        // 加入使用者的留言
        if (User.Identity.IsAuthenticated)
        {
            var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
            response.UserComment = commentDTOList.FirstOrDefault(c => c.UserId == userId);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("comment/filterValue")]
    public async Task<ActionResult<string[]>> GetCommentsSortByValue()
    {
        string[] sortByList = { "AddTime", "ModifiedTime" };
        return sortByList;
    }

    [HttpDelete("comment/{id}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, u => u.Comments);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var toDelete = user.Comments?.FirstOrDefault(x => x.Id == id);

        if (toDelete == null)
        {
            return BadRequest("留言不存在或沒有權限");
        }

        user.Comments?.Remove(toDelete);

        _userService.Update(user);
        await _userService.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("vote/{id}")]
    public async Task<IActionResult> VoteActivity(Guid id, [FromBody] VoteActivityDTO request)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, u => u.UserVoteActivities) ?? throw new UserNotFoundException();

        HashSet<int> voteOptions = new HashSet<int> { -1, 0, 1 };

        if (!voteOptions.Contains(request.UserVote))
        {
            throw new BadRequestException($"vote的值不在接受的範圍內, 可接受的值: {String.Join(", ", request.UserVote)}");
        }

        // 沒有任何投票存在
        if (user.UserVoteActivities == null)
        {
            user.UserVoteActivities = new List<UserVoteActivity> { };
            user.UserVoteActivities.Add(
                new UserVoteActivity
                {
                    Vote = request.UserVote,
                    ActivityId = id
                }
                );
            _userService.Update(user);
            await _userService.SaveChangesAsync();

            return Ok();
        }

        // 指定的活動投票不存在
        var existVote = user.UserVoteActivities.FirstOrDefault(x => x.ActivityId == id);
        if (existVote == null)
        {
            user.UserVoteActivities.Add(
                new UserVoteActivity
                {
                    Vote = request.UserVote,
                    ActivityId = id
                }
                );
            _userService.Update(user);
            await _userService.SaveChangesAsync();
            return Ok();
        }

        existVote.Vote = request.UserVote;
        _userService.Update(user);
        await _userService.SaveChangesAsync();
        return Ok();
    }

    /// INTERNAL FUNCTIONS
    private static void CheckOrderByValue(string? orderBy)
    {
        var orderByList = new HashSet<string>() { "descending", "ascending" };

        if (orderBy != null && !orderByList.Contains(orderBy.ToLower()))
        {
            throw new BadRequestException($"OrderBy 參數錯誤，可用的參數: {string.Join(", ", orderByList)}");
        }
    }

    private void AddUserVoteValue(IEnumerable<ActivityDTO> activityDTOList, Guid userId)
    {
        var userActivityVoteDict = _userService.GetUserVotedActivityDict(userId);
        if (userActivityVoteDict != null)
        {
            // 標示投票
            foreach (var activity in activityDTOList.Where(x => userActivityVoteDict.ContainsKey(x.Id)))
            {
                activity.UserVote = userActivityVoteDict[activity.Id];
            }
        }
    }

    private void AddUserVoteValue(ActivityDTO activityDTO, Guid userId)
    {
        var userActivityVoteDict = _userService.GetUserVotedActivityDict(userId);
        
        if (userActivityVoteDict != null)
        {
            activityDTO.UserVote = userActivityVoteDict.GetValueOrDefault(activityDTO.Id, 0);
        }
    }

    private async Task AddUserStatusValueAsync(IEnumerable<ActivityDTO> activityDTOList, Guid userId)
    {
        var activityStatus = await _userService.GetUserActivityStatusAsync(userId);
        if (activityStatus != null)
        {
            // 根據使用者更改 Status
            foreach (var activity in activityDTOList.Where(x => activityStatus.ContainsKey(x.Id)))
            {
                activity.Status = activityStatus[activity.Id].Key;
                activity.AddTime = activityStatus[activity.Id].Value;
            }
        }
    }

    private async Task AddUserVotedTagValueAsync(Activity activity, Guid userId, ActivityDTO activityDTO)
    {
        var userVotedTags = activity.UserVoteTagInActivity.Where(x => x.UserId.Equals(userId)).Select(x => x.Tag.Id).ToList();
        activityDTO.Tags.ForEach(x =>
        {
            if (userVotedTags.Contains(x.Id))
            {
                x.UserVoted = true;
            }
        });

        // Activity Status
        var user = await _userService.GetByIdAsync(userId, u => u.ActivityStatus);

        if (user == null)
            throw new UserNotFoundException();

        var activityStatus = user.ActivityStatus?.Select(a => new KeyValuePair<Guid, string>(a.ActivityId, a.Status)).ToDictionary(kv => kv.Key, kv => kv.Value);
        activityDTO.Status = activityStatus.GetValueOrDefault(activityDTO.Id, null);
    }
}


