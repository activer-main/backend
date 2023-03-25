using ActiverWebAPI.Interfaces.Repository;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Services;

public class UserService : GenericService<User>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<User> _userRepository;

    public UserService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _userRepository = _unitOfWork.Repository<User>();
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
}
