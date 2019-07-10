using System;
using static Nodsoft.YumeChan.Core.YumeCore;

namespace Nodsoft.YumeChan.ConsoleRunner
{
	static class Program
	{
		static void Main(string[] _)
		{
			Instance.Logger = new Logger();
			Instance.RunBot();
		}
	}
}
