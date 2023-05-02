using ActiverWebAPI.Enums;
using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Interfaces.Service;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using ActiverWebAPI.Services.Filters;
using ActiverWebAPI.Services.Middlewares;
using ActiverWebAPI.Services.UserServices;
using ActiverWebAPI.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : BaseController
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly AreaService _areaService;
    private readonly ProfessionService _professionService;
    private readonly CountyService _countyService;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public UserController(UserService userService,
        IMapper mapper,
        IWebHostEnvironment env,
        TokenService tokenService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        AreaService areaService,
        ProfessionService professionService,
        CountyService countyService)
    {
        _userService = userService;
        _mapper = mapper;
        _env = env;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _areaService = areaService;
        _professionService = professionService;
        _countyService = countyService;
    }

    [AllowAnonymous]
    [HttpGet("isEmailValid")]
    public async Task<ActionResult<bool>> IsEmailValid(string Email)
    {
        var user = await _userService.GetUserByEmailAsync(Email);
        return Ok(user != null);
    }

    /// <summary>
    /// 使用者資訊清單
    /// </summary>
    /// <remarks>
    /// 此端點需要使用者具備管理員或內部使用者角色才能存取
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
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId,
            user => user.Avatar,
            user => user.Area,
            user => user.Professions,
            user => user.SearchHistory,
            user => user.TagStorage,
            user => user.UserActivityRecords
            );

        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var userInfoDTO = _mapper.Map<UserInfoDTO>(user);
        return Ok(userInfoDTO);
    }

    /// <summary>
    /// 更新使用者部分資訊
    /// </summary>
    /// <remarks>
    /// 修改欄位限制為：username, gender, birthday, profession, phone, county, area
    /// </remarks>
    /// <param name="patchDoc">包含要更新的使用者部分資訊</param>
    /// <returns>更新後的使用者資訊</returns>
    /// <response code="200">更新成功，回傳更新後的使用者資訊</response>
    /// <response code="400">請求資料無效，回傳錯誤訊息</response>
    /// <response code="401">未授權，回傳錯誤訊息</response>
    [Authorize]
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDTO>> UpdateUser([FromBody] UserUpdateDTO patchDoc)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedException("使用者驗證失敗");
        }

        // 更新 Username
        if (!string.IsNullOrEmpty(patchDoc.Username))
        {
            user.Username = patchDoc.Username;
        }

        // 更新性別
        if (!string.IsNullOrEmpty(patchDoc.Gender))
        {
            if (!Enum.TryParse(patchDoc.Gender, true, out UserGender gender))
            {
                throw new BadRequestException("使用者的性別未定義，請聯絡客服");
            }
            user.Gender = (int)gender;
        }

        // 更新生日
        if (patchDoc.Birthday != null)
        {
            user.Birthday = DateTime.ParseExact(patchDoc.Birthday, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        // 更新職業
        if (!patchDoc.Professions.IsNullOrEmpty())
        {
            var professionList = patchDoc.Professions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            List<Profession> newProfessions = new ();
            foreach(var professionName in professionList)
            {
                var profession = await _professionService.GetByNameAsync(professionName);
                if (profession != null) {
                    newProfessions.Add(profession);
                }
                else
                {
                    newProfessions.Add(new Profession
                    {
                        Content = professionName
                    });
                }
            }
            user.Professions = newProfessions;
        }

        // 更新手機
        if (!string.IsNullOrEmpty(patchDoc.Phone))
        {
            user.Phone = patchDoc.Phone;
        }

        // 更新國家
        if (!string.IsNullOrEmpty(patchDoc.County))
        {
            var county = await _countyService.GetByNameAsync(patchDoc.County);
            if (county == null)
            {
                throw new BadRequestException($"County: {patchDoc.County} 不在選項中");
            }
        }   

        // 更新地區
        if (!string.IsNullOrEmpty(patchDoc.Area))
        {
            var area = await _areaService.GetByNameAsync(patchDoc.Area);
            if (area != null)
                user.Area = area;
            else user.Area = new Area() { 
                Content = patchDoc.Area
            };
        }

        _userService.Update(user);
        await _userService.SaveChangesAsync();
        var userInfoDTO = _mapper.Map<UserInfoDTO>(user);
        return Ok(userInfoDTO);
    }

    /// <summary>
    /// 使用者登入 API
    /// </summary>
    /// <param name="userSignInDto">使用者登入資訊</param>
    /// <returns>使用者資訊與權杖</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> Login(UserSignInDTO userSignInDto)
    {
        // 從資料庫中尋找使用者
        var user = await _userService.GetUserByEmailAsync(userSignInDto.Email.ToLower(),
            user => user.Avatar,
            user => user.Professions,
            user => user.County,
            user => user.Area);
        if (user == null)
        {
            throw new BadRequestException("帳號或密碼錯誤");
        }

        // 驗證密碼
        var passwordValid = _passwordHasher.VerifyHashedPassword(user.HashedPassword, userSignInDto.Password);
        if (!passwordValid)
        {
            throw new UnauthorizedException("帳號或密碼錯誤");
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
        if (await _userService.GetUserByEmailAsync(signUpDTO.Email.ToLower()) != null)
        {
            throw new BadRequestException("此電子郵件已被註冊");
        }

        var user = _mapper.Map<User>(signUpDTO);
        await _userService.AddAsync(user);
        await _userService.SaveChangesAsync();

        // 發送驗證電子郵件
        var token = await _userService.GenerateEmailConfirmationTokenAsync(user);
        var subject = "[noreply] Activer 註冊驗證碼";
        var message = $"<h1>您的驗證碼是: {token} ，此驗證碼於10分鐘後失效</h1>";
        await _emailService.SendEmailAsync(user.Email, subject, message);

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
    //[TypeFilter(typeof(EmailVerificationActionFilter))]
    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, user => user.Avatar);

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        // 確認有選擇檔案
        if (file == null || file.Length == 0)
        {
            throw new BadRequestException("未選擇檔案");
        }

        // 檢查檔案類型
        if (!DataHelper.IsImage(file))
        {
            throw new BadRequestException("不支援此類型檔案");
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
        await _userService.SaveChangesAsync();
        return Ok("檔案上傳成功");
    }

    /// <summary>
    /// 刪除使用者頭像
    /// </summary>
    /// <returns>回傳是否刪除成功的訊息</returns>
    /// <response code="200">成功刪除使用者頭像</response>
    /// <response code="401">未授權的請求</response>
    /// <response code="404">找不到指定的使用者</response>
    [Authorize]
    [TypeFilter(typeof(EmailVerificationActionFilter))]
    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        var user = await _userService.GetByIdAsync(userId, user => user.Avatar);

        // 確認 User 存在
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        // 確認 User 已有頭像
        if (user.Avatar == null)
        {
            throw new BadRequestException("使用者尚未設定頭像。");
        }

        // 刪除檔案
        var filePath = user.Avatar.FilePath;
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        // 更新使用者資料庫中的圖片
        user.Avatar = null;
        _userService.Update(user);
        await _userService.SaveChangesAsync();
        return Ok("成功刪除使用者頭像");
    }

    /// <summary>
    /// 取得使用者的頭像
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>頭像檔案</returns>
    [AllowAnonymous]
    [HttpGet("avatar/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAvatar(Guid userId)
    {
        var user = await _userService.GetByIdAsync(userId, user => user.Avatar);

        if (user == null)
            throw new UserNotFoundException();

        if (user.Avatar == null)
            throw new NotFoundException("我還沒做好默認頭貼");

        string filePath = user.Avatar.FilePath;
        string contentType = user.Avatar.FileType;

        // 檢查檔案是否存在
        if (!System.IO.File.Exists(filePath))
        {
            throw new NotFoundException("檔案不存在");
        }

        // 設定回傳結果
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(fileStream, contentType);
    }

    /// <summary>
    /// 刪除使用者
    /// </summary>
    /// <remarks>
    /// 此端點需要使用者具備管理員或內部使用者角色才能存取
    /// </remarks>
    /// <param name="id">使用者ID</param>
    /// <returns>刪除是否成功</returns>
    /// <response code="200">成功刪除使用者</response>
    /// <response code="404">找不到指定的使用者</response>
    [Authorize(Roles = "Admin, InternalUser")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            throw new UserNotFoundException();
        }
        _userService.Delete(user);
        await _userService.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// 驗證 email
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="code">驗證碼</param>
    /// <returns>ActionResult</returns>
    [HttpGet("verifyEmail")]
    [Authorize]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 401)]
    [Produces("application/json")]
    public async Task<ActionResult> VerifyEmail(string verifyCode)
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;
        // 檢查參數是否為 null 或空字串
        if (string.IsNullOrEmpty(verifyCode))
        {
            throw new BadRequestException("verifyCode 不得為空");
        }

        // 驗證電子郵件驗證碼
        var user = await _userService.GetByIdAsync(userId, 
            user => user.UserEmailVerifications);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var result = _userService.VerifyVerificationCode(user, verifyCode);
        if (!result)
        {
            throw new BadRequestException("驗證碼不正確或已失效");
        }
        user.Verified = true;
        _userService.Update(user);
        await _userService.SaveChangesAsync();
        return Ok("電子郵件驗證成功");
    }

    /// <summary>
    /// 重新發送驗證郵件
    /// </summary>
    /// <remarks>
    /// 需要授權
    /// </remarks>
    /// <returns>發送成功回傳 200，授權失敗回傳 401</returns>
    [Authorize]
    [HttpGet("resendVerifyEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ResendVerifyEmail()
    {
        var userId = (Guid?)ViewData["UserId"] ?? Guid.Empty;

        // 驗證電子郵件驗證碼
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        // 發送驗證電子郵件
        var token = await _userService.GenerateEmailConfirmationTokenAsync(user);
        var subject = "[noreply] Activer 註冊驗證碼";
        var message = $"<h1>您的驗證碼是: {token} ，此驗證碼於10分鐘後失效</h1>";
        await _emailService.SendEmailAsync(user.Email, subject, message);
        return Ok();
    }
}
