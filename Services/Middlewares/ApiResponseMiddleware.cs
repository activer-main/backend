using ActiverWebAPI.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ActiverWebAPI.Services.Middlewares;

public class ApiResponseMiddleware : IMiddleware
{
    public ApiResponseMiddleware()
    {
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var originalBody = context.Response.Body;

        try
        {
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            try
            {
                await next(context);
            } catch (Exception ex)
            {
                await HandleException(context, memStream, ex);
            }

            // 在回應發送之後進行統一格式化處理
            await FormatResponseAsync(context, memStream);

            memStream.Position = 0;

            // 將新的內容寫入原始的回應主體
            await memStream.CopyToAsync(originalBody);

        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    public async Task FormatResponseAsync(HttpContext context, MemoryStream memoryStream)
    {
        // 從 HttpContext.Items 中取得訊息
        var message = context.Items["Message"]?.ToString();

        // 將記憶體串流設定為回應的主體
        memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);

        var parsedBody = ParseApiResponse(responseBody, context.Response.StatusCode, message);

        // 清空原本的資料
        memoryStream.SetLength(0);

        // 寫入內容
        await memoryStream.WriteAsync(Encoding.UTF8.GetBytes(parsedBody));
    }

    private string ParseApiResponse(string originalBody, int statusCode, string? message)
    {
        // 在這裡根據你的需求，對原始回應內容進行解析和轉換成相同格式的邏輯
        var jsonObject = JsonConvert.DeserializeObject<JObject>(originalBody);

        // 分開處理回傳型態
        if (statusCode == (int) HttpStatusCode.BadRequest)
        {
            jsonObject["Message"] = jsonObject["title"];
            jsonObject["Errors"] = jsonObject["errors"];

            // 刪除不必要的屬性
            jsonObject.Remove("errors");
            jsonObject.Remove("type");
            jsonObject.Remove("title");
            jsonObject.Remove("status");
            jsonObject.Remove("traceId");
        }

        var isSuccess = statusCode >= 200 && statusCode < 300;

        // 假設你要將回應轉換成一個包含成功或錯誤訊息的 JSON 物件
        var responseObject = new
        {
            Success = isSuccess,
            StatusCode = statusCode,
            Message = message ?? GetDefaultMessage(statusCode),
            Data = isSuccess ? jsonObject : null, // 或是解析後的資料
            Error = !isSuccess ? jsonObject : null
        };
            
        return JsonConvert.SerializeObject(responseObject);
    }

    private string GetDefaultMessage(int statusCode)
    {
        // 根據狀態碼返回預設訊息
        return statusCode switch
        {
            200 => "OK",
            201 => "請求已接受",
            400 => "請求無效",
            401 => "驗證錯誤",
            404 => "已被刪除、移動或從未存在",
            _ => "發生未知的錯誤",
        };
    }

    private static ValueTask HandleException(HttpContext context, MemoryStream memoryStream, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError; // 預設狀態碼
        var message = "伺服器發生內部錯誤";

        if (exception is UnauthorizedException)
        {
            message = "驗證錯誤";
            code = HttpStatusCode.Unauthorized; // 401
        }
           
        else if (exception is NotFoundException)
        {
            message = "已被刪除、移動或從未存在";
            code = HttpStatusCode.NotFound; // 404
        }

        else if (exception is UserNotFoundException)
        {
            message = "使用者不存在";
            code = HttpStatusCode.NotFound; // 404
        }

        else if (exception is BadRequestException || exception is BadHttpRequestException)
        {
            message = "請求無效";
            code = HttpStatusCode.BadRequest; // 400
        }

        var jsonObject = new { Message = message, InnerException = exception.Message };
        var result = JsonConvert.SerializeObject(jsonObject);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return memoryStream.WriteAsync(Encoding.UTF8.GetBytes(result));
    }
}
