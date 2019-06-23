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
			string contextUser = Context.User.Mention;

			if (host.IsIPAddress())
			{
				await ReplyAsync($"{contextUser}, Isn't ``{host}`` already an IP address ?");
			}
			else
			{
					await ReplyAsync(TryResolveHostname(host, out string hostResolved, out Exception e) 
						? $"{contextUser}, Hostname ``{host}`` resolves to IP Address ``{hostResolved}``."
						: $"{contextUser}, Hostname ``{host}`` could not be resolved.\nException Thrown : {e.Message}");
			}
		}

		public static async Task<IPAddress> ResolveHostnameAsync(string hostname)
		{
			IPAddress[] a = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
			return a.FirstOrDefault();
		}

		public static bool TryResolveHostname(string hostname, out string resolved, out Exception exception)
		{
			bool tryResult = TryResolveHostname(hostname, out IPAddress resolvedIp, out exception);
			resolved = resolvedIp.ToString();
			return tryResult;
		}
		public static bool TryResolveHostname(string hostname, out IPAddress resolved, out Exception exception)
		{
			IPAddress[] a;
			try
			{
				a = Dns.GetHostAddresses(hostname);
				resolved = a.FirstOrDefault();
				exception = null;
				return true;
			}
			catch (Exception e)
			{
				resolved = null;
				exception = e;
				return false;
			}
		}
	}
}
