using Discord;
using Discord.Commands;
using System.Threading.Tasks;

using static Nodsoft.YumeChan.Core.YumeCore;
using Nodsoft.YumeChan.PluginBase;

namespace Nodsoft.YumeChan.Core.Modules.Status
{
	[Group("status")]
	public class Status : ModuleBase<SocketCommandContext>, ICoreModule
	{
		internal const string MissingVersionSubstitute = "Unknown";

		[Command]
		public async Task CoreStatusAsync()
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle(Instance.CoreProperties.AppDisplayName)
				.WithDescription($"Status : {Instance.CoreState}")
				.AddField("Core", $"Version : {CoreVersion.ToString() ?? MissingVersionSubstitute}", true)
				.AddField("Loaded Modules", $"Count : {(Instance.CommandHandler.Plugins is null ? "None" : Instance.CommandHandler.Plugins.Count.ToString())}", true);
#if DEBUG
			embed.AddField("Debug", "Debug Build Active.");
#endif

			await ReplyAsync(embed: embed.Build());
		}

		[Command("plugins")]
		public async Task PluginsStatusAsync()
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Plugins")
				.WithDescription($"Currently Loaded : **{Instance.CommandHandler.Plugins.Count}** Plugins.");

			foreach (Plugin pluginManifest in Instance.CommandHandler.Plugins)
			{
				embed.AddField(pluginManifest.PluginDisplayName,
					$"({pluginManifest.PluginAssemblyName})\n" +
					$"Version : {pluginManifest.PluginVersion.ToString() ?? MissingVersionSubstitute}\n" +
					$"Loaded : {(pluginManifest.PluginLoaded ? "Yes" : "No")}", true);
			}

			await ReplyAsync(embed: embed.Build());
		}
	}
}
