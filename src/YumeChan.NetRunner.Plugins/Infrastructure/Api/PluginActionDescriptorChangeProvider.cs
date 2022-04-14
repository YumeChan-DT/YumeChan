using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Api;

public class PluginActionDescriptorChangeProvider : IActionDescriptorChangeProvider
{
	public PluginActionDescriptorChangeProvider()
	{
		TokenSource = new();
	}
	
	public static PluginActionDescriptorChangeProvider Instance { get; } = new();
	
	public CancellationTokenSource TokenSource { get; private set; }

	public bool HasChanged { get; set; }

	public IChangeToken GetChangeToken()
	{
		TokenSource = new();
		return new CancellationChangeToken(TokenSource.Token);
	}
}