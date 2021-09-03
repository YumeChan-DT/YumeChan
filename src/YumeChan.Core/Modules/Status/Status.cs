using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using YumeChan.PluginBase;
using System;
using System.Threading.Tasks;
using static YumeChan.Core.YumeCore;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using DSharpPlus.SlashCommands.Attributes;

#pragma warning disable CA1822 // Statics cannot be used for Commands

namespace YumeChan.Core.Modules.Status
{
	[SlashCommandGroup("status", "Displays YumeCore Status")]
	public class Status : ApplicationCommandModule, ICoreModule
	{
		internal const string MissingVersionSubstitute = "Unknown";

		[SlashCommand("core", "Gets the status of current YumeCore.")]
		public async Task CoreStatusAsync(InteractionContext ctx)
		{
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
				.WithTitle(Instance.CoreProperties.AppDisplayName)
				.WithDescription($"Status : {Instance.CoreState}")
				.AddField("Core", $"Version : {CoreVersion ?? MissingVersionSubstitute}", true)
				.AddField("Loaded Plugins", $"Count : {(Instance.CommandHandler.Plugins is null ? "None" : Instance.CommandHandler.Plugins.Count)}", true);
#if DEBUG
			embed.AddField("Debug", "Debug Build Active.");
#endif

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AddEmbed(embed));
		}

		[SlashCommand("plugins", "Lists all loaded Plugins")]
		public async Task PluginsStatusAsync(InteractionContext ctx)
		{
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
				.WithTitle("Plugins")
				.WithDescription($"Currently Loaded : **{Instance.CommandHandler.Plugins.Count}** Plugins.");

			foreach (Plugin pluginManifest in Instance.CommandHandler.Plugins)
			{
				embed.AddField(pluginManifest.DisplayName,
					$"({pluginManifest.AssemblyName})\n" +
					$"Version : {pluginManifest.Version.ToString() ?? MissingVersionSubstitute}\n" +
					$"Loaded : {(pluginManifest.Loaded ? "Yes" : "No")}", true);
			}

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AddEmbed(embed));
		}

		[SlashCommand("botstats", "Gets the current stats for Yume-Chan.")]
		public async Task BotStat(InteractionContext ctx)
		{
			using Process process = Process.GetCurrentProcess();
			int guildCount = ctx.Client.Guilds.Count;
			int memberCount = ctx.Client.Guilds.Values.SelectMany(g => g.Members.Keys).Count();

			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
				.WithTitle($"{Instance.CoreProperties.AppDisplayName} - Bot Stats")
				.WithColor(DiscordColor.Gold)
				.AddField("Latency", $"{ctx.Client.Ping} ms", true)
				.AddField("Total Guilds", $"{guildCount}", true)
				.AddField("Total Members", $"{memberCount}", true)
				.AddField("Shards", $"{ctx.Client.ShardCount}", true)
				.AddField("Memory", $"{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB", true)
				.AddField("Threads", $"{ThreadPool.ThreadCount}", true);

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AddEmbed(embed));
		}

		[SlashCommand("throw", "(DEBUG) Tests error handling"), SlashRequireOwner]
		public Task Throw(InteractionContext _) => throw new ApplicationException();

		[SlashCommand("gc", "(DEBUG) Forces Memory GC cycle"), SlashRequireOwner]
		public async Task ForceGCCollect(InteractionContext ctx)
		{
			GC.Collect(2, GCCollectionMode.Forced, true, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, true, true);

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			{
				Content = $"Forced GC Cleanup cycle! \nCurrent memory usage: **{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB**"
			});
		}
	}
}
