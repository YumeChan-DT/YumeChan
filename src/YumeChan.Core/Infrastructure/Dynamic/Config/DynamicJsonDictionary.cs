using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using YumeChan.Core.Services.Config;

namespace YumeChan.Core.Infrastructure.Dynamic.Config;

/// <summary>
/// Provides a JSON-Config backed Dictionary of key <see cref="TKey" /> and value <see cref="TValue" />.
/// </summary>
internal sealed class DynamicJsonDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, ISerializable, IDeserializationCallback 
	where TKey : notnull
{
	private readonly Dictionary<TKey, TValue> _dictionary;
	private readonly JsonWritableConfig _config;
	private readonly string _jsonPath;

	public DynamicJsonDictionary(JsonWritableConfig config, string jsonPath, IEnumerable<KeyValuePair<TKey, TValue>>? collection = null)
	{
		_config = config;
		_jsonPath = jsonPath;
		_dictionary = collection is null ? new() : new(collection);
	}

	public void Add(TKey key, TValue value)
	{
		_dictionary.Add(key, value);
		_config.SetValue(_jsonPath, _dictionary);
	}

	bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => _dictionary.ContainsKey(key);
	bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => _dictionary.ContainsKey(key);
	
	bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);
	bool IDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);
	
	public bool Remove(TKey key)
	{
		bool result = _dictionary.Remove(key);
		_config.SetValue(_jsonPath, _dictionary);
		return result;
	}

	public TValue this[TKey key]
	{
		get => _dictionary[key];
		set
		{
			_dictionary[key] = value;
			_config.SetValue(_jsonPath, _dictionary);
		}
	}

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dictionary.Keys;
	ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dictionary.Keys;
	ICollection IDictionary.Keys => _dictionary.Keys;	
	
	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dictionary.Values;
	ICollection<TValue> IDictionary<TKey, TValue>.Values => _dictionary.Values;
	ICollection IDictionary.Values => _dictionary.Values;


	object? IDictionary.this[object key]
	{
		get => (_dictionary as IDictionary)[key];

		set
		{
			if (key is TKey tKey && value is TValue tValue)
			{
				this[tKey] = tValue;
			}
			else
			{
				throw new ArgumentException($"Key and Value must be of type {typeof(TKey)} and {typeof(TValue)} respectively.");
			}
		}
	}

	public bool Contains(object key) => (_dictionary as IDictionary).Contains(key);

	IDictionaryEnumerator IDictionary.GetEnumerator() => (_dictionary as IDictionary).GetEnumerator();

	public void Remove(object key)
	{
		(_dictionary as IDictionary).Remove(key);
		_config.SetValue(_jsonPath, _dictionary);
	}

	public bool IsFixedSize => (_dictionary as IDictionary).IsFixedSize;

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => (_dictionary as IEnumerable<KeyValuePair<TKey, TValue>>).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => (_dictionary as IEnumerable).GetEnumerator();

	public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
	
	public void Add(object key, object? value)
	{
		if (key is TKey tKey && value is TValue tValue)
		{
			Add(tKey, tValue);
		}
		else
		{
			throw new ArgumentException($"Key and Value must be of type {typeof(TKey)} and {typeof(TValue)} respectively.");
		}
	}

	void IDictionary.Clear()
	{
		_dictionary.Clear();
		_config.SetValue(_jsonPath, _dictionary);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
		_dictionary.Clear();
		_config.SetValue(_jsonPath, _dictionary);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);

	public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

	public void CopyTo(Array array, int index) => (_dictionary as ICollection).CopyTo(array, index);

	public int Count => _dictionary.Count;

	public bool IsSynchronized => (_dictionary as ICollection).IsSynchronized;

	public object SyncRoot => (_dictionary as ICollection).SyncRoot;

	public bool IsReadOnly => (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).IsReadOnly;

	
#pragma warning disable SYSLIB0050
	public void GetObjectData(SerializationInfo info, StreamingContext context) => (_dictionary as ISerializable).GetObjectData(info, context);
#pragma warning restore SYSLIB0050

	public void OnDeserialization(object? sender) => (_dictionary as IDeserializationCallback).OnDeserialization(sender);
}
