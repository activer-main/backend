using ActiverWebAPI.Services.UserServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ActiverWebAPI.Services.Filters;

public class PasswordChangedAuthorizationFilter : IAsyncActionFilter
{
    private readonly UserService _userService;

    public PasswordChangedAuthorizationFilter(UserService userService)
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
        if (user == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 獲取 JWT Token 的發行日期 (Issued Date)
        var issuedDateClaim = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat);
        if (issuedDateClaim != null && long.TryParse(issuedDateClaim.Value, out long issuedDateUnix))
        {
            var issuedDate = DateTimeOffset.FromUnixTimeSeconds(issuedDateUnix).UtcDateTime;
            // 使用 issuedDate 進行需要的處理
            if (user.LastChangePasswordTime == null)
            {
                return;
            }
            if (user.LastChangePasswordTime < issuedDate)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        await next();
    }
}
