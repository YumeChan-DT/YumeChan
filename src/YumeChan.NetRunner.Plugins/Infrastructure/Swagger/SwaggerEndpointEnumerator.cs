using System.Collections;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Swagger;

public class SwaggerEndpointEnumerator : IEnumerable<UrlDescriptor>
{
	public List<UrlDescriptor> Endpoints { get; } = new();
	
	public IEnumerator<UrlDescriptor> GetEnumerator() => (Endpoints as IEnumerable<UrlDescriptor>).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}