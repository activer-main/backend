using ActiverWebAPI.Exceptions;
using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Linq.Expressions;

namespace ActiverWebAPI.Services.UserServices;

public class UserService : GenericService<User, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IConfiguration _configuration;

    public UserService(IUnitOfWork unitOfWork, IConfiguration configuration) : base(unitOfWork)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _userRepository = _unitOfWork.Repository<User, Guid>();
    }

    /// <summary>
    /// 以 Email 取得 User 資訊
    /// </summary>
    /// <param name="email">Email</param>
    /// <returns>對應的 User</returns>
    public async Task<User>? GetUserByEmailAsync(string email, params Expression<Func<User, object>>[] includes)
    {
        var query = _userRepository.Query();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return await query.FirstOrDefaultAsync(e => e.Email == email);
    }

    /// <summary>
    /// 以 id 產生 Avatar URL
    /// </summary>
    /// <param name="id">User Id</param>
    /// <returns>Avatar URL</returns>
    public async Task<string>? GetUserAvatarURLAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId, e => e.Avatar);

        if (user == null)
            return null;

        if (user.Avatar == null)
            return null;

        return _configuration["Server:Domain"] + $"/api/user/avatar/{user.Avatar.Id}";
    }

    public async Task<IEnumerable<SearchHistory>>? GetSearchHistory(Guid userId)
    {
        var query = _userRepository.Query()
            .Include(x => x.SearchHistory)
                .ThenInclude(x => x.Tags);
        var user = await query.FirstOrDefaultAsync(e => e.Id == userId);
        if (user == null)
        {
            throw new UserNotFoundException();
        }
        return user.SearchHistory;
    }

    public void CheckUserPassword(string password)
    {
        // 加入 password 的規範
    }

    public async Task<Dictionary<Guid, KeyValuePair<string, DateTime>>> GetUserActivityStatusAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId, u => u.ActivityStatus);
        return user?.ActivityStatus?.ToDictionary(a => a.ActivityId, a => new KeyValuePair<string, DateTime>(a.Status, a.CreatedAt));
    }

    public void SaveSearchHistory(User user, SearchHistory searchHistory)
    {
        user.SearchHistory ??= new List<SearchHistory>() { };
        user.SearchHistory.Add(searchHistory);
        Update(user);
    }

    public bool VerifyEmailVerificationCode(User User, string token)
    {
        if (User.UserEmailVerifications == null)
            return false;
        var result = User.UserEmailVerifications.Any(e => e.VerificationCode == token && e.ExpiresTime > DateTime.UtcNow);

        // 刪除驗證碼
        if (result)
        {
            User.UserEmailVerifications.RemoveAll(e => e.VerificationCode == token || e.ExpiresTime > DateTime.UtcNow);
        }
        Update(User);
        return result;
    }

    public bool VerifyResetPasswordVerificationCodeAvailable(User User, string token)
    {
        if (User.ResetPasswordTokens == null)
            return false;
        var result = User.ResetPasswordTokens.Any(e => e.Token == token && e.ExpiresTime > DateTime.UtcNow);

        // 刪除驗證碼
        if (result)
        {
            User.ResetPasswordTokens.RemoveAll(e => e.Token == token || e.ExpiresTime > DateTime.UtcNow);
        }
        Update(User);
        return result;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        Random random = new();
        var length = 6;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string token;

        do
        {
            token = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        } while (!IsEmailVerificationCodeAvailable(user, token));

        user.UserEmailVerifications ??= new List<UserEmailVerification>() { };
        user.UserEmailVerifications.Add(new UserEmailVerification
        {
            VerificationCode = token,
            ExpiresTime = DateTime.UtcNow.AddMinutes(10),
        });
        Update(user);
        return token;
    }

    public async Task<string> GenerateResetTokenAsync(User user)
    {
        Random random = new();
        var length = 6;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string token;

        do
        {
            token = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        } while (!IsResetPasswordVerificationCodeAvailable(user, token));

        user.ResetPasswordTokens ??= new List<UserResetPasswordToken>() { };
        user.ResetPasswordTokens.Add(new UserResetPasswordToken
        {
            Token = token,
            ExpiresTime = DateTime.UtcNow.AddMinutes(10),
        });
        Update(user);
        return token;
    }

    private static bool IsResetPasswordVerificationCodeAvailable(User User, string token)
    {
        if (User.ResetPasswordTokens == null)
            return true;
        return !User.ResetPasswordTokens.Any(e => e.Token == token);
    }

    private static bool IsEmailVerificationCodeAvailable(User User, string token)
    {
        if (User.UserEmailVerifications == null)
            return true;
        return !User.UserEmailVerifications.Any(e => e.VerificationCode == token);
    }

}