using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Constraints;
using Konsarpoo.Collections;
using Akade.IndexedSet;

namespace Brudixy.Index
{
    internal interface IStringIndex : IIndexStorage
    {
        IEnumerable<int> StartsWith(string predicate);
        IEnumerable<int> EndsWith(string predicate);
        IEnumerable<int> Contains(string predicate);
        IEnumerable<int> FuzzyStartsWith(string predicate, int maxDistance);
        IEnumerable<int> FuzzyContains(string predicate, int maxDistance);
    }

    internal class StringIndex : IStringIndex
    {
        private readonly bool m_unique;
        private bool m_caseSensitive;

        // removed: Map<string, int> m_map; Map<int, Data<int>> m_notUniqueReferences
        private Data<int> m_nullReferences = new();

        // Akade-backed text indices for equality, prefix and contains
        private IndexedSet<StringEntry> m_textIndex;
        private Map<int, StringEntry> m_entriesByRef = new();
        private bool m_fullText;

        private sealed class StringEntry
        {
            public string Key { get; }
            public int Reference { get; set; }
            public StringEntry(string key, int reference)
            {
                Key = key;
                Reference = reference;
            }
        }

        public StringIndex(bool caseSensitive, bool unique, bool fulltext)
        {
            m_caseSensitive = caseSensitive;
            m_unique = unique;
            m_fullText = fulltext;
            m_textIndex = CreateTextIndex(caseSensitive, unique, fulltext);
        }

        private static StringComparer OrdinalIgnoreCase(bool caseSensitive)
        {
            return caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }

        private static IEqualityComparer<char> GetCharComparer(bool caseSensitive)
        {
            return caseSensitive ? EqualityComparer<char>.Default : Akade.IndexedSet.StringUtilities.CharEqualityComparer.OrdinalIgnoreCase;
        }

        private static IndexedSet<StringEntry> CreateTextIndex(bool caseSensitive, bool unique, bool fulltext)
        {
            var builder = IndexedSetBuilder<StringEntry>.Create();

            if (fulltext)
            {
                builder = builder.WithFullTextIndex(x => x.Key, GetCharComparer(caseSensitive), "Fulltext");
            }
            else
            {
                if (unique)
                {
                    builder = builder.WithUniqueIndex(x => x.Key, OrdinalIgnoreCase(caseSensitive), "Unique");
                }
                else
                {
                    builder = builder.WithIndex(x => x.Key, OrdinalIgnoreCase(caseSensitive), "Index");
                    
                    builder = builder.WithPrefixIndex(x => x.Key, GetCharComparer(caseSensitive), "Prefix");
                }
            }

            return builder.Build();
        }

        private StringComparison Comparison => m_caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        private void RebuildTextIndexFromCurrentData(bool caseSensitive, bool fullText)
        {
            m_textIndex = CreateTextIndex(caseSensitive, m_unique, fullText);
            m_entriesByRef.Clear();

            foreach (var (key, hasValue, reference) in GetAllKeysAndReferences())
            {
                if (!hasValue)
                {
                    continue; // nulls are not part of text indices
                }
                var entry = new StringEntry(key, reference);
                m_textIndex.Add(entry);
                m_entriesByRef[reference] = entry;
            }
        }

        public bool CaseSensitive
        {
            get => m_caseSensitive;
            set
            {
                if (m_caseSensitive != value)
                {
                    // snapshot
                    var temp = GetAllKeysAndReferences().ToData();

                    Clear();

                    m_caseSensitive = value;
                    m_textIndex = CreateTextIndex(value, m_unique, m_fullText);

                    foreach (var kv in temp)
                    {
                        Add(kv.key, kv.reference);
                    }

                    temp.Dispose();
                }
            }
        }
        
        public bool FullText
        {
            get => m_fullText;
            set
            {
                if (m_fullText != value)
                {
                    // snapshot
                    var temp = GetAllKeysAndReferences().ToData();

                    Clear();

                    m_fullText = value;
                    m_textIndex = CreateTextIndex(m_caseSensitive, m_unique, value);

                    foreach (var kv in temp)
                    {
                        Add(kv.key, kv.reference);
                    }

                    temp.Dispose();
                }
            }
        }

