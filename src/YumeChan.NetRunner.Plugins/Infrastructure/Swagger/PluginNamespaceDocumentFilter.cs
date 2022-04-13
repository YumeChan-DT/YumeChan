using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Swagger;

/// <summary>
/// Filters the Swagger document by removing Endpoints not defined within the same namespace as the document's name.
/// </summary>
public class PluginNamespaceDocumentFilter : IDocumentFilter
{
	/// <summary>
	/// Filters any endpoints that are not within the same namespace as the document's name.
	/// </summary>
	/// <param name="swaggerDoc">The Swagger document.</param>
	/// <param name="context">The current operation filter context.</param>
	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
	{
		foreach (KeyValuePair<string, OpenApiPathItem> route in swaggerDoc.Paths.Where(p => !p.Key.StartsWith($"/api/{swaggerDoc.Info.Title}/")))
		{
			swaggerDoc.Paths.Remove(route.Key);
		}
	}
}