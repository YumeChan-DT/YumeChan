using System;
using System.Collections.Generic;
using System.Text;

namespace Nodsoft.YumeChan.Modules
{
	public static class ModulesIndex
	{
		public static Version CoreVersion { get; set; }
		public static Version ModulesVersion { get; } = typeof(ModulesIndex).Assembly.GetName().Version;

		public static string MissingVersionSubstitute { get; } = "Unknown";
	}
}
