using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.TagServices;
using ActiverWebAPI.Services.UserServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.IdentityModel.Tokens;
using ActiverWebAPI.Services.Filters;
using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Utils;
using System.Linq.Expressions;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin, InternalUser")]
[TypeFilter(typeof(PasswordChangedAuthorizationFilter))]
[Route("api/[controller]")]
public class InternalController : BaseController
{
    private readonly ActivityService _activityService;
    private readonly ActivityFilterValidationService _activityFilterValidationService;
    private readonly ProfessionService _professionService;
    private readonly CountyService _countyService;
    private readonly TagService _tagService;
    private readonly LocationService _locationService;
    private readonly IMapper _mapper;
    private readonly UserService _userService;

    public InternalController(
        ActivityService activityService,
        ActivityFilterValidationService activityFilterValidationService,
        UserService userService,
        ProfessionService professionService,
        CountyService countyService,
        TagService tagService,
        LocationService locationService,
        IMapper mapper
    )
    {
        _activityService = activityService;
        _activityFilterValidationService = activityFilterValidationService;
        _countyService = countyService;
        _tagService = tagService;
        _locationService = locationService;
        _professionService = professionService;
        _userService = userService;
        _mapper = mapper;
    }


    /// <summary>
    /// 使用者資訊清單
    /// </summary>
    /// <remarks>
    /// 此端點需要使用者具備管理員或內部使用者角色才能存取
    /// </remarks>
    /// <returns>使用者資訊清單。</returns>
    [SwaggerOperation( 
        Summary = "Get all users for internal users only"
    )]
    [HttpGet("user")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IEnumerable<UserInfoDTO> Get() 
    {
        var users = _userService.GetAllUsersIncludeAll();
        var usersInfo = _mapper.Map<List<UserInfoDTO>>(users);
        return usersInfo;
    }

    [SwaggerOperation(
        Summary = "Get specific user for internal users only"
    )]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<UserInfoDTO>> GetUser(Guid userId)
    {
        var user = _userService.GetUserByIdIncludeAll(userId);

        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var userInfoDTO = _mapper.Map<UserInfoDTO>(user);
        return Ok(userInfoDTO);
    }

    [SwaggerOperation(
         Summary = "Get user's managed activities for internal users only"
     )]
    [HttpGet("user/manage")]
    public async Task<ActionResult<ActivitySegmentResponseDTO>> GetUserManageActivity([FromQuery] Guid userId, [FromQuery] ActivitySegmentDTO segmentRequest)
    {
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

        var SegmentResponse = _mapper.Map<ActivitySegmentResponseDTO>(segmentRequest);

        SegmentResponse.SearchData = activityDTOList;
        SegmentResponse.TotalPage = (totalCount / segmentRequest.CountPerPage) + 1;
        SegmentResponse.TotalData = totalCount;

        return Ok(SegmentResponse);
    }

    /// <summary>
    /// 新增活動
    /// </summary>
    /// <param name="activityPostDTOs">欲新增的活動資料</param>
    /// <returns>新增成功的活動資料</returns>
    /// <response code="200">成功回傳新增成功的活動資料</response>
    /// <response code="401">使用者未登入，無法新增活動</response>
    [SwaggerOperation(
        Summary = "Post activities for internal users only"
    )]
    [HttpPost("activity")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ActivityDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ActivityDTO>>> PostActivities(List<ActivityPostDTO> activityPostDTOs)
    {
        var activities = _mapper.Map<List<Activity>>(activityPostDTOs);

        // 獲取全部 tag
        var tagList = activities.Where(x => x.Tags != null).SelectMany(x => x.Tags).DistinctBy(x => new { x.Type, x.Text }).ToList();
        // 獲取全部 location
        var locationList = activities.SelectMany(ac => ac.Branches).Where(x => x.Location != null).SelectMany(b => b.Location).DistinctBy(x => x.Content).ToList();

        foreach (var location in locationList)
        {
            // 追蹤 location 實體
            var existingLocation = _locationService.GetByContent(location.Content);
            if (existingLocation == null)
            {
                // 加入 location 實體
                await _locationService.AddAsync(location);
            }
        }

        foreach (var tagDTO in tagList)
        {
            // 追蹤 tag 實體
            var existingTag = _tagService.GetTagByTextType(tagDTO.Text, tagDTO.Type);
            if (existingTag == null)
            {
                // 加入 tag 實體
                await _tagService.AddAsync(tagDTO);
            }
        }

        foreach (var activity in activities)
        {
            // 替換 activity 的 tag 為正在追蹤的實體
            for (int i = 0; i < activity.Tags?.Count; i++)
            {
                var tag = activity.Tags[i];
                var tagFind = _tagService.GetLocal().FirstOrDefault(x => x.Type == tag.Type && x.Text == tag.Text);
                if (tagFind != null)
                    activity.Tags[i] = tagFind;
            }

            for (int i = 0; i < activity.Branches.Count; i++)
            {
                for (int j = 0; j < activity.Branches[i].Location?.Count; j++)
                {
                    var location = activity.Branches[i].Location[j];
                    var locationFind = _locationService.GetLocal().FirstOrDefault(x => x.Content == location.Content);
                    if (locationFind != null)
                        activity.Branches[i].Location[j] = locationFind;
                }
            }
            await _activityService.AddAsync(activity);
        }

        await _activityService.SaveChangesAsync();

        var activityDTOs = _mapper.Map<List<ActivityDTO>>(activities);
        return activityDTOs;
    }

    /// <summary>
    /// 刪除活動
    /// </summary>
    /// <param name="ids">欲刪除的活動 ID</param>
    /// <remark>如果不給 ID 會直接刪除所有活動</remark>
    /// <returns>No Content</returns>
    /// <response code="204">成功刪除</response>
    /// <response code="401">使用者未登入</response>
    [SwaggerOperation(
        Summary = "Delete activity for internal users only"
    )]
    [HttpDelete("activity")]
    public async Task<IActionResult> DeleteActivities([FromQuery] Guid[]? ids)
    {
        if (ids == null || ids.Length == 0)
        {
            var activities = _activityService.GetAll();
            _activityService.RemoveRange(activities);
        }
        else
        {
            var activities = _activityService.GetAll().Where(x => ids.Contains(x.Id));
            _activityService.RemoveRange(activities);
        }

        await _activityService.SaveChangesAsync();
        return NoContent();
    }

    [SwaggerOperation(
        Summary = "Post Professions for internal users only"
    )]
    [HttpPost("professions")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PostProfessions([FromBody] List<string> professions)
    {
        var newProfessions = new List<string>(){ };
        var existProfessions = new List<string>(){ };

        for (int i =0; i< professions.Count; i++)
        {
            var professionName = professions[i];
            var profession = await _professionService.GetByNameAsync(professionName);
            if (profession == null && !professionName.IsNullOrEmpty() && !_professionService.GetLocal().Any(p => p.Content == professionName))
            {
                _professionService.Add(new Profession
                {
                    Content = professionName
                });
                newProfessions.Add(professionName);
            }
            else
            {
                existProfessions.Add(professionName);
            }
        }
    
        await _professionService.SaveChangesAsync();

        return Ok($"新增的職業: {string.Join(", ", newProfessions)};已存在的職業: {string.Join(", ", existProfessions)}");
    }

    [SwaggerOperation(
        Summary = "Delete Professions for internal users only"
    )]
    [HttpDelete("professions")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteProfessions([FromBody] List<string> professions)
    {
        var notExistProfessions = new List<string>() { };
        var deletedProfessions = new List<string>() { };

        for (int i = 0; i < professions.Count; i++)
        {
            var professionName = professions[i];
            var profession = await _professionService.GetByNameAsync(professionName);
            if (profession == null)
            {
                notExistProfessions.Add(professionName);
            }
            else
            {
                _professionService.Delete(profession);
                deletedProfessions.Add(professionName);
            }
        }

        await _professionService.SaveChangesAsync();

        return Ok($"不存在的職業: {string.Join(", ", notExistProfessions)};已刪除的職業: {string.Join(", ", deletedProfessions)}");
    }

    [SwaggerOperation(
         Summary = "Post Professions for internal users only"
     )]
    [HttpPost("locations")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PostLocation([FromBody] List<CountyPostDTO> countyDTOs)
    {
        for (int i = 0; i < countyDTOs.Count; i++)
        {
            var countyName = countyDTOs[i].CityName;
            var county = await _countyService.GetByNameAsync(countyName);

            if (county != null)
            {
                county.Areas ??= new List<Area>();
                countyDTOs[i].Areas.ForEach(a => {
                    var area = _mapper.Map<Area>(a);
                    if (!county.Areas.Any(x => x.ZipCode == area.ZipCode ))
                    {
                        county.Areas.Add(area);
                    }
                });
                _countyService.Update(county);
            } else
            {
                county = _mapper.Map<County>(countyDTOs[i]);
                _countyService.Add(county);
            }
        }

        await _countyService.SaveChangesAsync();

        return Ok();
    }


    [SwaggerOperation(
         Summary = "Test For Dev"
     )]
    [HttpGet("TestMiddleware")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestMiddelware()
    {
        throw new UserNotFoundException();
        return Ok();
    }

    private static void CheckOrderByValue(string? orderBy)
    {
        var orderByList = new HashSet<string>() { "descending", "ascending" };

        if (orderBy != null && !orderByList.Contains(orderBy.ToLower()))
        {
            throw new BadRequestException($"OrderBy 參數錯誤，可用的參數: {string.Join(", ", orderByList)}");
        }
    }

    [HttpPost("tag")]
    public async Task<IActionResult> PostTags([FromBody] IEnumerable<TagPostDTO> tagDTOs)
    {
        foreach (var tagDTO in tagDTOs)
        {
            var tag = await _tagService.GetTagByTextAsync(tagDTO.Text);

            // 新增 Tag
            if (tag == null)
            {
                var tagToSave = _mapper.Map<Tag>(tagDTO);
                _tagService.Add(tagToSave);
            }
        }

        _tagService.SaveChangesAsync();

        return Ok();
    }

}
