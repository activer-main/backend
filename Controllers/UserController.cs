using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ActiverWebAPI.Controllers;


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public UserController(UserService userService, IMapper mapper, IWebHostEnvironment env)
    {
        _userService = userService;
        _mapper = mapper;
        _env = env;
    }

    /// <summary>
    /// Get a list of user information for users who have roles of Admin or InternalUser.
    /// </summary>
    /// <remarks>
    /// This endpoint requires the user to have a role of Admin or InternalUser to access.
    /// </remarks>
    /// <returns>A list of user information.</returns>
    [Authorize(Roles = "Admin, InternalUser")]
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IEnumerable<UserInfoDTO> Get()
    {
        var users = _userService.GetAll();
        var usersInfo = _mapper.Map<List<UserInfoDTO>>(users);
        return usersInfo;
    }

    /// <summary>
    /// 上傳使用者頭像
    /// </summary>
    /// <remarks>
    /// 頭像檔案上傳至 WebRootPath 下的 Sources/avatars 中
    /// </remarks>
    /// <param name="file">欲上傳的檔案</param>
    /// <returns></returns>
    /// <response code="200">檔案上傳成功</response>
    /// <response code="400">未選擇檔案或不支援此類型檔案</response>
    /// <response code="401">未授權的存取</response>
    /// <response code="404">找不到使用者</response>
    [Authorize]
    [HttpPost("uploadAvatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userService.GetByIdAsync(userId);

        // 確認 User 存在
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // 確認有選擇檔案
        if (file == null || file.Length == 0)
        {
            return BadRequest("未選擇檔案");
        }

        // 檢查檔案類型
        if (!IsImage(file))
        {
            return BadRequest("不支援此類型檔案");
        }

        // 產生檔名
        var fileName = $"{Guid.NewGuid()}.{Path.GetExtension(file.FileName)}";

        // 設定檔案路徑
        var filePath = Path.Combine(_env.WebRootPath, "Sources", "avatars", fileName);

        // 確認路徑是否存在
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        // 儲存檔案
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var avatar = new Avatar
        {
            Length = file.Length,
            Filename = fileName,
            FileType = file.ContentType,
            FilePath = filePath
        };

        // 更新使用者資料庫中的圖片
        user.Avatar = avatar;
        await _userService.UpdateAsync(user);

        return Ok("檔案上傳成功");
    }

    /// <summary>
    /// 取得使用者的頭像。
    /// </summary>
    /// <param name="userId">使用者 ID。</param>
    /// <returns>頭像檔案。</returns>
    [AllowAnonymous]
    [HttpGet("avatar/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAvatar(Guid userId)
    {
        var user = await _userService.GetByIdAsync(userId, user => user.Avatar);

        if (user == null)
            return NotFound();

        if (user.Avatar == null)
            return NotFound("我還沒做好默認頭貼");

        string filePath = user.Avatar.FilePath;
        string contentType = user.Avatar.FileType;

        // 檢查檔案是否存在
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("檔案不存在");
        }

        // 設定回傳結果
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(fileStream, contentType);
    }

    private static bool IsImage(IFormFile file)
    {
        return file.ContentType.StartsWith("image/");
    }
}
