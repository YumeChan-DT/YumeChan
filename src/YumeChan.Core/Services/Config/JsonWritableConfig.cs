using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

internal class JsonWritableConfig : IWritableConfiguration
{
	private readonly FileInfo _file;
	private readonly JsonSerializerOptions _serializerOptions;
	private JsonNode _json;
	private readonly bool _autosave;
	private readonly bool _autoreload;

	public JsonWritableConfig(FileInfo file, JsonSerializerOptions serializerOptions, string prefix = null, bool autosave = true, bool autoreload = true)
	{
		_autosave = autosave;
		_autoreload = autoreload;
		CurrentPrefix = prefix;
		_file = file ?? throw new ArgumentNullException(nameof(file));
		_serializerOptions = serializerOptions ?? new(JsonSerializerDefaults.General);


	}

	public string this[string parameter]
	{
		get => GetValue<string>(parameter);
		set => SetValue(parameter, value);
	}

	public string CurrentPrefix { get; }

	public IWritableConfiguration GetSection(string sectionName)
	{
		throw new NotImplementedException();
	}

	public object GetValue(string parameter) => _json[parameter]?.AsValue();

	public T GetValue<T>(string parameter) => _json[parameter].GetValue<T>();

	public Task LoadFromFileAsync()
	{
		using FileStream fs = File.OpenRead(_file.FullName);

		try
		{
			_json = JsonNode.Parse(fs);
		}
		finally
		{
			fs.Close();
		}

		if (CurrentPrefix is not null)
		{
			_json = _json[CurrentPrefix];
		}

		return Task.CompletedTask;
	}

	public Task SaveToFileAsync()
	{
		using FileStream fs = File.OpenWrite(_file.FullName);
		using Utf8JsonWriter writer = new(fs);

		try
		{
			_json.WriteTo(writer, _serializerOptions);
		}
		finally
		{
			writer.Flush();
			writer.Dispose();
		}

		return Task.CompletedTask;
	}

	public void SetValue(string parameter, object value) => _json[parameter] = JsonValue.Create(value);

	public void SetValue<T>(string parameter, T value) => _json[parameter] = JsonValue.Create(value);


	private string FromCurrentPrefix(string value) => CurrentPrefix is null ? value : $"{CurrentPrefix}:{value}";
}
