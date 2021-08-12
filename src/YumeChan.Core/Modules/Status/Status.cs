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





#pragma warning disable CA1822 // Statics cannot be used for Commands



namespace YumeChan.Core.Modules.Status
{
	[Group("status"), Description("Displays YumeCore Status")]
	public class Status : BaseCommandModule, ICoreModule
	{
		internal const string MissingVersionSubstitute = "Unknown";

		[GroupCommand]
		public async Task CoreStatusAsync(CommandContext context)
		{
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
				.WithTitle(Instance.CoreProperties.AppDisplayName)
				.WithDescription($"Status : {Instance.CoreState}")
				.AddField("Core", $"Version : {CoreVersion.ToString() ?? MissingVersionSubstitute}", true)
				.AddField("Loaded Plugins", $"Count : {(Instance.CommandHandler.Plugins is null ? "None" : Instance.CommandHandler.Plugins.Count.ToString())}", true);
#if DEBUG
			embed.AddField("Debug", "Debug Build Active.");
#endif

			await context.RespondAsync(embed: embed.Build());
		}

		[Command("plugins"), Description("Lists all loaded Plugins")]
		public async Task PluginsStatusAsync(CommandContext ctx)
		{
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
				.WithTitle("Plugins")
				.WithDescription($"Currently Loaded : **{Instance.CommandHandler.Plugins.Count}** Plugins.");

			foreach (Plugin pluginManifest in Instance.CommandHandler.Plugins)
			{
				embed.AddField(pluginManifest.PluginDisplayName,
					$"({pluginManifest.PluginAssemblyName})\n" +
					$"Version : {pluginManifest.PluginVersion.ToString() ?? MissingVersionSubstitute}\n" +
					$"Loaded : {(pluginManifest.PluginLoaded ? "Yes" : "No")}", true);
			}

			await ctx.RespondAsync(embed: embed.Build());
		}

		[Command("botstats"), Aliases("botinfo"), Description("Gets the current stats for Yume-Chan.")]
		public async Task BotStat(CommandContext ctx)
		{
			using var process = Process.GetCurrentProcess();
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

			await ctx.RespondAsync(embed);
		}

		[Command("throw"), RequireOwner]
		public Task Throw(CommandContext _) => throw new ApplicationException();

		[Command("gc"), RequireOwner]
		public async Task ForceGCCollect(CommandContext ctx)
		{
			GC.Collect(2, GCCollectionMode.Forced, true, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, true, true);

			await ctx.RespondAsync($"Forced GC Cleanup cycle! \nCurrent memory usage: **{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB**");
		}
	}
}
