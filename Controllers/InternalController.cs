using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.TagServices;
using ActiverWebAPI.Services.UserServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ActiverWebAPI.Models.DBEntity;
using Azure;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InternalController : BaseController
{
    private readonly ActivityService _activityService;
    private readonly UserService _userService;
    private readonly TagService _tagService;
    private readonly LocationService _locationService;
    private readonly IMapper _mapper;

    public InternalController(
        ActivityService activityService,
        UserService userService,
        TagService tagService,
        LocationService locationService,
        IMapper mapper
    )
    {
        _activityService = activityService;
        _userService = userService;
        _tagService = tagService;
        _locationService = locationService;
        _mapper = mapper;
    }

    /// <summary>
    /// 新增活動
    /// </summary>
    /// <param name="activityPostDTOs">欲新增的活動資料</param>
    /// <returns>新增成功的活動資料</returns>
    /// <response code="200">成功回傳新增成功的活動資料</response>
    /// <response code="401">使用者未登入，無法新增活動</response>
    [Authorize(Roles = "Admin, InternalUser")]
    [SwaggerOperation(
        Summary = "Post activities for internal users only"
    )]
    [HttpPost("Activity")]
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

        foreach(var location in locationList)
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
}
