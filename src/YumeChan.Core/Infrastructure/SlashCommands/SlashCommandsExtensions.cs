﻿using DSharpPlus.SlashCommands;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace YumeChan.Core.Infrastructure.SlashCommands;

public static class SlashCommandsExtensions
{
	public static void RegisterCommands(this SlashCommandsExtension slashCommands, Assembly assembly, ulong? guildId = null)
	{
		Type[] types = assembly.ExportedTypes
			.Where(xt =>
			{
				TypeInfo typeInfo = xt.GetTypeInfo();
				return typeInfo.IsModuleCandidateType() && !typeInfo.IsNested;
			})
			.ToArray();

		if (types is not [])
		{
			foreach (Type item in types)
			{
				slashCommands.RegisterCommands(item, guildId);
			}
		}
	}


	private static bool IsModuleCandidateType(this TypeInfo ti)
	{
		// check if compiler-generated
		if (ti.GetCustomAttribute<CompilerGeneratedAttribute>(false) is not null)
		{
			return false;
		}

		// check if derives from the required base class
		Type tmodule = typeof(ApplicationCommandModule);
		TypeInfo timodule = tmodule.GetTypeInfo();

		if (!timodule.IsAssignableFrom(ti) || (ti.IsGenericType && ti.Name.Contains("AnonymousType")
				&& (ti.Name.StartsWith("<>") || ti.Name.StartsWith("VB$"))
				&& (ti.Attributes & TypeAttributes.NotPublic) is TypeAttributes.NotPublic) || !ti.IsClass || ti.IsAbstract)
		{
			return false;
		}

		// check if delegate type
		TypeInfo tdelegate = typeof(Delegate).GetTypeInfo();
		if (tdelegate.IsAssignableFrom(ti))
		{
			return false;
		}

		// qualifies if any method or type qualifies
		return ti.DeclaredMethods.Any(xmi => xmi.IsCommandCandidate(out _)) || ti.DeclaredNestedTypes.Any(xti => xti.IsModuleCandidateType());
	}

	private static bool IsCommandCandidate(this MethodInfo method, out ParameterInfo[] parameters)
	{
		parameters = null;

		if (method.IsStatic || method.IsAbstract || method.IsConstructor || method.IsSpecialName)
		{
			return false;
		}

		// check if appropriate return and arguments
		parameters = method.GetParameters();
		return parameters is not [] && !parameters.First().ParameterType.IsAssignableFrom(typeof(BaseContext)) && method.ReturnType == typeof(Task);
	}
}