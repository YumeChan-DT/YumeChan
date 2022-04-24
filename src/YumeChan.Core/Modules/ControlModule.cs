using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Config;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;


namespace YumeChan.Core.Modules;
#pragma warning disable CA1822 // Statics cannot be used for Commands
// ReSharper disable once UnusedType.Global
// ReSharper disable UnusedMember.Global

[SlashCommandGroup("control", "Controls the bot core"), SlashRequireOwner, SlashModuleLifespan(SlashModuleLifespan.Singleton)]
public class ControlModule : ApplicationCommandModule, ICoreModule
{
	private readonly JsonConfigProvider<InternalPlugin> _jsonConfigProvider;
	private readonly IPluginLoaderProperties _pluginConfig;

	public ControlModule(JsonConfigProvider<InternalPlugin> jsonConfigProvider, IPluginLoaderProperties pluginConfig)
	{
		_jsonConfigProvider = jsonConfigProvider;
		_pluginConfig = pluginConfig;
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
	
	#region JSON Config
	
	[SlashCommand("config-get", "(DEBUG) Gets a config value")]
	public async Task GetConfig(InteractionContext ctx, [Option("file", "Name of config file to read.")] string filename,
		[Option("key", "Config key to query.")] string key, 
		[Option("return-raw", "Whether to return the raw value or the parsed value.")] bool returnRaw)
	{
		JsonWritableConfig config = _jsonConfigProvider.GetConfiguration(filename, true, false, false);

		object value = returnRaw ? config.GetValue(key, typeof(string), true) : config.GetValue(key);
		
		if (value is null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"No value found for key `{key}`."
			});
			return;
		}

		if (returnRaw)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"Value (raw) for key `{key}` in file `{filename}`: ```json\n{value}```"
			});
			return;
		}
		else
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"Value for key `{key}` in config file `{filename}` is: `{value}`."
			});
		}
	}

	[SlashCommand("config-set", "(DEBUG) Sets a config value")]
	public async Task SetConfig(InteractionContext ctx, 
		[Option("file", "Name of config file to read.")] string filename,
		[Option("key", "Config key to set value for.")] string key,
		[Option("value", "Value to set for key. Defaults to null, to wipe.")] string value = null)
	{
		JsonWritableConfig config = _jsonConfigProvider.GetConfiguration(filename, true, false, false);
		
		config[key] = value;
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Set value for key `{key}` in config file `{filename}` to `{value}`."
		});
	}
	
	[SlashCommand("config-list", "(DEBUG) Lists all config keys")]
	public async Task ListConfig(InteractionContext ctx, [Option("file", "Name of config file to read.", true)] string filename)
	{
		JsonWritableConfig config = _jsonConfigProvider.GetConfiguration(filename, true, false, false);
		ImmutableArray<string> keys = ((IDictionary<string, JsonNode>)config.JsonData.AsObject()).Keys.ToImmutableArray();

		if (keys.Length is 0)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"No keys found in config file `{filename}`."
			});
			return;
		}
		StringBuilder sb = new($"Keys found in config file `{filename}`: \n```\n");
		foreach (string key in keys)
		{
			sb.AppendLine(key);
		}
		sb.AppendLine("```");
		
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = sb.ToString()
		});
	}
	
	#endregion // JSON Config
	
	#region Plugin Config
	
	[SlashCommand("list-plugins", "(DEBUG) Lists all enabled plugins")]
	public async Task ListPlugins(InteractionContext ctx)
	{
		IDictionary<string, string> plugins = _pluginConfig.EnabledPlugins;
		StringBuilder sb = new("Plugins currently configured: \n```\n");
		
		foreach (KeyValuePair<string, string> plugin in plugins)
		{
			sb.AppendLine($"{plugin.Key} (v{plugin.Value})");
		}
		
		sb.AppendLine("```");
		
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = sb.ToString()
		});
	}
	
	[SlashCommand("add-plugin", "(DEBUG) Adds a plugin to the enabled list")]
	public async Task AddPlugin(InteractionContext ctx, 
		[Option("name", "Name of plugin to add.")] string name,
		[Option("version", "Version of plugin to add.")] string version = "*")
	{
		// Add plugin to list
		if (!_pluginConfig.EnabledPlugins.ContainsKey(name))
		{
			_pluginConfig.EnabledPlugins.Add(name, version);
			
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"Added plugin `{name}` v`{version}` to plugins list."
			});
		}
		// Or update version
		else
		{
			_pluginConfig.EnabledPlugins[name] = version;
			
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"Updated plugin `{name}` to version `{version}`."
			});
		}
	}
	
	[SlashCommand("remove-plugin", "(DEBUG) Removes a plugin from the enabled list")]
	public async Task RemovePlugin(InteractionContext ctx, 
		[Option("name", "Name of plugin to remove.")] string name)
	{
		if (_pluginConfig.EnabledPlugins.ContainsKey(name))
		{
			_pluginConfig.EnabledPlugins.Remove(name);
			
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"Removed plugin `{name}` from plugins list."
			});
		}
		else
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = "Plugin was not found in plugins list."
			});
		}
	}
	
	#endregion // Plugin Config
	
}