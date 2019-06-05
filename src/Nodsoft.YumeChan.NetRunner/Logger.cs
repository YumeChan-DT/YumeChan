using System.Threading.Tasks;
using Discord;
using Nodsoft.YumeChan.Core;

namespace Nodsoft.YumeChan.NetRunner
{
	public class Logger : ILogger
	{
		public Task Log(LogMessage msg)
		{
			System.Console.WriteLine(msg.Message);
			return Task.CompletedTask;
		}
	}
}