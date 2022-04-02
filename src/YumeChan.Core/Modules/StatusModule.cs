using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using YumeChan.PluginBase;
using static YumeChan.Core.YumeCore;


namespace YumeChan.Core.Modules;
#pragma warning disable CA1822 // Statics cannot be used for Commands
// ReSharper disable once UnusedType.Global
// ReSharper disable UnusedMember.Global


[SlashCommandGroup("status", "Displays YumeCore Status")]
public class StatusModule : ApplicationCommandModule, ICoreModule
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

		foreach (IPlugin pluginManifest in Instance.CommandHandler.Plugins)
		{
			embed.AddField(pluginManifest.DisplayName,
				$"({pluginManifest.AssemblyName})\n" +
				$"Version : {pluginManifest.Version}\n" +
				$"Loaded : {(pluginManifest.Loaded ? "Yes" : "No")}", true);
		}

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AddEmbed(embed));
	}

	[SlashCommand("botstats", "Gets the current stats for Yume-Chan.")]
	public async Task BotStatsAsync(InteractionContext ctx)
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
}