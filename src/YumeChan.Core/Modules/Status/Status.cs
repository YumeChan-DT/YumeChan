using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using YumeChan.PluginBase;
using System;
using System.Threading.Tasks;
using static YumeChan.Core.YumeCore;


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
		public Task Throw(CommandContext _) => throw new ApplicationException();
	}
}
