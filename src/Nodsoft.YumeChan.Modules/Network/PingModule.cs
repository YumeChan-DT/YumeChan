using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.Network
{
	[Group("ping")]
	public class PingModule : ModuleBase<SocketCommandContext>
	{
		//[Command]
		public async Task SimplePingCommand()
		{
			await ReplyAsync("Pong !");
		}

		[Command("")]
		public async Task NetworkPingCommand(string host)
		{
#if RELEASE
			throw new NotImplementedException();
#endif

			string contextUser = Context.User.Mention;
			IPAddress resolvedHost;
			// 1A. Find out if supplied Hostname or IP
			bool hostIsIP = host.IsIPAddress();

			// 1B. Resolve if necessary
			try
			{
				resolvedHost = hostIsIP ? IPAddress.Parse(host) : await Resolve.ResolveHostnameAsync(host).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				await ReplyAsync($"{contextUser}, Hostname ``{host}`` could not be resolved.\nException Thrown : {e.Message}");
				return;
			}

			// 2. Ping the IP
			int pingCount = 4;
			PingReply[] pingReplies = ComplexPing(resolvedHost, pingCount).Result;

			// 3. Retrieve statistics
			IPStatus[] pingMessages = new IPStatus[pingCount];
			long[] roundTripTimings = new long[pingCount];

			for (int i = 0; i < pingCount; i++)
			{
				if (pingReplies[i].Status == IPStatus.Success)
				{
					roundTripTimings[i] = pingReplies[i].RoundtripTime;
				}

				pingMessages[i] = pingReplies[i].Status;
			}

			double roundTripMedian = roundTripTimings.Average();
			// 4. Return results to user with ReplyAsync(); (Perhaps Embed ?)

			EmbedBuilder embedBuilder = new EmbedBuilder()
			{
				Title = "Ping Results",
				Description = $"Results of Ping on **{host}** " + (hostIsIP ? ":" : $"({resolvedHost}) :")
			};

			for (int i = 0; i < pingCount; i++)
			{
				embedBuilder.AddField(
					name: $"Ping {i}",
					value: pingReplies[i].Status == IPStatus.Success
						? $"RTD = {roundTripTimings[i]} ms"
						: $"Error : {pingReplies[i].Status.ToString()}",
					inline: true);
			}

			embedBuilder.AddField("Average RTD", $"Average Round-Trip Time/Delay = {roundTripTimings.Average().ToString()} ms");

			await ReplyAsync(message: contextUser, embed: embedBuilder.Build()); //Quote user in main message, and attach Embed.
		}

		internal static async Task<PingReply[]> ComplexPing(IPAddress host, int count) => await ComplexPing(host, count, 2000, new PingOptions(64, true)).ConfigureAwait(false);
		internal static async Task<PingReply[]> ComplexPing(IPAddress host, int count, int timeout, PingOptions options)
		{
			// Create a buffer of 32 bytes of data to be transmitted.
			byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");


			// Send the request.

			PingReply[] pingReplies = new PingReply[count];

			for (int i = 0; i < count; i++)
			{
				pingReplies[i] = await new Ping().SendPingAsync(host, timeout, buffer, options).ConfigureAwait(false);
			}

			return pingReplies;
		}

	}
}
