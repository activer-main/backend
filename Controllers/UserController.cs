using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Models.DBEntity;
using Microsoft.AspNetCore.Mvc;
namespace ActiverWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public UserController(IUnitOfWork unitOfWork)
    {
        _uow = unitOfWork;
    }

    [HttpGet]
    public IQueryable<User> Get()
    {
        return _uow.Repository<User>().GetAll();
    }
}
