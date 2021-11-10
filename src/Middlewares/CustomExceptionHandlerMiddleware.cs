using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Cloudware.Utilities.Common.Exceptions;
using Cloudware.Utilities.Common.Response;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cloudware.Utilities.HttpExceptionHandler.Middlewares
{
    public static class CustomExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
        }
    }

    public class CustomExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;
        private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;

        public CustomExceptionHandlerMiddleware(RequestDelegate next,
            IHostingEnvironment env,
            ILogger<CustomExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            string message = "Error Occurred!!!";
            object messageObject = "Error Occurred!!!";
            HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;
            long ClwStatusCode = 500;
            string requestId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
            try
            {
                await _next(context);
            }
            catch (AppException exception)
            {
                _logger.LogError(exception, exception.Message);
                httpStatusCode = exception.HttpStatusCode;
                ClwStatusCode = exception.ClwStatusCode;

                if (_env.IsDevelopment())
                {
                    var dic = new Dictionary<string, object>
                    {
                        ["Exception"] = exception,
                        ["StackTrace"] = exception.StackTrace,
                    };
                    // if (exception.InnerException != null)
                    // {
                    //     dic.Add("InnerException.Exception", exception.InnerException.Message);
                    //     dic.Add("InnerException.StackTrace", exception.InnerException.StackTrace);
                    // }
                    if (exception.AdditionalData != null)
                        dic.Add("AdditionalData", JsonConvert.SerializeObject(exception.AdditionalData));

                    messageObject = dic;
                }
                message = exception.Message;


                // message = exception.Message;
                await WriteToResponseAsync();
            }
            catch (SecurityTokenExpiredException exception)
            {
                _logger.LogError(exception, exception.Message);
                SetUnAuthorizeResponse(exception);
                await WriteToResponseAsync();
            }
            catch (UnauthorizedAccessException exception)
            {
                _logger.LogError(exception, exception.Message);
                SetUnAuthorizeResponse(exception);
                await WriteToResponseAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                if (exception.InnerException is not AppException appException)
                {
                    if (_env.IsDevelopment())
                    {
                        var dic = new Dictionary<string, object>
                        {
                            ["Exception"] = exception,
                            ["StackTrace"] = exception.StackTrace,
                        };
                        messageObject = dic;
                    }
                    message = exception.Message;
                }
                else
                {
                    if (_env.IsDevelopment())
                    {
                        var dic = new Dictionary<string, object>
                        {
                            ["Exception"] = appException,
                            ["StackTrace"] = appException.StackTrace,
                        };
                        ClwStatusCode = appException.ClwStatusCode;
                        httpStatusCode = appException.HttpStatusCode;
                        messageObject = dic;
                    }
                      ClwStatusCode = appException.ClwStatusCode;
                    httpStatusCode = appException.HttpStatusCode;
                    message = appException.Message;
                }
                await WriteToResponseAsync();
            }

            async Task WriteToResponseAsync()
            {
                if (_env.IsDevelopment())
                    await WriteToResponseAsyncDev();
                else
                    await WriteToResponseAsyncProd();
            }

            async Task WriteToResponseAsyncDev()
            {
                if (context.Response.HasStarted)
                    throw new InvalidOperationException("The response has already started, the http status code middleware will not be executed.");

                var result = new ApiResult<object>((int)httpStatusCode, ClwStatusCode, requestId, messageObject, message);
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                context.Response.StatusCode = (int)httpStatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }
            async Task WriteToResponseAsyncProd()
            {
                if (context.Response.HasStarted)
                    throw new InvalidOperationException("The response has already started, the http status code middleware will not be executed.");

                var result = new ApiResult((int)httpStatusCode, ClwStatusCode, requestId, message);
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                context.Response.StatusCode = (int)httpStatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }

            void SetUnAuthorizeResponse(Exception exception)
            {
                httpStatusCode = HttpStatusCode.Unauthorized;
                //unauthorized error code
                ClwStatusCode = 500;

                if (_env.IsDevelopment())
                {
                    var dic = new Dictionary<string, string>
                    {
                        ["Exception"] = exception.Message,
                        ["StackTrace"] = exception.StackTrace
                    };
                    if (exception is SecurityTokenExpiredException tokenException)
                        dic.Add("Expires", tokenException.Expires.ToString());

                    message = JsonConvert.SerializeObject(dic, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                }
            }
        }
    }
}
