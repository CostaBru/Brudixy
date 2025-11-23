using System;
using System.Collections;
using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class IndexedDict<TKey, TValue> : IDictionary, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private Dictionary<TKey, TValue> m_dictionary = new Dictionary<TKey, TValue>();
        private List<TKey> m_order = new List<TKey>();
        private int m_version;

        public IndexedDict()
        {
        }

        public IndexedDict(IEqualityComparer<TKey> comparer)
        {
            m_dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public TValue this[TKey key]
        {
            get
            {
                var k = (TKey)key;

                return m_dictionary[k];
            }
            set
            {
                var k = (TKey)key;
                if (m_dictionary.ContainsKey(k))
                {
                    m_dictionary[k] = (TValue)value;
                }
                else
                {
                    m_dictionary[k] = (TValue)value;
                    m_order.Add(k);
                }
                
                m_version++;
            }
        }
        
        public bool ContainsKey(TKey key)
        {
            var k = (TKey)key;

            return m_dictionary.ContainsKey(k);
        }

        public TValue GetOrAdd(TKey key, TValue defaultV)
        {
            if(m_dictionary.TryGetValue(key, out var res))
            {
                return res;
            }

            m_dictionary[key] = defaultV;
            m_order.Add(key);
            
            return defaultV;
        }
        
        public TValue GetOrDefault(TKey key, TValue defaultV = default)
        {
            if(m_dictionary.TryGetValue(key, out var res))
            {
                return res;
            }
            
            return defaultV;
        }

        void IDictionary.Add(object key, object value)
        {
            var k = (TKey)key;
            if (m_dictionary.ContainsKey(k))
            {
                m_dictionary[k] = (TValue)value;
            }
            else
            {
                m_dictionary[k] = (TValue)value;
                m_order.Add(k);
            }

            m_version++;
        }

        public void Clear()
        {
            m_dictionary.Clear();
            m_order.Clear();
            
            m_version++;
        }

        bool IDictionary.Contains(object key)
        {
            var k = (TKey)key;

            return m_dictionary.ContainsKey(k);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new DictGenericEnum(this);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictEnum(this);
        }

        private class DictGenericEnum : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly IndexedDict<TKey, TValue> m_dictionary;
            private readonly int m_version;
            private int m_index;
            private KeyValuePair<TKey, TValue> m_current;

            internal DictGenericEnum(IndexedDict<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            public KeyValuePair<TKey, TValue> Current => m_current;

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return new KeyValuePair<TKey, TValue>(m_current.Key, m_current.Value);
                }
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                CheckVersion();

                for (var index = m_index; index < m_dictionary.m_order.Count;)
                {
                    var key = m_dictionary.m_order[index];
                    m_current = new KeyValuePair<TKey, TValue>(key, m_dictionary.m_dictionary[key]);
                    m_index++;
                    return true;
                }

                m_index = m_dictionary.Count + 1;
                m_current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"IndexedDict collection was modified during enumeration.");
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.Count + 1)))
                {
                    throw new InvalidOperationException("IndexedDict collection was modified during enumeration. ");
                }
            }

            public void Dispose()
            {
            }
        }
        
        private class DictEnum : IDictionaryEnumerator
        {
            private readonly IndexedDict<TKey, TValue> m_dictionary;
            private readonly int m_version;
            private int m_index;
            private DictionaryEntry m_current;

            internal DictEnum(IndexedDict<TKey, TValue> dictionary)
            {
                m_dictionary = dictionary;
                m_version = dictionary.m_version;
                m_index = 0;
                m_current = new DictionaryEntry();
            }

            public DictionaryEntry Current => m_current;

            object IEnumerator.Current
            {
                get
                {
                    CheckState();

                    return new DictionaryEntry(m_current.Key, m_current.Value);
                }
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                CheckVersion();

                for (var index = m_index; index < m_dictionary.m_order.Count;)
                {
                    var key = m_dictionary.m_order[index];
                    m_current = new DictionaryEntry(key, m_dictionary.m_dictionary[key]);
                    m_index++;
                    return true;
                }

                m_index = m_dictionary.Count + 1;
                m_current = new DictionaryEntry();
                return false;
            }

            void IEnumerator.Reset()
            {
                CheckVersion();

                m_index = 0;
                m_current = new DictionaryEntry();
            }

            private void CheckVersion()
            {
                if (m_version != m_dictionary.m_version)
                {
                    throw new InvalidOperationException($"IndexedDict collection was modified during enumeration.");
                }
            }

            private void CheckState()
            {
                if ((m_index == 0) || (m_index == (m_dictionary.Count + 1)))
                {
                    throw new InvalidOperationException("IndexedDict collection was modified during enumeration. ");
                }
            }

            public DictionaryEntry Entry => new DictionaryEntry(Current.Key, Current.Value);

            public object Key => Current.Key;

            public object Value => Current.Value;
        }

        void IDictionary.Remove(object key)
        {
            var k = (TKey)key;

            if (m_dictionary.ContainsKey(k))
            {
                m_dictionary.Remove(k);
                m_order.Remove(k);
                
                m_version++;
            }
        }

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        object IDictionary.this[object key]
        {
            get
            {
                var k = (TKey)key;

                return m_dictionary[k];
            }
            set
            {
                var k = (TKey)key;
                if (m_dictionary.ContainsKey(k))
                {
                    m_dictionary[k] = (TValue)value;
                }
                else
                {
                    m_dictionary[k] = (TValue)value;
                    m_order.Add(k);
                }
                
                m_version++;
            }
        }

        public IEnumerable<TKey> Keys => m_order;
        
        ICollection IDictionary.Keys => m_order;

        public ICollection Values => m_dictionary.Values;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DictEnum(this);
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count => m_dictionary.Count;

        public bool IsSynchronized => true;
        
        public object SyncRoot => m_dictionary;
    }
}