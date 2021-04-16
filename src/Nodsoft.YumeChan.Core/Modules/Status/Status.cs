using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nodsoft.YumeChan.PluginBase;
using System;
using System.Threading.Tasks;
using static Nodsoft.YumeChan.Core.YumeCore;


namespace Nodsoft.YumeChan.Core.Modules.Status
{
	[Group("status")]
	public class Status : BaseCommandModule, ICoreModule
	{
		internal const string MissingVersionSubstitute = "Unknown";

		[Command]
		public async Task CoreStatusAsync(CommandContext context)
		{
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
				.WithTitle(Instance.CoreProperties.AppDisplayName)
				.WithDescription($"Status : {Instance.CoreState}")
				.AddField("Core", $"Version : {CoreVersion.ToString() ?? MissingVersionSubstitute}", true)
				.AddField("Loaded Modules", $"Count : {(Instance.CommandHandler.Plugins is null ? "None" : Instance.CommandHandler.Plugins.Count.ToString())}", true);
#if DEBUG
			embed.AddField("Debug", "Debug Build Active.");
#endif

			await context.RespondAsync(embed: embed.Build());
		}

		[Command("plugins")]
		public async Task PluginsStatusAsync(CommandContext context)
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

			await context.RespondAsync(embed: embed.Build());
		}

		[Command("throw"), RequireOwner]
		public Task ThrowAsync() => throw new ApplicationException();
	}
}
