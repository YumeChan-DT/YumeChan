using System;
using System.Collections;
using System.Collections.Generic;
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

		try
		{
			// Split property name into tuple of "get_"/"set_" prefix and property name
			(bool isSetter, string propertyName) = property[..4] switch
			{
				"get_" => (false, property[4..]),
				"set_" => (true, property[4..]),
				_      => throw new InvalidOperationException($"Property {property} is not a valid getter or setter.")
			};
		
			if (invocation.Method.IsSpecialName && isSetter)
			{
				// Setter
				object value = invocation.Arguments[0];
				_config.SetValue(propertyName, value, invocation.Method.GetParameters()[0].ParameterType);
			}
			else
			{
				// Getter
				Type type = invocation.Method.ReturnType;
			
				// Assign values and Instantiate new proxies for nested interface properties
				invocation.ReturnValue = type.IsInterface 
					? InterfaceWritableConfigWrapper.CreateInstance(_config.GetSectionInternal(propertyName), type) 
					: _config.GetValue(propertyName, type);
			}
		}
		catch (InvalidOperationException e)
		{
			// If the property id not found, it may be an enumerator, so try to get the value as an enumerator.
			if (property == nameof(IEnumerable.GetEnumerator))
			{
				
			}
		}
	}
}