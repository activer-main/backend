using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ActiverWebAPI.Dev.Swagger;

public class Filter
{
    // 非可為空 string 屬性架構過濾器
    public class NonNullStringPropertiesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Type == "object" && schema.Properties != null)
            {
                foreach (var property in schema.Properties)
                {
                    if (property.Value.Type == "string" && !property.Value.Nullable)
                    {
                        property.Value.Nullable = true;
                    }
                }
            }
        }
    }
}
