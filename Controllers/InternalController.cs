using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.TagServices;
using ActiverWebAPI.Services.UserServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ActiverWebAPI.Models.DBEntity;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InternalController : BaseController
{
    private readonly ActivityService _activityService;
    private readonly UserService _userService;
    private readonly TagService _tagService;
    private readonly IMapper _mapper;

    public InternalController(
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
        await _activityService.AddRangeAsync(activities);
        await _activityService.SaveChangesAsync();
        var activityDTOs = _mapper.Map<List<ActivityDTO>>(activities);
        return activityDTOs;
    }
}
