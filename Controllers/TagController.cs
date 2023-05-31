using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.TagServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ActiverWebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TagController : BaseController
{
    private readonly TagService _tagService;
    private readonly IMapper _mapper;

    public TagController(TagService tagService, IMapper mapper)
    {
        _tagService = tagService;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpGet]
    public IEnumerable<TagDTO> GetAllTags([FromQuery] TagFilterDTO filter)
    {
        var tags = _tagService.GetAll(t => t.UserVoteTagInActivity, t => t.Activities);
        if (!filter.Type.IsNullOrEmpty())
        {
            tags = tags.Where(t => filter.Type.Contains(t.Type));
        }
        if (!filter.Key.IsNullOrEmpty())
        {
            tags = tags.Where(t => t.Text.Contains(filter.Key));
        }

        var tagsDTO = _mapper.Map<IEnumerable<TagDTO>>(tags);
        return tagsDTO;
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<TagDTO> GetTag(int id)
    {
        var tag = await _tagService.GetByIdAsync(id, t => t.UserVoteTagInActivity, t => t.Activities);
        if(tag == null)
        {
            throw new TagNotFoundException(id);
        }
        var tagDTO = _mapper.Map<TagDTO>(tag);

        // 增加 tag Trend
        _tagService.AddTagTrendCount(tag);
        _tagService.Update(tag);
        await _tagService.SaveChangesAsync();

        return tagDTO;
    }

    [AllowAnonymous]
    [HttpGet("Type")]
    public async Task<IEnumerable<string>> GetAllTagType()
    {
        var tagTypes = _tagService.GetAll().Select(t => t.Type).Distinct();
        return tagTypes;
    }
}
