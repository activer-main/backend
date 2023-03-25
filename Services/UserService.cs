using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Services;

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
    /// 以 Email 取得 User 資訊。
    /// </summary>
    /// <param name="email">Email。</param>
    /// <returns>對應的 User。</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var user = await _userRepository
            .GetAll(e => e.Email == email)
            .FirstOrDefaultAsync();
        return user;
    }

    /// <summary>
    /// 以 id 產生 Avatar URL。
    /// </summary>
    /// <param name="id">User Id。</param>
    /// <returns>Avatar URL</returns>
    public string GetUserAvatarURL(Guid userId)
    {
        var user = _userRepository.GetAll(user => user.Id == userId)
            .Include(e => e.Avatar).FirstOrDefault();

        if (user == null)
            return null;

        if (user.Avatar == null)
            return null;

        return _configuration["Server:Domain"] + $"/api/user/avatar/{user.Avatar.Id}";
    }
}
