using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Api;

/// <summary>
/// Provides a routing convention for API controllers found in YumeChan plugins.
/// </summary>
public class PluginApiRoutingConvention : IControllerModelConvention
{
	/// <summary>
	/// Sets the route template for the API controller, based on plugin name and controller name.
	/// Example: <c>/api/{pluginName}/{controllerName}</c>
	/// </summary>
	public void Apply(ControllerModel controller)
	{
		string? assemblyName = controller.ControllerType.Assembly.GetName().Name;
		
		// Bypass the convention if :
		//   - for some reason the assembly name is null.
		//   - the controller is from the YumeChan.NetRunner or YumeChan.Core assemblies (or derivative namespaces).
		//   - the controller does not have the ApiController attribute.
		if (assemblyName is null 
			|| assemblyName.StartsWith("YumeChan.NetRunner") 
			|| assemblyName.StartsWith("YumeChan.Core")
			|| !controller.ControllerType.IsDefined(typeof(ApiControllerAttribute)))
		{
			return;
		}

		// Get the plugin name from the controller assembly name.
		// HACK: This is an unorthodox way of getting the plugin's InternalName, as it assumes the assembly name is the same as the plugin's InternalName.
		string? pluginName = controller.ControllerType.Assembly.GetName().Name;
		
		// Set the route templates.
		controller.Selectors[0].AttributeRouteModel = new()
		{
			Template = $"/api/{pluginName}/[controller]"
		};
	}
}