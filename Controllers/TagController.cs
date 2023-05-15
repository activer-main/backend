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
        var tags = _tagService.GetAll();
        var tagsDTO = _mapper.Map<IEnumerable<TagDTO>>(tags);
        return tagsDTO;
    }
}
