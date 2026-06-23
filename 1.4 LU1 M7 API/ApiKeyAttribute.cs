using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NACGames
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string HEADER_NAME = "X-API-KEY";
        private const string SECRET_KEY = "BredaTrashModelSecret2026!"; // Change this to your preferred secret token

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Check if the X-API-KEY header exists in the incoming request
            if (!context.HttpContext.Request.Headers.TryGetValue(HEADER_NAME, out var extractedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key was not provided in the 'X-API-KEY' header."
                };
                return;
            }

            // 2. Validate the extracted key against our secret string
            if (!SECRET_KEY.Equals(extractedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "Unauthorized client. Invalid API Key."
                };
                return;
            }

            // 3. If everything is valid, continue to the Controller endpoint method
            await next();
        }
    }
}