        public int Search(IComparable predicate)
        {
            var word = predicate as string;

            if (string.IsNullOrEmpty(word))
            {
                return m_nullReferences.Count == 0 ? -1 : m_nullReferences[0];
            }

            if (m_unique)
            {
                if (m_textIndex.TryGetSingle(x => x.Key, word, out var entry, "Unique"))
                {
                    return entry.Reference;
                }
                return -1;
            }

            var first = m_textIndex
                .Where(x => x.Key, word, "Index")
                .FirstOrDefault();
            
            return first == null ? -1 : first.Reference;
        }

        public IEnumerable<int> SearchRange(IComparable predicate)
        {
            var word = predicate as string;

            if (string.IsNullOrEmpty(word))
            {
                return m_nullReferences;
            }

            if (m_unique)
            {
                int single = Search(word);
                return new[] { single };
            }

            return m_textIndex.Where(x => x.Key, word, "Index").Select(e => e.Reference);
        }

        public IEnumerable<int> StartsWith(string predicate)
        {
            if (m_fullText)
            {
                foreach (var entry in m_textIndex.StartsWith(x => x.Key, predicate, "Fulltext"))
                {
                    yield return entry.Reference;
                }
            }
            else
            {
                if (m_unique)
                {
                    foreach (var entry in m_textIndex.FullScan())
                    {
                        if(entry.Key.StartsWith(predicate, Comparison))
                        {
                            yield return entry.Reference;
                        }
                    }
                }
                else
                {
                    foreach (var entry in m_textIndex.StartsWith(x => x.Key, predicate, "Prefix"))
                    {
                        yield return entry.Reference;
                    }
                }
            }
        }

        public IEnumerable<int> EndsWith(string predicate)
        {
            if (m_fullText)
            {
                foreach (var entry in m_textIndex.Contains(x => x.Key, predicate, "Fulltext"))
                {
                    if (entry.Key.EndsWith(predicate, Comparison))
                    {
                        yield return entry.Reference;
                    }
                }
            }
            else
            {
                    
                foreach (var entry in m_textIndex.FullScan())
                {
                    if (entry.Key.EndsWith(predicate, Comparison))
                    {
                        yield return entry.Reference;
                    }
                }
            }
        }

        public IEnumerable<int> Contains(string predicate)
        {
            if (m_fullText)
            {
                var stringEntries = m_textIndex.Contains(x => x.Key, predicate, "Fulltext").ToArray();
                
                foreach (var entry in stringEntries)
                {
                    yield return entry.Reference;
                }
            }
            else
            {
                foreach (var entry in m_textIndex.FullScan())
                {
                    if (entry.Key.Contains(predicate, Comparison))
                    {
                        yield return entry.Reference;
                    }
                }
            }
        }

        public IEnumerable<int> FuzzyStartsWith(string predicate, int maxDistance)
        {
            foreach (var entry in m_textIndex.FuzzyStartsWith(x => x.Key, predicate, maxDistance, "Fulltext"))
            {
                yield return entry.Reference;
            }
        }

        public IEnumerable<int> FuzzyContains(string predicate, int maxDistance)
        {
            foreach (var entry in m_textIndex.FuzzyContains(x => x.Key, predicate, maxDistance, "Fulltext"))
            {
                yield return entry.Reference;
            }
        }

        public void Add(IComparable value, int reference)
        {
            var word = value as string;

            if (string.IsNullOrEmpty(word))
            {
                if (m_unique)
                {
                    throw new ConstraintException("The null value can't be added to the unique index.");
                }

                m_nullReferences.Add(reference);
                Count++;
                return;
            }

            if (m_unique)
            {
                if (Search(value) >= 0)
                {
                    throw new ConstraintException($"The '{value}' is already exist in the index.");
                }
            }

            var entry = new StringEntry(word, reference);
            m_textIndex.Add(entry);
            m_entriesByRef[reference] = entry;

            Count++;
        }

        public int Count { get; private set; }

        public bool Update(IComparable value, int reference, IComparable oldValue)
        {
            var newKey = value as string;
            var oldKey = oldValue as string;

            if (m_unique)
            {
                if (string.IsNullOrEmpty(newKey))
                {
                    throw new ConstraintException("The null value can't be added to the unique index.");
                }

                if (string.Equals(newKey, oldKey, Comparison))
                {
                    // Same key, only reference change
                    if (m_entriesByRef.TryGetValue(reference, out var existingEntry))
                    {
                        existingEntry.Reference = reference;
                        return true;
                    }

                    // fallback: locate by key
                    if (m_textIndex.TryGetSingle(x => x.Key, newKey, out var entry, "Unique"))
                    {
                        entry.Reference = reference;
                        m_entriesByRef[reference] = entry;
                        return true;
                    }

                    return false;
                }
                else
                {
                    if (Search(value) >= 0)
                    {
                        throw new ConstraintException($"The '{value}' is already exist in the index.");
                    }
                }

                bool isRemoved = Remove(oldValue, reference);
                Add(value, reference);
                return isRemoved;
            }
            else
            {
                bool isRemoved = Remove(oldValue, reference);
                Add(value, reference);
                return isRemoved;
            }
        }

