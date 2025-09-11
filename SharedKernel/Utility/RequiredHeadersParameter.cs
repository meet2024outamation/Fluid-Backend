using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SharedKernel.Utility
{
    public class AddRequiredHeadersParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Tenant-Id",
                In = ParameterLocation.Header,
                Required = false
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Platform-Id",
                In = ParameterLocation.Header,
                Required = true
            });
        }
    }
}
