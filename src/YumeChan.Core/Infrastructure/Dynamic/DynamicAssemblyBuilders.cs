using System;
using System.Reflection;
using System.Reflection.Emit;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;

namespace YumeChan.Core.Infrastructure.Dynamic;

/// <summary>
/// Provides a builder class to create dynamic assemblies.
/// This will be used to create the underlying dynamic assemblies for the dynamic types.
/// </summary>
public static class DynamicAssemblyBuilders
{
	/// <summary>
	/// Namespace used by the dynamic assemblies and their types.
	/// </summary>
	public const string DynamicNamespace = "YumeChan.Core.Infrastructure.Dynamic";
	
	public static AssemblyBuilder AssemblyBuilder { get; }
	public static ModuleBuilder ModuleBuilder { get; }
	public static ModuleScope ModuleScope { get; }
	
	static DynamicAssemblyBuilders()
	{
		AssemblyName assemblyName = new(DynamicNamespace);
		AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
		ModuleBuilder = AssemblyBuilder.DefineDynamicModule(assemblyName.Name!);
		ModuleScope = new(false);
	}
}