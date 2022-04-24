using System.Collections;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Swagger;

public class SwaggerDocumentEnumerator
{
	public IDictionary<string,OpenApiInfo> Documents { get; set; } = new Dictionary<string, OpenApiInfo>();
}