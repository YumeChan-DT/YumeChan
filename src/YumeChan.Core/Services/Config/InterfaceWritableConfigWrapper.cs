using System;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;
using YumeChan.Core.Infrastructure.Dynamic.Config;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace YumeChan.Core.Services.Config;

/// <summary>
/// Provides a wrapper for a configuration interface, based on <see cref="JsonWritableConfig"/>, using Castle Dynamic Proxy.
/// </summary>
/// <typeparam name="TConfig">Type of config interface to be wrapped</typeparam>
internal sealed class InterfaceWritableConfigWrapper<TConfig>
	where TConfig : class
{
	/// <summary>
	/// Represents the instantiated config interface.
	/// </summary>
	public TConfig Instance { get; private set; }
	
	private readonly JsonWritableConfig _config;
	
	public InterfaceWritableConfigWrapper(JsonWritableConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Creates a new instance of the config interface.
	/// </summary>
	/// <returns>New instance of the config interface.</returns>
	public TConfig CreateInstance() => Instance = InterfaceWritableConfigWrapper.CreateInstance(_config, typeof(TConfig)) as TConfig;


}

internal static class InterfaceWritableConfigWrapper
{
	internal static object CreateInstance(JsonWritableConfig config, Type configType) 
		=> new ProxyGenerator().CreateInterfaceProxyWithoutTarget(configType, new JsonConfigInterceptor(config));
}

