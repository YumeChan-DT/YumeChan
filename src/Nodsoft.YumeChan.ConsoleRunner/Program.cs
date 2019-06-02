using Nodsoft.YumeChan.Core;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.ConsoleRunner
{
	static class Program
	{
		static void Main(string[] args) => new YumeCore(new Logger()).RunBot();
	}
}
