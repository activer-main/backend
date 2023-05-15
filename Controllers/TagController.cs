using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.TagServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActiverWebAPI.Controllers;

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
    public IEnumerable<TagDTO> GetAllTags()
    {
        var tags = _tagService.GetAll(t => t.UserVoteTagInActivity, t => t.Activities);
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
}
