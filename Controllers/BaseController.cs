using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActiverWebAPI.Controllers;

[Authorize]
public class BaseController : Controller
{
    public Guid UserId
    {
        get
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }

            throw new ApplicationException("Invalid user ID.");
        }
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["UserId"] = UserId;
        base.OnActionExecuting(context);
    }
}
