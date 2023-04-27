using ActiverWebAPI.Exceptions;
using Newtonsoft.Json;
using System.Net;

namespace ActiverWebAPI.Services.Middlewares;

public class ErrorHandlingMiddleware : IMiddleware
{
    public ErrorHandlingMiddleware()
    {
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError; // 預設狀態碼

        if (exception is UnauthorizedAccessException)
            code = HttpStatusCode.Unauthorized; // 401

        else if (exception is NotFoundException)
            code = HttpStatusCode.NotFound; // 404

        else if (exception is UserNotFoundException)
            code = HttpStatusCode.NotFound; // 404

        else if (exception is BadRequestException)
            code = HttpStatusCode.BadRequest; // 400

        else if (exception is BadHttpRequestException)
            code = HttpStatusCode.BadRequest;

        // 根據需要添加更多狀態碼的處理邏輯
        var result = JsonConvert.SerializeObject(new { statusCode = code, message = exception.Message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int) code;
        return context.Response.WriteAsync(result);
    }
}
