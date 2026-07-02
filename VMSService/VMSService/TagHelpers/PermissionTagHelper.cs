using Microsoft.AspNetCore.Razor.TagHelpers;
using VMSService.Web.Services;

namespace VMSService.Web.TagHelpers
{
    [HtmlTargetElement("permission", Attributes = "name")]
    public class PermissionTagHelper(IHttpContextAccessor httpContextAccessor, IPermissionService permissionService)
        : TagHelper
    {
        [HtmlAttributeName("name")]
        public string Name { get; set; } = string.Empty;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true ||
                !await permissionService.HasPermissionAsync(user, Name))
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = null;
            _ = await output.GetChildContentAsync();
        }
    }
}
