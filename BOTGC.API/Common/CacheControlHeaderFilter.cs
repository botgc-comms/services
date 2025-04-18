using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BOTGC.API.Common
{

    public class CacheControlHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Cache-Control",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Set to 'no-cache' to force a fresh response instead of a cached one.",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}