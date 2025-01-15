using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Helpers
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var modelParameter = context.MethodInfo.GetParameters()
                .FirstOrDefault(p => p.ParameterType.Namespace?.Contains("ViewModels") == true);

            var fileParam = context.MethodInfo.GetParameters()
                .FirstOrDefault(p => p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile));

            if (modelParameter == null && fileParam == null)
            {
                return;
            }

            if (operation.RequestBody == null)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>()
                };
            }

            if (!operation.RequestBody.Content.ContainsKey("multipart/form-data"))
            {
                operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>()
                    }
                };
            }

            var multipartSchema = operation.RequestBody.Content["multipart/form-data"].Schema;

            if (modelParameter != null)
            {
                var modelType = modelParameter.ParameterType;
                var schema = context.SchemaGenerator.GenerateSchema(modelType, context.SchemaRepository);

                foreach (var property in schema.Properties)
                {
                    if (!multipartSchema.Properties.ContainsKey(property.Key))
                    {
                        multipartSchema.Properties[property.Key] = property.Value;
                    }
                }
            }

            if (fileParam != null && !multipartSchema.Properties.ContainsKey("file"))
            {
                multipartSchema.Properties.Add("file", new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "Optional file to upload",
                    Nullable = true
                });
            }

            operation.RequestBody.Content["multipart/form-data"].Schema = multipartSchema;
        }
    }
}
