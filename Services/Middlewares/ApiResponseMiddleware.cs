using ActiverWebAPI.Exceptions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

            // 找出 content type
            string contentType = context.Response.ContentType ?? "";
            bool isFileResponse = !contentType.StartsWith("application/problem+json") && !contentType.StartsWith("application/json") && contentType.StartsWith("application/") || contentType.StartsWith("image/") || contentType.StartsWith("audio/") || contentType.StartsWith("video/");

            // 如果不是 檔案 才進行 parse
            if (!isFileResponse)
            {
                // 在回應發送之後進行統一格式化處理
                await FormatResponseAsync(context, memStream);
            }

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

        context.Response.ContentType = "application/json";

        // 清空原本的資料
        memoryStream.SetLength(0);

        // 寫入內容
        await memoryStream.WriteAsync(Encoding.UTF8.GetBytes(parsedBody));
    }

    private static string ParseApiResponse(string originalBody, int statusCode, string? message)
    {

        JToken? jsonToken;
        try
        {
            jsonToken = JToken.Parse(originalBody);
        }
        catch (JsonReaderException)
        {
            jsonToken = null;
        }

        var isSuccess = statusCode >= 200 && statusCode < 300;

        // 不是 json 物件
        if (jsonToken == null)
        {
            var rawBody = originalBody.IsNullOrEmpty() ? null : originalBody;

            var responseRawObject = new
            {
                Success = isSuccess,
                StatusCode = statusCode,
                Message = message ?? GetDefaultMessage(statusCode),
                Data = isSuccess ? rawBody : null,
                Error = !isSuccess ? rawBody : null
            };

            return JsonConvert.SerializeObject(responseRawObject);
        }

        // 是 json 物件
        if (jsonToken.Type == JTokenType.Object)
        {
            // 轉換成 jsonObject
            JObject jsonObject = (JObject)jsonToken;

            // Bad request
            if (statusCode == (int)HttpStatusCode.BadRequest)
            {
                jsonObject["Exception"] = jsonObject["InnerException"];

                if (jsonObject["errors"] != null && jsonObject["errors"] is JObject fieldObject && jsonObject["title"] != null && jsonObject["title"].ToString() == "One or more validation errors occurred.")
                {
                    var errorFields = string.Join(", ", fieldObject.Properties().Select(p => $"欄位 {p.Name} 不能為空"));
                    
                    jsonObject["Exception"] = errorFields;
                }

                // 刪除不必要的屬性
                jsonObject.Remove("Message");
                jsonObject.Remove("errors");
                jsonObject.Remove("InnerException");
                jsonObject.Remove("type");
                jsonObject.Remove("title");
                jsonObject.Remove("status");
                jsonObject.Remove("traceId");
            }

            // 寫回 JsonToken
            jsonToken = jsonObject;
        }

        var responseObject = new
        {
            Success = isSuccess,
            StatusCode = statusCode,
            Message = message ?? GetDefaultMessage(statusCode),
            Data = isSuccess ? jsonToken : null,
            Error = !isSuccess ? jsonToken : null
        };

        return JsonConvert.SerializeObject(responseObject);
    }

    private static string GetDefaultMessage(int statusCode)
    {
        // 根據狀態碼返回預設訊息
        return statusCode switch
        {
            200 => "OK",
            201 => "資源已創建",
            202 => "請求已接受",
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
        var excpetion = exception.Message;

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

        else if (exception is SecurityTokenValidationException)
        {
            excpetion = "JWT token 驗證失敗";
        }

        var jsonObject = new { Message = message, InnerException = excpetion };
        var result = JsonConvert.SerializeObject(jsonObject);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return memoryStream.WriteAsync(Encoding.UTF8.GetBytes(result));
    }
}
