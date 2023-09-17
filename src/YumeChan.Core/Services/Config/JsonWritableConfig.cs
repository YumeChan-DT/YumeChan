using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

/// <summary>
/// Represents a Read-Write JSON-based configuration file.
/// </summary>
internal sealed class JsonWritableConfig : IWritableConfiguration
{
	public string? CurrentPrefix { get; }

	public bool IsFirstLoad { get; }

	private readonly FileInfo _file;
	private readonly ILogger<JsonWritableConfig> _logger;
	private readonly bool _autoreload;
	private readonly JsonSerializerOptions _serializerOptions;
	private readonly IChangeToken? _changeToken;
	private readonly bool _autosave;
	internal JsonNode JsonData;


	/// <summary>
	/// Public constructor, used to create or load a new configuration file.
	/// </summary>
	public JsonWritableConfig(FileInfo file, JsonSerializerOptions? serializerOptions, ILogger<JsonWritableConfig> logger,
		IChangeToken? changeToken = null,
		string? prefix = null,
		bool firstLoad = false,
		bool autosave = true,
		bool autoreload = true)
		: this(file, serializerOptions, logger, changeToken, prefix, firstLoad, autosave, autoreload, null) { }
	
	/// <summary>
	/// Private constructor, used in section loading and nested operations.
	/// </summary>
	private JsonWritableConfig(FileInfo file, JsonSerializerOptions? serializerOptions, ILogger<JsonWritableConfig> logger,
		IChangeToken? changeToken,
		string? prefix,
		bool firstLoad,
		bool autosave,
		bool autoreload,
		JsonNode? jsonData
	) {
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

		if (jsonData is null && !firstLoad)
		{
			try
			{
				LoadFromFileAsync().GetAwaiter().GetResult();
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to load config file {fileName}", file.FullName);
			}
		}

		// Fallback to creating a new dictionnary if _jsonData is null, and a null jsonData was provided.
		JsonData ??= jsonData ?? new JsonObject();
	}

	/// <inheritdoc />
	public string? this[string key]
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
		
		JsonData = await JsonSerializer.DeserializeAsync<JsonNode>(stream, _serializerOptions) 
			?? throw new InvalidOperationException("Failed to deserialize JSON file.");
		
