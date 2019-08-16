using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.Network
{
	[Group("ping")]
	public class PingModule : ModuleBase<SocketCommandContext>
	{
		[Command("", RunMode = RunMode.Async)]
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
			PingModuleReply[] pingReplies = TcpPing(hostResolved, 80, pingCount).Result;

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

		internal static async Task<PingModuleReply[]> ComplexPing(IPAddress host, int count) => await ComplexPing(host, count, 2000, new PingOptions(64, true)).ConfigureAwait(false);
		internal static async Task<PingModuleReply[]> ComplexPing(IPAddress host, int count, int timeout, PingOptions options)
		{
			PingModuleReply[] pingReplies = new PingModuleReply[count];

			// Create a buffer of 32 bytes of data to be transmitted.
			byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

			// Send the request.
			for (int i = 0; i < count; i++)
			{
				Ping ping = new Ping();
				pingReplies[i] = new PingModuleReply(await ping.SendPingAsync(host, timeout, buffer, options).ConfigureAwait(false));
				ping.Dispose();
			}

			return pingReplies;
		}

		// See : https://stackoverflow.com/questions/26067342/how-to-implement-psping-tcp-ping-in-c-sharp
		internal static Task<PingModuleReply[]> TcpPing(IPAddress host, int port, int count) 
		{
			PingModuleReply[] pingReplies = new PingModuleReply[count];
			for (int i = 0; i < count; i++)
			{
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Blocking = true;

				Stopwatch latencyMeasurement = new Stopwatch();
				IPStatus? status = null;
				try
				{
					latencyMeasurement.Start();
					socket.Connect(host, port);
					latencyMeasurement.Stop();
				}
				catch (Exception)
				{
					status = IPStatus.TimedOut;
				}

				pingReplies[i] = new PingModuleReply(host, latencyMeasurement.ElapsedMilliseconds, status ?? IPStatus.Success );
			}
			
			return Task.FromResult(pingReplies);
		}

		internal struct PingModuleReply
		{
			internal IPAddress Host { get; }
			internal long RoundtripTime { get; }
			internal IPStatus Status { get; }

			public PingModuleReply(IPAddress host, long roundtripTime, IPStatus status)
			{
				Host = host;
				RoundtripTime = roundtripTime;
				Status = status;
			}
			public PingModuleReply(PingReply reply)
			{
				Host = reply.Address;
				RoundtripTime = reply.RoundtripTime;
				Status = reply.Status;
			}
		}
	}
}
