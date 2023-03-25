using ActiverWebAPI.Interfaces.Service;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services;
using ActiverWebAPI.Services.Middlewares;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActiverWebAPI.Controllers;


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public UserController(UserService userService,
        IMapper mapper,
        IWebHostEnvironment env,
        TokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userService = userService;
        _mapper = mapper;
        _env = env;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 取得擁有管理員或內部使用者角色的使用者資訊清單。
    /// </summary>
    /// <remarks>
    /// 此端點需要使用者具備管理員或內部使用者角色才能存取。
    /// </remarks>
    /// <returns>使用者資訊清單。</returns>
    [Authorize(Roles = "Admin, InternalUser")]
    [HttpGet("all")]
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
    /// 取得當前已登入的使用者資訊
    /// </summary>
    /// <returns>使用者資訊</returns>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDTO>> GetUser()
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userService.GetByIdAsync(userId,
            user => user.Avatar,
            user => user.Area,
            user => user.Gender,
            user => user.Professions,
            user => user.SearchHistory,
            user => user.TagStorage,
            user => user.UserActivityRecords
            );

        if (user == null)
        {
            return NotFound("使用者不存在");
        }

        var userInfoDTO = _mapper.Map<UserInfoDTO>(user);
        return Ok(userInfoDTO);
    }

    /// <summary>
    /// 使用者登入 API
    /// </summary>
    /// <param name="userSignInDto">使用者登入資訊</param>
    /// <returns>使用者資訊與權杖</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> Login(UserSignInDTO userSignInDto)
    {
        // 從資料庫中尋找使用者
        var user = await _userService.GetUserByEmailAsync(userSignInDto.Email);
        if (user == null)
        {
            return BadRequest("帳號或密碼錯誤");
        }

        // 驗證密碼
        var passwordValid = _passwordHasher.VerifyHashedPassword(user.HashedPassword, userSignInDto.Password);
        if (!passwordValid)
        {
            return Unauthorized("帳號或密碼錯誤");
        }

        // 轉換為 UserDTO 回傳
        var userInfoDTO = _mapper.Map<UserInfoDTO>(user);
        var TokenDTO = _tokenService.GenerateToken(user);
        var userDTO = _mapper.Map<UserDTO>(userInfoDTO);
        _mapper.Map(TokenDTO, userDTO);

        return Ok(userDTO);
    }

    /// <summary>
    /// 建立使用者帳戶
    /// </summary>
    /// <remarks>
    /// 使用者註冊後會建立帳戶，若電子郵件已被註冊則會回傳 BadRequest。
    /// </remarks>
    /// <param name="signUpDto">使用者註冊資訊</param>
    /// <returns>建立成功後的使用者資訊和 Token</returns>
    /// <response code="200">建立成功</response>
    /// <response code="400">電子郵件已被註冊</response>
    [AllowAnonymous]
    [HttpPost("signup")]
    [ProducesResponseType(typeof(UserDTO), 201)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<ActionResult<UserDTO>> SignUp([FromBody] UserSignUpDTO signUpDTO)
    {
        if (await _userService.GetUserByEmailAsync(signUpDTO.Email) != null)
        {
            return BadRequest("此電子郵件已被註冊");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = _mapper.Map<User>(signUpDTO);
        await _userService.AddAsync(user);

        var userInfoDTO = _mapper.Map<UserInfoDTO>(user);
        var TokenDTO = _tokenService.GenerateToken(user);
        var userDTO = _mapper.Map<UserDTO>(userInfoDTO);
        _mapper.Map(TokenDTO, userDTO);

        return userDTO;
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
    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userService.GetByIdAsync(userId, user => user.Avatar);

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

        // 確認是否有 Avatar
        if (user.Avatar != null)
        {
            // 刪除檔案
            var filePathToDelete = user.Avatar.FilePath;
            if (System.IO.File.Exists(filePathToDelete))
            {
                System.IO.File.Delete(filePathToDelete);
            }
        }
        
        // 產生檔名
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        // 設定檔案路徑
        var filePath = Path.Combine(_env.WebRootPath, "avatars", fileName);

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
        _userService.Update(user);

        return Ok("檔案上傳成功");
    }

    /// <summary>
    /// 刪除使用者頭像。
    /// </summary>
    /// <returns>回傳是否刪除成功的訊息。</returns>
    /// <response code="200">成功刪除使用者頭像。</response>
    /// <response code="401">未授權的請求。</response>
    /// <response code="404">找不到指定的使用者。</response>
    [Authorize]
    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userService.GetByIdAsync(userId, user => user.Avatar);

        // 確認 User 存在
        if (user == null)
        {
            return NotFound("找不到指定的使用者。");
        }

        // 確認 User 已有頭像
        if (user.Avatar == null)
        {
            return BadRequest("使用者尚未設定頭像。");
        }

        // 刪除檔案
        var filePath = user.Avatar.FilePath;
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        // 更新使用者資料庫中的圖片
        user.Avatar = null;
        await _userService.UpdateAsync(user);

        return Ok("成功刪除使用者頭像。");
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

    /// <summary>
    /// 刪除使用者
    /// </summary>
    /// <remarks>刪除指定使用者</remarks>
    /// <param name="id">使用者ID</param>
    /// <returns>刪除是否成功</returns>
    /// <response code="200">成功刪除使用者</response>
    /// <response code="404">找不到指定的使用者</response>
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        await _userService.DeleteAsync(user);
        return Ok();
    }

    private static bool IsImage(IFormFile file)
    {
        return file.ContentType.StartsWith("image/");
    }
}
