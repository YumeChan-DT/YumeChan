using Discord;
using System;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.ConsoleRunner
{
	public class Logger : Core.ILogger
	{
		public Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}
}