using Discord;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core
{
	public interface ILogger
	{
		Task Log(LogMessage msg);
	}
}