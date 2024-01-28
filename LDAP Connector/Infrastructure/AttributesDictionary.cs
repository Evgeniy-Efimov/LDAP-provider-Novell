namespace LDAP_Connector.Infrastructure
{
	public class AttributesDictionary<TKey, TValue> where TKey : notnull
	{
		private IDictionary<TKey, TValue> _dictionary;

		public AttributesDictionary()
		{
			_dictionary = new Dictionary<TKey, TValue>();
		}

		public AttributesDictionary(IDictionary<TKey, TValue> dictionary)
		{
			_dictionary = dictionary;
		}

		public IDictionary<TKey, TValue> GetAllAsCopy()
		{
			return _dictionary.ToDictionary(k => k.Key, v => v.Value);
		}

		public IDictionary<TKey, TValue> GetAllByRef()
		{
			return _dictionary;
		}

		public void SetValue(TKey key, TValue value)
		{
			if (_dictionary.ContainsKey(key))
			{
				_dictionary[key] = value;
			}
			else
			{
				_dictionary.Add(key, value);
			}
		}

		public object GetValue(TKey key)
		{
			if (_dictionary.ContainsKey(key))
			{
				return _dictionary[key];
			}
			else
			{
				return null;
			}
		}

		public void Remove(TKey key)
		{
			if (_dictionary.ContainsKey(key))
			{
				_dictionary.Remove(key);
			}
		}

		public void RemoveEmpty()
		{
			_dictionary = _dictionary.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);
		}

		public bool ContainsKey(TKey key)
		{
			return _dictionary.ContainsKey(key);
		}

		public bool Any()
		{
			return _dictionary.Any();
		}

		public IEnumerable<TKey> GetKeys()
		{
			return _dictionary.Keys;
		}
	}
}
