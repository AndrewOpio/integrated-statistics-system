using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VMSService.Web.Helpers;
public class ValidateAntiForgeryTokenFromHeaderAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext ctx)
    {
        var af = ctx.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        // IsRequestValidAsync checks both the cookie and the request token.
        // For JSON POSTs the token arrives in the RequestVerificationToken header,
        // which ASP.NET Core's antiforgery system reads automatically.
        if (!af.IsRequestValidAsync(ctx.HttpContext).GetAwaiter().GetResult())
            ctx.Result = new BadRequestObjectResult(new { errors = new[] { "Invalid request token." } });
    }
}
