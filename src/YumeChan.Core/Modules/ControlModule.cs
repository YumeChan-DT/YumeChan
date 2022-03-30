using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;


namespace YumeChan.Core.Modules;
#pragma warning disable CA1822 // Statics cannot be used for Commands
// ReSharper disable once UnusedType.Global
// ReSharper disable UnusedMember.Global

[SlashCommandGroup("control", "Controls the bot core"), SlashRequireOwner, SlashModuleLifespan(SlashModuleLifespan.Singleton)]
public class ControlModule : ApplicationCommandModule, ICoreModule
{
	private readonly IWritableConfiguration _configuration;

	public ControlModule(IJsonConfigProvider<InternalPlugin> configuration)
	{
		_configuration = configuration?.GetConfiguration("spike");
	}
	
	[SlashCommand("throw", "(DEBUG) Tests error handling")]
	public Task Throw(InteractionContext _) => throw new ApplicationException();

	[SlashCommand("gc", "(DEBUG) Forces Memory GC cycle")]
	public async Task ForceGCCollect(InteractionContext ctx)
	{
		GC.Collect(2, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(2, GCCollectionMode.Forced, true, true);

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Forced GC Cleanup cycle. \nCurrent memory usage: **{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB**"
		});
	}
		
	[SlashCommand("reload", "(DEBUG) Reloads YumeCore")]
	public async Task Restart(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = "Restarting Yume-Chan..."
		});

		await ctx.Client.DisconnectAsync();
		await ctx.Client.ConnectAsync();
	}
	
	
	[SlashCommand("config-get", "(DEBUG) Gets a config value")]
	public async Task GetConfig(InteractionContext ctx, [Option("key", "Config key to get value from.")] string key)
	{
		object value = _configuration[key];
		
		if (value is null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"No value found for key `{key}`."
			});
			return;
		}

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Value for key `{key}` is `{value}`."
		});
	}

	[SlashCommand("config-set", "(DEBUG) Sets a config value")]
	public async Task SetConfig(InteractionContext ctx, 
		[Option("key", "Config key to set value for.")] string key,
		[Option("value", "Value to set for key.")] string value)
	{
		_configuration[key] = value;
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Set value for key `{key}` to `{value}`."
		});
	} 
}