using System.Collections;
using Castle.DynamicProxy;
using YumeChan.Core.Services.Config;

#nullable enable
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

				/*
				 * Use an intercepting proxy to handle setting elements in Lists, Dictionaries, and all associated interfaces
				 */
				
				// Lists
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
				{
					IList? list = _config.GetValue(propertyName, type) as IList;
						
					// Instantiate a new DynamicJsonList to handle the list (use Activator.CreateInstance to avoid the need for a strongly-typed constructor)
					invocation.ReturnValue = Activator.CreateInstance(typeof(DynamicJsonList<>).MakeGenericType(type.GetGenericArguments()[0]),
						_config, propertyName, list);
				}
				// Dictionaries
				else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
				{
					IDictionary? dictionary = _config.GetValue(propertyName, type) as IDictionary;
					
					// Instantiate a new DynamicJsonDictionary to handle the list (use Activator.CreateInstance to avoid the need for a strongly-typed constructor)
					invocation.ReturnValue = Activator.CreateInstance(typeof(DynamicJsonDictionary<,>).MakeGenericType(type.GetGenericArguments()), 
						_config, propertyName, dictionary);
				}
				// Non-list, and non-dictionary. It will either be a nested interface or a primitive type.
				else
				{
					// Assign values and Instantiate new proxies for nested interface properties
					invocation.ReturnValue = type.IsInterface 
						? InterfaceWritableConfigWrapper.CreateInstance(_config.GetSectionInternal(propertyName), type) 
						: _config.GetValue(propertyName, type);
				}
			}
		}
		// If the property is not found, it may be an enumerator. 
		catch (InvalidOperationException) when (property is nameof(IEnumerable.GetEnumerator))
		{
			// in this case, this is fine.
		}
	}
}