        public IComparable GetMaxNotNullValue(Func<int, bool> validCheck)
        {
            return null;
        }

        public IComparable GetMinNotNullValue(Func<int, bool> validCheck)
        {
            return null;
        }

        public IEnumerable<(string key, bool hasValue, int reference)> GetAllKeysAndReferences()
        {
            foreach (var nullReference in m_nullReferences)
            {
                yield return (string.Empty, false, nullReference);
            }

            foreach (var e in m_textIndex.FullScan())
            {
                yield return (e.Key, true, e.Reference);
            }
        }

        public void Clear()
        {
            m_textIndex?.Clear();
            m_entriesByRef.Clear();
            m_nullReferences.Clear();
            Count = 0;
        }

        public IIndexStorage Copy()
        {
            var stringIndex = new StringIndex(m_caseSensitive, m_unique, m_fullText);

            var all = GetAllKeysAndReferences();
            foreach (var tuple in all)
            {
                stringIndex.Add(tuple.key, tuple.reference);
            }

            return stringIndex;
        }

        public IIndexStorage Clone()
        {
            return new StringIndex(m_caseSensitive, m_unique, m_fullText);
        }

        public (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull) StorageType => (TableStorageType.String, TableStorageTypeModifier.Simple, true);

        public bool Remove(IComparable indexKey, int rowHandle)
        {
            var word = indexKey as string;

            if (string.IsNullOrEmpty(word))
            {
                var removed = m_nullReferences.Remove(rowHandle);
                if (removed)
                {
                    Count--;
                }
                return removed;
            }

            // Prefer reference-based removal
            if (m_entriesByRef.TryGetValue(rowHandle, out var entryByRef))
            {
                if (string.Equals(entryByRef.Key, word, Comparison))
                {
                    bool removedSet = m_textIndex.Remove(entryByRef);
                    if (removedSet)
                    {
                        m_entriesByRef.Remove(rowHandle);
                        Count--;
                        return true;
                    }
                }
            }

            // Fallback: locate via equality index
            if (m_unique)
            {
                if (m_textIndex.TryGetSingle(x => x.Key, word, out var single, "Unique"))
                {
                    if (single.Reference == rowHandle)
                    {
                        bool removedSet = m_textIndex.Remove(single);
                        if (removedSet)
                        {
                            m_entriesByRef.Remove(rowHandle);
                            Count--;
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                foreach (var candidate in m_textIndex.Where(x => x.Key, word, "Index"))
                {
                    if (candidate.Reference == rowHandle)
                    {
                        bool removedSet = m_textIndex.Remove(candidate);
                        if (removedSet)
                        {
                            m_entriesByRef.Remove(rowHandle);
                            Count--;
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public IReadOnlyList<int> CheckAllKeys(IIndexStorage storage, Func<int, bool> validCheck, Func<int, bool> storageValidCheck)
        {
            var errorReferences = new Data<int>();

            var typedIndex = (StringIndex)storage;

            foreach (var kv in typedIndex.GetComparableKeyValues())
            {
                if (kv.hasValue == false)
                {
                    continue;
                }

                if (storageValidCheck(kv.reference) == false)
                {
                    continue;
                }

                var range = this.SearchRange(kv.Item1);

                bool missingInIndex = true;

                foreach (var reference in range)
                {
                    if (validCheck(reference))
                    {
                        missingInIndex = false;
                        break;
                    }
                }

                if (missingInIndex)
                {
                    errorReferences.Add(kv.reference);
                }
            }

            return errorReferences;
        }

        public IEnumerable<(IComparable key, bool hasValue, int reference)> GetComparableKeyValues()
        {
            foreach (var kv in GetAllKeysAndReferences())
            {
                yield return (kv.key, kv.hasValue, kv.reference);
            }
        }

        public bool IsUnique => m_unique;

        public void Dispose()
        {
            Clear();
            m_nullReferences.Dispose();
            m_entriesByRef.Dispose();
        }
    }
}
