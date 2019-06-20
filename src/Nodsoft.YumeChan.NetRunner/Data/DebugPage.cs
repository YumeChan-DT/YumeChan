using System;

namespace Nodsoft.YumeChan.NetRunner.Data
{
	public static class DebugPage
	{
		public static string DebugEnvVar { get; } = "WhereIsThatCake";
		public static string DebugEnvValue
		{
			get => Environment.GetEnvironmentVariable(DebugEnvVar);
		}
	}
}
