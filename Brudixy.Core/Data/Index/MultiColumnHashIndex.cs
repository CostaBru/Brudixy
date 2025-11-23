using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Akade.IndexedSet;
using Brudixy.Constraints;
using Konsarpoo.Collections;

namespace Brudixy.Index
{
    // Helper element stored inside the indexed set
    [DebuggerDisplay("Ref: {Ref}, Values: {ValuesString}")]
    internal readonly struct RowElement : IEquatable<RowElement>
    {
        public readonly int Ref;
        public readonly IComparable[] Values;

        public RowElement(int @ref, IComparable[] values)
        {
            Ref = @ref;
            Values = values;
        }

        public string ValuesString => MultiColumnBisectIndex.ValueToStringTableLess(Values);

        // Equality by Ref only so HashSet-based storage treats the same row handle as the same element
        public bool Equals(RowElement other) => Ref == other.Ref;
        public override bool Equals(object obj) => obj is RowElement re && Equals(re);
        public override int GetHashCode() => Ref.GetHashCode();
    }

    // Full key wrapper used for unique index comparisons
    internal readonly struct FullKey
    {
        public readonly IComparable[] Values;
        public FullKey(IComparable[] values) => Values = values;
        public override string ToString() => MultiColumnBisectIndex.ValueToStringTableLess(Values);
    }

    internal sealed class FullKeyComparer : IEqualityComparer<FullKey>
    {
        public bool Equals(FullKey x, FullKey y)
        {
            var xv = x.Values; var yv = y.Values;
            if (ReferenceEquals(xv, yv)) return true;
            if (xv is null || yv is null) return false;
            if (xv.Length != yv.Length) return false;
            for (int i = 0; i < xv.Length; i++)
            {
                if (!Equals(xv[i], yv[i])) return false;
            }
            return true;
        }