		_logger.LogDebug("Loaded config file {fileName}", _file.FullName);
	}

	/// <summary>
	/// Saves JSON-keyed dictionary to file as serialized JSON.
	/// </summary>
	public async Task SaveToFileAsync()
	{
		await using FileStream stream = _file.OpenWrite();
		await JsonSerializer.SerializeAsync(stream, JsonData, _serializerOptions);
		_logger.LogDebug("Saved config file {fileName}", _file.FullName);
	}

	/// <inheritdoc />
	/// <summary>
	/// Gets a value from the JSON-keyed dictionary, and returns a prefixed <see cref="IWritableConfiguration" /> if the value is a complex object.
	/// Returns null if a value is not found.
	/// </summary>
	public object? GetValue(string path) => GetValue(path, typeof(object));

	/// <inheritdoc />
	/// <summary>
	/// Gets a value from the JSON-Keyed dictionary, or returns the default value if the value cannot be casted or is not found.
	/// </summary>
	public T? GetValue<T>(string path) => GetValue(path, typeof(T)) is T value ? value : default;

	internal object? GetValue(string path, Type returnType, bool returnRaw = false)
	{
		// Sanitize string, then get absolute JSON path relative to the current prefix
		path = ParseRelativePath(path, CurrentPrefix);

		// Introspect down the JSON tree
		JsonNode node = JsonData;

		foreach (string key in path.Split(':'))
		{
			switch (node)
			{
				// If the node is an object, try to get the value from the key, as if it were a dictionary.
				case JsonObject obj when ((IDictionary<string, JsonNode?>)obj).TryGetValue(key, out JsonNode? value) && value is not null:
					node = value;
					break;

				// If the node is an array, try to get the value from the index, so long as it's within the array's bounds (and not empty).
				case JsonArray arr when int.TryParse(key, out int index) && index >= 0 && index < arr.Count:
					node = arr[index]!;
					break;

				default:
					return null;
			}
		}

		object? returnValue = node switch
		{
			null => null,
			
			// If returnRaw is true, return the raw value.
			_ when returnRaw => node.ToJsonString(_serializerOptions),
			
			// Return the value if it's a primitive type, or adapt it to the returnType.
			JsonObject when returnType.IsAssignableTo(typeof(IDictionary)) || returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IDictionary<,>) => node,
			JsonArray when returnType.IsAssignableTo(typeof(IEnumerable)) => node,
			JsonObject or JsonArray => new JsonWritableConfig(_file, _serializerOptions, _logger, _changeToken, path, false, _autosave, false, JsonData),
			_ => node.AsValue()
		};

		return returnValue switch
		{
			null => null,
			
			// If returnRaw is true, return the raw value.
			string s when returnRaw => s,
			
			// Otherwise, try to cast the value to the returnType.
			JsonValue value => value.Deserialize(returnType, _serializerOptions),
			JsonArray value => value.Deserialize(returnType, _serializerOptions),
			JsonObject value => value.Deserialize(returnType, _serializerOptions),
			JsonWritableConfig value => value,
			
			_ => throw new JsonException($"Cannot cast value on key {path} to type {returnType.FullName}.")
		};
	}

	/// <inheritdoc />
	public void SetValue(string path, object? value) => SetValue<object>(path, value);

	/// <inheritdoc />
	/// <summary>
	/// Set property in _json at specified path (creating any missing nodes)
	/// </summary>
	public void SetValue<T>(string path, T? value) => SetValue(path, value, typeof(T));

	internal void SetValue(string path, object? value, Type valueType)
	{
		path = ParseRelativePath(path, CurrentPrefix);
		
		// Introspect down the JSON tree, until last node is reached
		JsonNode? node = JsonData;

		foreach (string key in path.Split(':'))
		{
			node = node switch
			{
				JsonObject obj when ((IDictionary<string, JsonNode?>)obj).TryGetValue(key, out JsonNode? jsonValue) => jsonValue,
				JsonObject obj => obj[key] = new JsonObject(),
				JsonArray arr => int.TryParse(key, out int index) && index >= 0 && index < arr.Count ? arr[index] : arr[index] = new JsonArray(),
				_ => JsonData[key] = new JsonObject()
			};
		}

		// Set the value on the last node
		if (node is JsonObject)
		{
			node = node.Parent ?? node;

			switch (value)
			{
				// Assign directly if the value is a string
				case string str:
					node[path.Split(':').Last()] = str;
					break;
				
				// Special case for Dictionary types
				case IDictionary dictionary:
					JsonObject jsonObject = new();

					foreach (DictionaryEntry entry in dictionary)
					{
						// I swear this is the only way to do this... Serialize the value to JSON, then deserialize it back to a JsonNode...
						jsonObject[entry.Key.ToString()!] = JsonNode.Parse(JsonSerializer.Serialize(entry.Value, _serializerOptions));
					}

					node[path.Split(':').Last()] = jsonObject;
					break;

				// Special case made for enumerables
				case IEnumerable enumerable:
					JsonArray arr = new();

					foreach (object item in enumerable)
					{
						arr.Add(item);
					}

					node[path.Split(':').Last()] = arr;
					break;
				
				default:
					// Serialize the value to JSON
					node[path.Split(':').Last()] = JsonSerializer.Serialize(value, valueType, _serializerOptions);
					break;
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
	private static string ParseRelativePath(string path, string? currentPrefix)
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
	public IChangeToken? GetReloadToken() => _changeToken;

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

		return new(_file, _serializerOptions, _logger, _changeToken, prefix, IsFirstLoad, _autosave, _autoreload, JsonData);
	}
}