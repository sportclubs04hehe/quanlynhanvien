using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace api.Filters
{
    public class CustomUnauthorizedFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is UnauthorizedResult)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = "Email hoặc mật khẩu không đúng"
                })
                {
                    StatusCode = 401
                };
            }
        }
    }
}