        public int GetHashCode(FullKey obj)
        {
            unchecked
            {
                int hash = 314159;
                var vals = obj.Values;
                if (vals != null)
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        hash = (hash * 397) ^ (vals[i]?.GetHashCode() ?? 3);
                    }
                    hash = (hash * 397) ^ vals.Length;
                }
                return hash;
            }
        }
    }

    // Prefix key wrapper to support prefix lookups of composite keys
    internal readonly struct KeyPrefix
    {
        public readonly IComparable[] Values;
        public readonly int Length;
        public KeyPrefix(IComparable[] values, int length)
        {
            Values = values ?? Array.Empty<IComparable>();
            Length = length;
        }
        public override string ToString()
        {
            var slice = new IComparable[Length];
            Array.Copy(Values, slice, Math.Min(Length, Values.Length));
            return MultiColumnBisectIndex.ValueToStringTableLess(slice);
        }
    }

    internal sealed class KeyPrefixComparer : IEqualityComparer<KeyPrefix>
    {
        public bool Equals(KeyPrefix x, KeyPrefix y)
        {
            if (x.Length != y.Length) return false;
            // Compare up to Length
            int len = x.Length;
            for (int i = 0; i < len; i++)
            {
                if (!Equals(x.Values[i], y.Values[i])) return false;
            }
            return true;
        }

        public int GetHashCode(KeyPrefix obj)
        {
            unchecked
            {
                int hash = 271828;
                var vals = obj.Values;
                int len = Math.Min(obj.Length, vals?.Length ?? 0);
                for (int i = 0; i < len; i++)
                {
                    hash = (hash * 397) ^ (vals[i]?.GetHashCode() ?? 3);
                }
                hash = (hash * 397) ^ obj.Length;
                return hash;
            }
        }
    }

    internal class MultiColumnHashIndex : IMultiValueIndex
    {
        private const string PrimaryKeyIndexName = "__RowRefPrimaryKey";
        private const string FullKeyIndexName = "__FullKeyIndex";
        private const string PrefixIndexName = "__PrefixIndex";

        private readonly bool m_unique;
        private readonly int m_keyCount;

        // Backing store
        private IndexedSet<int, RowElement> m_set;
        // Track current elements by reference for removals and enumeration
        private Map<int, RowElement> m_refToElement = new();
        private Data<int> m_nullReferencesList = new();

        public MultiColumnHashIndex(bool unique, int keyCount)
        {
            m_unique = unique;
            m_keyCount = keyCount;

            // Build the indexed set with deterministic index names
            var builder = IndexedSetBuilder.Create<int, RowElement>(Array.Empty<RowElement>(), x => x.Ref, PrimaryKeyIndexName);

            if (m_unique)
            {
                builder = builder
                    .WithUniqueIndex(x => new FullKey(x.Values), new FullKeyComparer(), FullKeyIndexName)
                    .WithIndex(x => GetAllPrefixes(x), new KeyPrefixComparer(), PrefixIndexName);
            }
            else
            {
                builder = builder
                    .WithIndex(x => GetAllPrefixes(x), new KeyPrefixComparer(), PrefixIndexName);
            }

            m_set = builder.Build();
        }

        // Helper to yield prefix keys for an element
        private IEnumerable<KeyPrefix> GetAllPrefixes(RowElement e)
        {
            var values = e.Values;
            if (values == null || values.Length == 0)
            {
                yield break;
            }

            // Prefixes of length 1..m_keyCount (including full key as prefix)
            int max = Math.Min(m_keyCount, values.Length);
            for (int i = 1; i <= max; i++)
            {
                yield return new KeyPrefix(values, i);
            }
        }

        public bool IsUnique => m_unique;
        public int KeysCount => m_keyCount;

        public int Count { get; private set; }

        public int Search(IComparable[] predicate)
        {
            if (predicate is null)
            {
                return m_nullReferencesList.Count > 0 ? m_nullReferencesList[0] : -1;
            }

            if (predicate.Length != m_keyCount)
            {
                return -1;
            }

            if (m_unique)
            {
                RowElement element;
                if (m_set.TryGetSingle(x => new FullKey(x.Values), new FullKey(predicate), out element, indexName: FullKeyIndexName))
                {
                    return element.Ref;
                }
                return -1;
            }
            else
            {
                // Non-unique: return first match for the full key via prefix index (length == keyCount)
                return m_set.Where(x => GetAllPrefixes(x), new KeyPrefix(predicate, m_keyCount), indexName: PrefixIndexName)
                            .Select(el => el.Ref)
                            .FirstOrDefault(-1);
            }
        }

        public IEnumerable<int> SearchRange(IComparable[] predicate)
        {
            if (predicate is null)
            {
                return m_nullReferencesList;
            }

            if (predicate.Length == m_keyCount)
            {
                if (m_unique)
                {
                    RowElement el;
                    if (m_set.TryGetSingle(x => new FullKey(x.Values), new FullKey(predicate), out el, indexName: FullKeyIndexName))
                    {
                        return new[] { el.Ref };
                    }
                    return Array.Empty<int>();
                }
                else
                {
                    return m_set.Where(x => GetAllPrefixes(x), new KeyPrefix(predicate, m_keyCount), indexName: PrefixIndexName)
                                .Select(el => el.Ref)
                                .ToArray();
                }
            }
            else
            {
                // Prefix search using non-unique prefix index
                int len = predicate.Length;
                return m_set.Where(x => GetAllPrefixes(x), new KeyPrefix(predicate, len), indexName: PrefixIndexName)
                            .Select(el => el.Ref)
                            .ToArray();
            }
        }

        public void Add(IComparable[] values, int reference)
        {
            if (m_unique)
            {
                MultiColumnBisectIndex.CheckAddParamValid(values, reference, m_keyCount);
                AddNotNull(values, reference);
            }
            else
            {
                if (values is null)
                {
                    m_nullReferencesList.Add(reference);
                    Count++;
                }
                else
                {
                    AddNotNull(values, reference);
                }
            }
        }

        private void AddNotNull(IComparable[] values, int reference)
        {
            if (m_unique)
            {
                // check if exists
                if (Search(values) >= 0)
                {
                    throw new ConstraintException($"The '{MultiColumnBisectIndex.ValueToStringTableLess(values)}' is already exist in the index.");
                }
            }

            // Insert element
            var element = new RowElement(reference, values);
            m_set.Add(element);
            m_refToElement[reference] = element;
            Count++;
        }

        public bool Update(IComparable[] newKey, int reference, IComparable[] oldKey)
        {
            if (m_unique)
            {
                MultiColumnBisectIndex.CheckAddParamValid(newKey, reference, m_keyCount);
                MultiColumnBisectIndex.CheckRemoveParamValid(reference, oldKey, m_keyCount);

                if (newKey.SequenceEqual(oldKey))
                {
                    // No key change: nothing to update within indices since PK is the same; but ensure element present
                    if (m_refToElement.TryGetValue(reference, out var existing))
                    {
                        // overwrite stored values if needed
                        if (!ReferenceEquals(existing.Values, newKey) && !existing.Values.SequenceEqual(newKey))
                        {
                            // remove and re-add so prefix index stays correct
                            m_set.Remove(existing);
                            var updatedElement = new RowElement(reference, newKey);
                            m_set.Add(updatedElement);
                            m_refToElement[reference] = updatedElement;
                        }
                    }
                    else
                    {
                        // Add fresh
                        AddNotNull(newKey, reference);
                    }
                    return true;
                }

                var existingRef = Search(newKey);
                if (existingRef >= 0)
                {
                    throw new ConstraintException($"The '{MultiColumnBisectIndex.ValueToStringTableLess(newKey)}' already contains in the unique multi value index.");
                }

                bool wasUpdated = Remove(oldKey, reference);
                Add(newKey, reference);
                return wasUpdated;
            }
            else
            {
                bool wasUpdated = Remove(oldKey, reference);
                Add(newKey, reference);
                return wasUpdated;
            }
        }

        public bool Remove(IComparable[] key, int reference)
        {
            if (key is null)
            {
                var removed = m_nullReferencesList.Remove(reference);
                if (removed)
                {
                    Count--;
                }
                return removed;
            }

            // Try remove the current element tracked for the reference, but ensure keys match
            if (m_refToElement.TryGetValue(reference, out var element))
            {
                if (!element.Values.SequenceEqual(key))
                {
                    return false;
                }

                bool removed = m_set.Remove(element);
                if (removed)
                {
                    m_refToElement.Remove(reference);
                    Count--;
                }
                return removed;
            }

            // Nothing to remove
            return false;
        }

        public void Clear()
        {
            m_set.Clear();
            m_refToElement.Clear();
            m_nullReferencesList.Clear();
            Count = 0;
        }

        public IEnumerable<(object[] key, int reference)> GetKeyValues()
        {
            foreach (var nullRef in m_nullReferencesList)
            {
                yield return (default, nullRef);
            }

            foreach (var kv in m_refToElement)
            {
                var el = kv.Value;
                // avoid covariant array assignment by copying into object[]
                var keyCopy = new object[el.Values.Length];
                for (int i = 0; i < el.Values.Length; i++) keyCopy[i] = el.Values[i];
                yield return (keyCopy, el.Ref);
            }
        }

        public Data<int> CheckAllKeys(IMultiValueIndex storage, Func<int, bool> validCheck, Func<int, bool> storageValidCheck)
        {
            var errorReferences = new Data<int>();
            foreach (var kv in storage.GetKeyValues())
            {
                if (!storageValidCheck(kv.reference))
                {
                    continue;
                }

                var matches = SearchRange(TypeConvertor.AsComparable(kv.key)).ToData();
                if (matches.Count == 0)
                {
                    errorReferences.Add(kv.reference);
                    continue;
                }

                bool ok = false;
                foreach (var r in matches)
                {
                    if (validCheck(r))
                    {
                        ok = true;
                        break;
                    }
                }
                if (!ok)
                {
                    errorReferences.Add(kv.reference);
                }
            }
            return errorReferences;
        }

        public IMultiValueIndex Copy()
        {
            var copy = CloneCore();
            foreach (var (key, reference) in GetKeyValues())
            {
                copy.AddCoreNoCheck(key, reference);
            }
            return copy;
        }

        public IMultiValueIndex Clone()
        {
            return CloneCore();
        }

        private MultiColumnHashIndex CloneCore()
        {
            var index = new MultiColumnHashIndex(m_unique, m_keyCount);
            return index;
        }

        private void AddCoreNoCheck(object[] keyValue, int reference)
        {
            if (keyValue is null)
            {
                m_nullReferencesList.Add(reference);
                Count++;
                return;
            }

            // Convert to IComparable[] safely instead of array-cast
            var comparable = new IComparable[keyValue.Length];
            for (int i = 0; i < keyValue.Length; i++) comparable[i] = (IComparable)keyValue[i];

            var element = new RowElement(reference, comparable);
            m_set.Add(element);
            m_refToElement[reference] = element;
            Count++;
        }

        public void Dispose()
        {
            Clear();
            m_refToElement.Dispose();
            m_nullReferencesList.Dispose();
        }
    }
}