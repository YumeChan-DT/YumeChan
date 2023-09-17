using System.Collections;
using YumeChan.Core.Services.Config;
namespace YumeChan.Core.Infrastructure.Dynamic.Config;

/// <summary>
/// Provides a JSON-Config backed List of <see cref="T"/>.
/// </summary>
internal sealed class DynamicJsonList<T> : IList<T>, IList, IReadOnlyList<T>
{
	private readonly List<T> _list;
	private readonly JsonWritableConfig _config;
	private readonly string _jsonPath;

	public DynamicJsonList(JsonWritableConfig config, string jsonPath, IEnumerable<T>? collection = null)
	{
		_list = collection is null ? new() : new(collection);
		_config = config;
		_jsonPath = jsonPath;
	}
	
	public T this[int index]
	{
		get => _list[index];
		
		set
		{
			_list[index] = value;
			_config.SetValue(_jsonPath, _list);
		}
	}

	public void CopyTo(Array array, int index) => (_list as IList).CopyTo(array, index);

	public int Count => _list.Count;
	public bool IsSynchronized => (_list as ICollection).IsSynchronized;
	public object SyncRoot => (_list as ICollection).SyncRoot;

	public bool IsReadOnly => false;

	object? IList.this[int index]
	{
		get => this[index];
		set => this[index] = (T)value!;
	}

	public void Add(T item)
	{
		_list.Add(item);
		_config.SetValue(_jsonPath, _list);
	}

	public int Add(object? value)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		
		Add((T)value);
		return _list.Count - 1;
	}

	public void Clear()
	{
		_list.Clear();
		_config.SetValue(_jsonPath, _list);
	}

	public bool Contains(object? value) => (_list as IList).Contains(value);

	public int IndexOf(object? value) => (_list as IList).IndexOf(value);

	public void Insert(int index, object? value)
	{
		if (IsCompatibleObject(value))
		{
			Insert(index, (T)value!);
		}
		else
		{
			throw new ArgumentException("Specified value is not a compatible object.", nameof(value));
		}
	}



	public void Remove(object? value)
	{
		if (IsCompatibleObject(value))
		{
			Remove((T)value!);
		}
		else
		{
			throw new ArgumentException("Specified value is not a compatible object.", nameof(value));
		}
	}

	public bool Contains(T item) => _list.Contains(item);
	
	public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
	
	public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
	
	public int IndexOf(T item) => _list.IndexOf(item);
	
	public void Insert(int index, T item)
	{
		_list.Insert(index, item);
		_config.SetValue(_jsonPath, _list);
	}
	
	public bool Remove(T item)
	{
		bool result = _list.Remove(item);
		_config.SetValue(_jsonPath, _list);
		return result;
	}
	
	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
		_config.SetValue(_jsonPath, _list);
	}

	public bool IsFixedSize => (_list as IList).IsFixedSize;

	IEnumerator IEnumerable.GetEnumerator() => (_list as IEnumerable).GetEnumerator();

	// Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
    // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
	private static bool IsCompatibleObject(object? value) => value is T || (value is null && default(T) is null);
}