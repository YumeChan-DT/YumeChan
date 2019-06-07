using System;

namespace Nodsoft.YumeChan.NetRunner.Data
{
	public static class DebugPage
	{
		public static string DebugEnvVar { get; } = "WhereIsThatCake";
		public static string DebugEnvValue { get => ReadDebug(); }

		public static string ReadDebug()
		{
			return Environment.GetEnvironmentVariable(DebugEnvVar);
		}
	}
}
