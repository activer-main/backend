using System.Security.Claims;

namespace ActiverWebAPI.Services.Middlewares;

public class EmailVerificationMiddleware : IMiddleware
{
    private readonly UserService _userService;

    public EmailVerificationMiddleware(UserService userService)
    {
        _userService = userService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var user = await _userService.GetByIdAsync(Guid.Parse(userId));
        if (user == null || !user.Verified)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("該使用者電子郵箱未驗證");
            return;
        }

        await next(context);
    }
}
