using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Brudixy.Interfaces.Tools
{
    public sealed class StringValueStore<V> : IReadOnlyCollection<string>
    {
        private FrozenDictionary<string, V> m_dict;

        public StringValueStore(params (string key, V)[] items) : this(StringComparer.OrdinalIgnoreCase, items)
        {
        }
        
        public StringValueStore(StringComparer comparer, params (string key, V)[] items) 
        {
            m_dict = items.ToFrozenDictionary(i => i.key, i => i.Item2, comparer);
        }

        public bool TryGetValue(string key, out V value)
        {
            return m_dict.TryGetValue(key, out value);
        }
        
        public V this[string key] => m_dict[key];
        
        public IEnumerator<string> GetEnumerator()
        {
            foreach (var key in m_dict.Keys)
            {
                yield return key;
            }
        }
        
        public bool Contains(string item)
        {
            return m_dict.ContainsKey(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => m_dict.Count;
    }
}