using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SaasApi.API.OpenApi;

/// <summary>
/// Substitutes the {version} route segment with a literal "v1" in every path
/// and drops the corresponding path parameter from each operation, so Scalar
/// doesn't prompt for it on every request.
/// </summary>
internal sealed class ApiVersionPathTransformer : IOpenApiDocumentTransformer
{
    private const string DefaultVersion = "1";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var rewritten = new OpenApiPaths();

        foreach (var (path, item) in document.Paths)
        {
            var newPath = path.Replace("{version}", DefaultVersion);

            foreach (var operation in item.Operations.Values)
            {
                var versionParam = operation.Parameters
                    .FirstOrDefault(p => p.Name == "version" && p.In == ParameterLocation.Path);
                if (versionParam is not null)
                    operation.Parameters.Remove(versionParam);
            }

            rewritten.Add(newPath, item);
        }

        document.Paths = rewritten;
        return Task.CompletedTask;
    }
}
