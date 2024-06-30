namespace LDAP_Connector.Infrastructure
{
    public class SafeDictionary<TKey, TValue> where TKey : notnull
    {
        private IDictionary<TKey, TValue> dictionary;

        public SafeDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public IDictionary<TKey, TValue> All()
        {
            return dictionary;
        }

        public void Add(TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        public TValue Get(TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            else
            {
                return default;
            }
        }

        public void Remove(TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public bool Any()
        {
            return dictionary.Any();
        }

        public IEnumerable<TKey> GetKeys()
        {
            return dictionary.Keys;
        }
    }
}
