using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using JetBrains.Annotations;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Config;


namespace YumeChan.Core.Modules;

/// <summary>
/// Provides commands to control the bot core.
/// </summary>
[SlashCommandGroup("control", "Controls the bot core"), PublicAPI, SlashRequireOwner, SlashModuleLifespan(SlashModuleLifespan.Singleton)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public sealed class ControlModule : ApplicationCommandModule, ICoreModule
{
	private readonly JsonConfigProvider<InternalPlugin> _jsonConfigProvider;
	private readonly IPluginLoaderProperties _pluginConfig;

	public ControlModule(JsonConfigProvider<InternalPlugin> jsonConfigProvider, IPluginLoaderProperties pluginConfig)
	{
		_jsonConfigProvider = jsonConfigProvider;
		_pluginConfig = pluginConfig;
	}
	
	/// <summary>
	/// Tests error handling by throwing an exception.
	/// </summary>
	/// <exception cref="ApplicationException">Always thrown.</exception>
	[SlashCommand("throw", "(DEBUG) Tests error handling"), DoesNotReturn]
	public static Task Throw(InteractionContext _) => throw new ApplicationException();

	/// <summary>
	/// Forces a garbage collection cycle.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("gc", "(DEBUG) Forces Memory GC cycle")]
	// ReSharper disable once InconsistentNaming
	public async Task ForceGCCollect(InteractionContext ctx)
	{
		GC.Collect(2, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(2, GCCollectionMode.Forced, true, true);

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = /*lang=Markdown*/$"Forced GC Cleanup cycle. \nCurrent memory usage: **{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB**"
		});
	}
	
	/// <summary>
	/// Restarts the bot core.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
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
	
	/// <summary>
	/// Lists all config 
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="filename">The name of the config file to list.</param>
	/// <param name="key">The key to query.</param>
	/// <param name="returnRaw">Whether to return the raw value or the parsed value.</param>
	[SlashCommand("config-get", "(DEBUG) Gets a config value")]
	public async Task GetConfig(InteractionContext ctx, [Option("file", "Name of config file to read.")] string filename,
		[Option("key", "Config key to query.")] string key, 
		[Option("return-raw", "Whether to return the raw value or the parsed value.")] bool returnRaw)
	{
		JsonWritableConfig config = _jsonConfigProvider.GetConfiguration(filename, true, false, false);

		object? value = returnRaw ? config.GetValue(key, typeof(string), true) : config.GetValue(key);
		
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = value switch
			{
				null => $"No value found for key `{key}`.",
				_ when returnRaw => $"Value (raw) for key `{key}` in file `{filename}`: ```json\n{value}```",
				_ => $"Value for key `{key}` in config file `{filename}` is: `{value}`."
			} 
		});
	}

	/// <summary>
	/// Sets a config value.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="filename">The name of the config file to set.</param>
	/// <param name="key">The key to set.</param>
	/// <param name="value">The value to set.</param>
	[SlashCommand("config-set", "(DEBUG) Sets a config value")]
	public async Task SetConfig(InteractionContext ctx, 
		[Option("file", "Name of config file to read.")] string filename,
		[Option("key", "Config key to set value for.")] string key,
		[Option("value", "Value to set for key. Defaults to null, to wipe.")] string? value = null)
	{
		JsonWritableConfig config = _jsonConfigProvider.GetConfiguration(filename, true, false, false);
		
		config[key] = value;
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Set value for key `{key}` in config file `{filename}` to `{value}`."
		});
	}
	
	/// <summary>
	/// Lists all config keys.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="filename">The name of the config file to list.</param>
	[SlashCommand("config-list", "(DEBUG) Lists all config keys")]
	public async Task ListConfig(InteractionContext ctx, [Option("file", "Name of config file to read.", true)] string filename)
	{
		JsonWritableConfig config = _jsonConfigProvider.GetConfiguration(filename, true, false, false);
		string[] keys = ((IDictionary<string, JsonNode?>)config.JsonData.AsObject()).Keys.ToArray();

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
	
	/// <summary>
	/// Lists all enabled plugins.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("list-plugins", "(DEBUG) Lists all enabled plugins")]
	public async Task ListPlugins(InteractionContext ctx)
	{
		IDictionary<string, string?> plugins = _pluginConfig.EnabledPlugins;
		StringBuilder sb = new("Plugins currently configured: \n```\n");
		
		foreach ((string key, string? value) in plugins)
		{
			sb.AppendLine($"{key} (v{value ?? "*"})");
		}
		
		sb.AppendLine("```");
		
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = sb.ToString()
		});
	}
	
	/// <summary>
	/// Adds a plugin to the enabled list.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="name">The name of the plugin to add.</param>
	/// <param name="version">
	///		The version of the plugin to add. 
	///		<value>Set to * to allow any version.</value>
	/// </param>
	[SlashCommand("add-plugin", "(DEBUG) Adds a plugin to the enabled list")]
	public async Task AddPlugin(InteractionContext ctx, 
		[Option("name", "Name of plugin to add.")] string name,
		[Option("version", "Version of plugin to add.")] string version = "*")
	{
		// Add plugin to list
		if (!_pluginConfig.EnabledPlugins.TryAdd(name, version))
		{
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
	
	/// <summary>
	/// Removes a plugin from the enabled list.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="name">The name of the plugin to remove.</param>
	[SlashCommand("remove-plugin", "(DEBUG) Removes a plugin from the enabled list")]
	public async Task RemovePlugin(InteractionContext ctx, 
		[Option("name", "Name of plugin to remove.")] string name)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = _pluginConfig.EnabledPlugins.Remove(name)
				? $"Removed plugin `{name}` from plugins list."
				: "Plugin was not found in plugins list."
		});
	}
	
	#endregion // Plugin Config
	
}