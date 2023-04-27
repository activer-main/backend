using ActiverWebAPI.Services.UserServices;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActiverWebAPI.Services.Filters;

public class EmailVerificationActionFilter : IAsyncActionFilter
{
    private readonly UserService _userService;

    public EmailVerificationActionFilter(UserService userService)
    {
        _userService = userService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var user = _userService.GetById(Guid.Parse(userId));
        if (user == null || !user.Verified)
        {
            context.Result = new UnauthorizedObjectResult("該使用者電子郵箱未驗證");
            return;
        }

        await next();
    }
}
