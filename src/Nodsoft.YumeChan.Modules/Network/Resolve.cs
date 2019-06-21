using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.Network
{
	[Group("resolve")]
	public class Resolve : ModuleBase<SocketCommandContext>
	{
		[Command]
		public async Task ResolveCommand(string host)
		{
			string hostResolved;
			string contextUser = Context.User.Mention;

			if (IsIPAddress(host))
			{
				await ReplyAsync($"{contextUser}, Isn't ``{host}`` already an IP address ?");
			}
			else
			{
				try
				{
					IPAddress[] a = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
					hostResolved = a.FirstOrDefault().ToString();

					await ReplyAsync($"{contextUser}, Hostname ``{host}`` resolves to IP Address ``{hostResolved}``.");
				}
				catch (Exception e)
				{
					await ReplyAsync($"{contextUser}, Hostname ``{host}`` could not be resolved.\nException Thrown : {e.Message}");
				}
			}
		}

		internal static bool IsIPAddress(string address)
		{
			try
			{
				IPAddress.Parse(address);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
