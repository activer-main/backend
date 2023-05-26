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
using System.Linq;
using ActiverWebAPI.Exceptions;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InternalController : BaseController
{
    private readonly ActivityService _activityService;
    private readonly ProfessionService _professionService;
    private readonly CountyService _countyService;
    private readonly TagService _tagService;
    private readonly LocationService _locationService;
    private readonly IMapper _mapper;

    public InternalController(
        ActivityService activityService,
        UserService userService,
        ProfessionService professionService,
        CountyService countyService,
        TagService tagService,
        LocationService locationService,
        IMapper mapper
    )
    {
        _activityService = activityService;
        _countyService = countyService;
        _tagService = tagService;
        _locationService = locationService;
        _professionService = professionService;
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

    [Authorize(Roles = "Admin, InternalUser")]
    [SwaggerOperation(
        Summary = "Post Professions for internal users only"
    )]
    [HttpPost("Professions")]
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

    [Authorize(Roles = "Admin, InternalUser")]
    [SwaggerOperation(
        Summary = "Delete Professions for internal users only"
    )]
    [HttpDelete("Professions")]
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

    [Authorize(Roles = "Admin, InternalUser")]
    [SwaggerOperation(
         Summary = "Post Professions for internal users only"
     )]
    [HttpPost("Locations")]
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
}
