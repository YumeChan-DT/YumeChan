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
		public static string MissingVersionSubstitute { get; } = "Unknown";

		[Command]
		public async Task CoreStatusAsync()
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Yume-Chan")
				.WithDescription($"Status : {Instance.CoreState.ToString()}")
				.AddField("Core", $"Version : {(CoreVersion != null ? CoreVersion.ToString() : MissingVersionSubstitute)}", true)
				.AddField("Loaded Modules", $"Count : {(Instance.Plugins != null ? Instance.Plugins.Count.ToString() : "None")}", true);
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
				.WithDescription($"Currently Loaded : **{Instance.Plugins.Count}** Plugins.");

			foreach (IPlugin pluginManifest in Instance.Plugins)
			{
				embed.AddField(pluginManifest.PluginDisplayName,
					$"Version : {(pluginManifest.PluginVersion != null ? pluginManifest.PluginVersion.ToString() : MissingVersionSubstitute)}\n" +
					$"Loaded : {(pluginManifest.PluginLoaded ? "Yes" : "No")}", true);
			}

			await ReplyAsync(embed: embed.Build());
		}
	}
}
