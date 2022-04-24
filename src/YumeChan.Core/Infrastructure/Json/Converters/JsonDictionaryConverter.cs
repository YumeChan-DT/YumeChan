using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YumeChan.Core.Infrastructure.Json.Converters;

/// <summary>
/// Represents a custom JSON converter for Dictionary types.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class JsonDictionaryConverter<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>> where TKey : IConvertible
{
	public override bool CanConvert(Type typeToConvert)
	{
		/* Only use this converter if 
		 * 1. It's a dictionary
		 * 2. The key is not a string
		 */
		return typeToConvert.IsAssignableTo(typeof(IReadOnlyDictionary<TKey, TValue>)) && typeToConvert.GenericTypeArguments.First() != typeof(string);
	}
	public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		//Step 1 - Use built-in serializer to deserialize into a dictionary with string key
		Dictionary<string, TValue> dictionaryWithStringKey = (Dictionary<string, TValue>)JsonSerializer.Deserialize(ref reader, typeof(Dictionary<string, TValue>), options);


		//Step 2 - Convert the dictionary to one that uses the actual key type we want
		return dictionaryWithStringKey?.ToDictionary(
			kvp => (TKey)Convert.ChangeType(kvp.Key, typeof(TKey)), 
			kvp => kvp.Value
		);
	}

	public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options)
	{
		//Step 1 - Convert dictionary to a dictionary with string key
		Dictionary<string, TValue> dictionary = new(value.Count);

		foreach (KeyValuePair<TKey, TValue> kvp in value)
		{
			dictionary.Add(kvp.Key.ToString()!, kvp.Value);
		}
		
		//Step 2 - Use the built-in serializer, because it can handle dictionaries with string keys
		JsonSerializer.Serialize(writer, dictionary, options);
	}
}