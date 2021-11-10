using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloudware.Utilities.Common.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace Cloudware.Utilities.HttpExceptionHandler.Filter
{
   public  class ApiResultFilterAttribute:ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            //if (context.Result is OkObjectResult okObjectResult)
            //{
            //    var apiResult = new ApiResult<object>(200, 200,context.HttpContext.TraceIdentifier, okObjectResult.Value);
            //    context.Result = new JsonResult(apiResult) { StatusCode = okObjectResult.StatusCode };
            //}
            //else if (context.Result is OkResult okResult)
            //{
            //    var apiResult = new ApiResult(200, 200, context.HttpContext.TraceIdentifier);
            //    context.Result = new JsonResult(apiResult) { StatusCode = okResult.StatusCode };
            //}
           // else if (context.Result is BadRequestResult badRequestResult)
             if (context.Result is BadRequestResult badRequestResult)
            {
                var apiResult = new ApiResult(400, 400, context.HttpContext.TraceIdentifier);
                context.Result = new JsonResult(apiResult) { StatusCode = badRequestResult.StatusCode };
            }
            else if (context.Result is BadRequestObjectResult badRequestObjectResult)
            {
                var message = badRequestObjectResult.Value.ToString();
                if (badRequestObjectResult.Value is SerializableError errors)
                {
                    var errorMessages = errors.SelectMany(p => (string[])p.Value).Distinct();
                    message = string.Join(" | ", errorMessages);
                }
                var apiResult = new ApiResult(400, 400, context.HttpContext.TraceIdentifier, message);
                context.Result = new JsonResult(apiResult) { StatusCode = badRequestObjectResult.StatusCode };
            }
            //else if (context.Result is ContentResult contentResult)
            //{
            //    var apiResult = new ApiResult(200, 200, context.HttpContext.TraceIdentifier, contentResult.Content);
            //    context.Result = new JsonResult(apiResult) { StatusCode = contentResult.StatusCode };
            //}
            else if (context.Result is NotFoundResult notFoundResult)
            {
                var apiResult = new ApiResult(404,404, context.HttpContext.TraceIdentifier);
                context.Result = new JsonResult(apiResult) { StatusCode = notFoundResult.StatusCode };
            }
            else if (context.Result is NotFoundObjectResult notFoundObjectResult)
            {
                var apiResult = new ApiResult<object>(404, 404, context.HttpContext.TraceIdentifier, notFoundObjectResult.Value);
                context.Result = new JsonResult(apiResult) { StatusCode = notFoundObjectResult.StatusCode };
            }
            //else if (context.Result is ObjectResult objectResult && objectResult.StatusCode == null
            //    && !(objectResult.Value is ApiResult))
            //{
            //    var apiResult = new ApiResult<object>(200, 200, context.HttpContext.TraceIdentifier, objectResult.Value);
            //    context.Result = new JsonResult(apiResult) { StatusCode = objectResult.StatusCode };
            //}

            base.OnResultExecuting(context);
        }
    }
}
