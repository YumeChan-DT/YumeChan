using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
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
			string contextUser = Context.User.Mention;
			
			// 1A. Find out if supplied Hostname or IP
			bool hostIsIP = host.IsIPAddress();

			// 1B. Resolve if necessary
			if (!Resolve.TryResolveHostname(host, out IPAddress hostResolved, out Exception e))
			{
				await ReplyAsync($"{contextUser}, Hostname ``{host}`` could not be resolved.\nException Thrown : {e.Message}");
				return;
			}

			// 2. Ping the IP
			int pingCount = 4;
			PingReply[] pingReplies = ComplexPing(hostResolved, pingCount).Result;

			// 3. Retrieve statistics			// 4. Return results to user with ReplyAsync(); (Perhaps Embed ?)
			List<long> roundTripTimings = new List<long>();

			EmbedBuilder embedBuilder = new EmbedBuilder
			{
				Title = "Ping Results",
				Description = $"Results of Ping on **{host}** " + (hostIsIP ? ":" : $"({hostResolved}) :")
			};

			for (int i = 0; i < pingCount; i++)
			{
				EmbedFieldBuilder field = new EmbedFieldBuilder { Name = $"Ping {i}", IsInline = true };
				if (pingReplies[i].Status == IPStatus.Success)
				{
					field.Value = $"RTD = **{pingReplies[i].RoundtripTime}** ms";
					roundTripTimings.Add(pingReplies[i].RoundtripTime);
				}
				else { field.Value = $"Error : **{pingReplies[i].Status.ToString()}**"; }

				embedBuilder.AddField(field);
			}

			embedBuilder.AddField("Average RTD", (roundTripTimings.Count is 0 
				? $"No RTD Average Assertable : No packets returned from Pings."
				: $"Average Round-Trip Time/Delay = **{roundTripTimings.Average().ToString()}** ms / **{roundTripTimings.Count}** packets"));

			await ReplyAsync(message: contextUser, embed: embedBuilder.Build()); //Quote user in main message, and attach Embed.
		}

		internal static async Task<PingReply[]> ComplexPing(IPAddress host, int count) => await ComplexPing(host, count, 2000, new PingOptions(64, true)).ConfigureAwait(false);
		internal static async Task<PingReply[]> ComplexPing(IPAddress host, int count, int timeout, PingOptions options)
		{
			PingReply[] pingReplies = new PingReply[count];

			// Create a buffer of 32 bytes of data to be transmitted.
			byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

			// Send the request.
			for (int i = 0; i < count; i++)
			{
				Ping ping = new Ping();
				pingReplies[i] = await ping.SendPingAsync(host, timeout, buffer, options).ConfigureAwait(false);
				ping.Dispose();
			}

			return pingReplies;
		}

	}
}
