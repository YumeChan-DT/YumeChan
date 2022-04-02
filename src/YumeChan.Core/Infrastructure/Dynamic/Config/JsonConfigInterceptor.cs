using System;
using System.Reflection;
using Castle.DynamicProxy;
using YumeChan.Core.Services.Config;

namespace YumeChan.Core.Infrastructure.Dynamic.Config;

/// <summary>
/// Simple interceptor for dynamic configuration properties from interface-based configuration sources.
/// </summary>
internal sealed class JsonConfigInterceptor : IInterceptor
{
	private readonly JsonWritableConfig _config;

	public JsonConfigInterceptor(JsonWritableConfig config)
	{
		_config = config;
	}
	
	/// <summary>
	/// Handle interception for both get and set operations on interface properties.
	/// </summary>
	public void Intercept(IInvocation invocation)
	{
		string property = invocation.Method.Name;
		Type type = invocation.Method.ReturnType;

		// Split property name into tuple of "get_"/"set_" prefix and property name
		(bool isSetter, string propertyName) = property[..4] switch
		{
			"get_" => (false, property[4..]),
			"set_" => (true, property[4..]),
			_ => throw new InvalidOperationException($"Property {property} is not a valid getter or setter.")
		};
		
		if (invocation.Method.IsSpecialName && isSetter)
		{
			// Setter
			object value = invocation.Arguments[0];
			_config.SetValue(propertyName, value, type);
		}
		else
		{
			// Getter
			// invocation.ReturnValue = _config.GetValue(propertyName, type);
			
			// Assign values and Instantiate new proxies for nested interface properties
			invocation.ReturnValue = type.IsInterface 
				? InterfaceWritableConfigWrapper.CreateInstance(_config.GetValue(propertyName) as JsonWritableConfig, type) 
				: _config.GetValue(propertyName, type);
		}
	}
}