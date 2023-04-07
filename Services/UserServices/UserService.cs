using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;
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
    public async Task<User?> GetUserByEmailAsync(string email, params Expression<Func<User, object>>[] includes)
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

    public bool VerifyVerificationCode(User User, string token)
    {
        if (User.UserEmailVerifications == null)
            return false;
        return User.UserEmailVerifications.Any(e => e.VerificationCode == token && e.ExpiresTime > DateTime.UtcNow);
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
        } while (!IsVerificationCodeAvailable(user, token));

        user.UserEmailVerifications ??= new List<UserEmailVerification>() { };
        user.UserEmailVerifications.Add(new UserEmailVerification
        {
            VerificationCode = token,
            ExpiresTime = DateTime.UtcNow.AddMinutes(10),
        });
        Update(user);
        await SaveChangesAsync();
        return token;
    }

    private static bool IsVerificationCodeAvailable(User User, string token)
    {
        if (User.UserEmailVerifications == null)
            return true;
        return !User.UserEmailVerifications.Any(e => e.VerificationCode == token);
    }
}