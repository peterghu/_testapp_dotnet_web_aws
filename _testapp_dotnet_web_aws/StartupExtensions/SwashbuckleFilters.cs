using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace _testapp_dotnet_web_aws.StartupExtensions
{
    /// <summary>
    ///
    /// Source: https://dev.to/htissink/versioning-asp-net-core-apis-with-swashbuckle-making-space-potatoes-v-x-x-x-3po7
    ///
    /// </summary>
    public class RemoveVersionFromParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters.SingleOrDefault(p => p.Name == "version" || p.Name == "api-version");

            if (versionParameter != null)
            {
                operation.Parameters.Remove(versionParameter);
            }
        }
    }

    /// <summary>
    ///
    /// /api/v{version}/{API} to /api/v1/{API}
    ///
    /// </summary>
    public class ReplaceVersionWithExactValueInPath : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var toReplaceWith = new OpenApiPaths();

            foreach (var (key, value) in swaggerDoc.Paths)
            {
                toReplaceWith.Add(key.Replace("v{version}", "v" + swaggerDoc.Info.Version, StringComparison.InvariantCulture), value);
            }

            swaggerDoc.Paths = toReplaceWith;
        }
    }

    /// <summary>
    ///
    /// https://dejanstojanovic.net/aspnet/2020/june/dealing-with-default-api-versions-in-swagger-ui/
    /// Remove default version route
    ///
    /// </summary>
    public class RemoveDefaultApiVersionRoute : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var apiDescription in context.ApiDescriptions)
            {
                var versionParam = apiDescription.ParameterDescriptions
                     .FirstOrDefault(p => p.Name == "api-version" &&
                     p.Source.Id.Equals("Query", StringComparison.InvariantCultureIgnoreCase));

                if (versionParam == null)
                    continue;

                var route = "/" + apiDescription.RelativePath.TrimEnd('/');
                swaggerDoc.Paths.Remove(route);
            }
        }
    }
}