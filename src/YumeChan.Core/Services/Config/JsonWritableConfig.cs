using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

/// <summary>
/// Represents a Read-Write JSON-based configuration file.
/// </summary>
internal class JsonWritableConfig : IWritableConfiguration
{
	public string CurrentPrefix { get; }

	public bool IsFirstLoad { get; }

	private readonly FileInfo _file;
	private readonly ILogger<JsonWritableConfig> _logger;
	private readonly bool _autoreload;
	private readonly JsonSerializerOptions _serializerOptions;
	private readonly IChangeToken _changeToken;
	private readonly bool _autosave;
	private JsonNode _jsonData;

	public JsonWritableConfig(FileInfo file, JsonSerializerOptions serializerOptions, ILogger<JsonWritableConfig> logger,
		IChangeToken changeToken = null,
		string prefix = null,
		bool firstLoad = false,
		bool autosave = true,
		bool autoreload = true)
	{
		_file = file ?? throw new ArgumentNullException(nameof(file));
		_logger = logger;
		_autoreload = autoreload;
		_serializerOptions = serializerOptions ?? new(JsonSerializerDefaults.General);
		CurrentPrefix = prefix;
		IsFirstLoad = firstLoad;


		if (autoreload)
		{
			_changeToken = changeToken;
			_changeToken?.RegisterChangeCallback(_ => LoadFromFileAsync().GetAwaiter().GetResult(), null);
		}

		if (autosave)
		{
			_autosave = true;
		}

		if (!firstLoad)
		{
			try
			{
				LoadFromFileAsync().GetAwaiter().GetResult();
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to load config file {FileName}.", file.FullName);
			}
		}

		// Fallback to creating a new dictionnary if _jsonData is null
		_jsonData ??= new JsonObject();
	}

	/// <inheritdoc />
	public string this[string key]
	{
		get => GetValue<string>(key);
		set => SetValue(key, value);
	}

	/// <summary>
	/// Loads a JSON-keyed <see cref="Dictionary{TKey,TValue}"/> from the file.
	/// </summary>
	public async Task LoadFromFileAsync()
	{
		await using FileStream stream = _file.OpenRead();
		_jsonData = await JsonSerializer.DeserializeAsync<JsonNode>(stream, _serializerOptions);
		_logger.LogDebug("Loaded config file {FileName}.", _file.FullName);
	}

	/// <summary>
	/// Saves JSON-keyed dictionary to file as serialized JSON.
	/// </summary>
	public async Task SaveToFileAsync()
	{
		await using FileStream stream = _file.OpenWrite();
		await JsonSerializer.SerializeAsync(stream, _jsonData, _serializerOptions);
		_logger.LogDebug("Saved config file {FileName}.", _file.FullName);
	}

	/// <inheritdoc />
	/// <summary>
	/// Gets a value from the JSON-keyed dictionary, and returns a prefixed <see cref="IWritableConfiguration" /> if the value is a complex object.
	/// Returns null if a value is not found.
	/// </summary>
	public object GetValue(string path)
	{
		// Sanitize string, then get absolute JSON path relative to the current prefix
		path = ParseRelativePath(path, CurrentPrefix);

		// Introspect down the JSON tree
		JsonNode node = _jsonData;

