using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ScanService.Api.Infrastructure
{
    /// <summary>
    /// Describes the multipart body of the scan endpoints to Swagger.
    /// </summary>
    /// <remarks>
    /// Those actions take no parameters - they read the multipart body themselves so that ASP.NET
    /// Core never spools the upload to a temp file - and Swashbuckle builds its request body from
    /// parameters, so without this the UI would offer no file picker and Execute would post an
    /// empty body. The shape declared here has to stay in step with
    /// <see cref="MultipartUploadReader"/> and the field names the controller reads.
    /// </remarks>
    public sealed class MultipartUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var disablesFormBinding = context.MethodInfo
                .GetCustomAttributes(typeof(DisableFormValueModelBindingAttribute), inherit: true)
                .Length > 0;

            if (!disablesFormBinding)
            {
                return;
            }

            var properties = new Dictionary<string, OpenApiSchema>
            {
                ["file"] = new()
                {
                    Type = "string",
                    Format = "binary",
                    Description = "The file to scan."
                }
            };

            var required = new HashSet<string> { "file" };

            // Only the store endpoint reads a destination off the form.
            if (context.ApiDescription.RelativePath?.EndsWith("store", StringComparison.OrdinalIgnoreCase) == true)
            {
                properties["destinationDirectory"] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "Absolute directory to write to. Must sit under an allowed root.",
                    Example = new OpenApiString(@"C:\CASFiles\uploads\scantest")
                };

                properties["destinationFileName"] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "Optional. Defaults to \"{guid}_{originalName}\"."
                };

                properties["overwrite"] = new OpenApiSchema
                {
                    Type = "boolean",
                    Default = new OpenApiBoolean(false),
                    Description = "Whether an existing file at that path may be replaced."
                };

                required.Add("destinationDirectory");
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new()
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = required
                        }
                    }
                }
            };
        }
    }
}