		foreach (string key in path.Split(':'))
		{
			if (node is JsonObject obj)
			{
				if (((IDictionary<string, JsonNode>)obj).TryGetValue(key, out JsonNode value))
				{
					node = value;
				}
				else
				{
					return null;
				}
			}
			else if (node is JsonArray arr)
			{
				if (int.TryParse(key, out int index) && index >= 0 && index < arr.Count)
				{
					node = arr[index];
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		return node switch
		{
			// Return the value if it's a primitive type
			JsonObject or JsonArray => new JsonWritableConfig(_file, _serializerOptions, _logger, _changeToken, path, false, _autosave, false),
			_                       => node?.AsValue()
		};
	}

	/// <inheritdoc />
	/// <summary>
	/// Gets a value from the JSON-Keyed dictionary, or returns the default value if the value cannot be casted or is not found.
	/// </summary>
	public T GetValue<T>(string path) => GetValue(path, typeof(T)) is T value ? value : default;

	internal object GetValue(string path, Type returnType) => GetValue(path) switch
	{
		JsonValue value          => value.Deserialize(returnType, _serializerOptions),
		JsonWritableConfig value => value,
		null					 => null,
		_                        => throw new JsonException($"Cannot cast value on key {path} to type {returnType.FullName}.")
	};

	/// <inheritdoc />
	public void SetValue(string path, object value) => SetValue<object>(path, value);

	/// <inheritdoc />
	/// <summary>
	/// Set property in _json at specified path (creating any missing nodes)
	/// </summary>
	public void SetValue<T>(string path, T value) => SetValue(path, value, typeof(T));

	internal void SetValue(string path, object value, Type valueType)
	{
		path = ParseRelativePath(path, CurrentPrefix);


		// Introspect down the JSON tree, until last node is reached
		JsonNode node = _jsonData;

		foreach (string key in path.Split(':'))
		{
			node = node switch
			{
				JsonObject obj when ((IDictionary<string, JsonNode>)obj).TryGetValue(key, out JsonNode jsonValue) => jsonValue,
				JsonObject obj                                                                                    => obj[key] = new JsonObject(),
				JsonArray arr                                                                                     => int.TryParse(key, out int index) && index >= 0 && index < arr.Count ? arr[index] : arr[index] = new JsonArray(),
				_                                                                                                 => _jsonData[key] = new JsonObject()
			};
		}

		// Set the value on the last node
		if (node is JsonObject)
		{
			node = node.Parent ?? node;

			// Assign directly if the value is a string
			if (value is string str)
			{
				node[path.Split(':').Last()] = str;
			}
			else
			{
				// Serialize the value to JSON
				node[path.Split(':').Last()] = JsonSerializer.Serialize(value, valueType, _serializerOptions);
			}
			
		}
		else if (node is JsonArray arr)
		{
			arr[int.Parse(path.Split(':').Last())] = JsonSerializer.Serialize(value, valueType, _serializerOptions);
		}
		else
		{
			throw new InvalidOperationException($"Cannot set value on node {node}");
		}

		// Save to file if autosave is enabled
		if (_autosave)
		{
			SaveToFileAsync().GetAwaiter().GetResult();
		}
	}

	/// <summary>
	/// Sanitizes string, then get absolute JSON path relative to the current prefix
	/// </summary>
	private static string ParseRelativePath(string path, string currentPrefix)
	{
		if (path.EndsWith(':'))
		{
			path = path[..^1];
		}

		return currentPrefix is not null ? $"{currentPrefix}:{path}" : path;
	}

	/// <inheritdoc />
	IWritableConfiguration IWritableConfiguration.GetSection(string key) => GetSectionInternal(key);

	/// <inheritdoc />
	IConfigurationSection IConfiguration.GetSection(string key) => throw new NotImplementedException();

	/// <inheritdoc />
	public IEnumerable<IConfigurationSection> GetChildren() => throw new NotImplementedException();

	/// <inheritdoc />
	public IChangeToken GetReloadToken() => _changeToken;

	/// <summary>
	/// Gets a new <see cref="JsonWritableConfig"/> instance with the specified prefix.
	/// </summary>
	/// <param name="prefix">Prefix of section</param>
	/// <returns>Section of type <see cref="JsonWritableConfig"/></returns>
	internal JsonWritableConfig GetSectionInternal(string prefix)
	{
		// Sanitize string, then get absolute JSON path relative to the current prefix
		if (prefix.EndsWith(':'))
		{
			prefix = prefix[..^1];
		}

		prefix = CurrentPrefix is not null ? $"{CurrentPrefix}:{prefix}" : prefix;

		return new(_file, _serializerOptions, _logger, _changeToken, prefix, IsFirstLoad, _autosave, _autoreload);
	}
}