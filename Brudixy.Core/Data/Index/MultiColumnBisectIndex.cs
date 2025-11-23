using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brudixy.Constraints;
using Konsarpoo.Collections;

namespace Brudixy.Index
{
    internal sealed class MultiColumnBisectIndex : IMultiValueIndex
    {
        [DebuggerDisplay("{ValuesString} - {Ref}")]
        public class IndexKey
        {
            public IComparable[] Values;
            public int Ref;

            public string ValuesString
            {
                get { return ValueToStringTableLess(Values); }
            }

            public static bool operator ==(IndexKey a, IndexKey b)
            {
                var null1 = ReferenceEquals(a, null);
                var null2 = ReferenceEquals(b, null);

                if (null1 && null2)
                {
                    return true;
                }

                if (null1 || null2)
                {
                    return false;
                }

                if (!a.Values.SequenceEqual(b.Values) || a.Ref != b.Ref)
                {
                    return false;
                }

                return true;
            }

            public static bool operator !=(IndexKey a, IndexKey b)
            {
                var null1 = ReferenceEquals(a, null);
                var null2 = ReferenceEquals(b, null);

                if (null1 && null2)
                {
                    return false;
                }

                if (null1 || null2)
                {
                    return true;
                }

                if (a.Values.SequenceEqual(b.Values) && a.Ref == b.Ref )
                {
                    return false;
                }

                return true;
            }
            
            public override bool Equals(object obj)
            {
                var x = this;
                var y = obj as IndexKey;
                
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
               
                if (ReferenceEquals(y, null))
                {
                    return false;
                }
              
                if (x.Values.Length == 0 && y.Values.Length == 0)
                {
                    return true;
                }

                return x.Values.SequenceEqual(y.Values);
            }
            
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 314159;

                    foreach (var value in Values)
                    {
                        hashCode ^= value?.GetHashCode() ?? 3;
                    }

                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"{nameof(Ref)}: {Ref}, {nameof(ValuesString)}: {ValuesString}";
            }
        }

        public MultiColumnBisectIndex(int capacity, int keyCount, bool unique) : this( keyCount, new Data<IndexKey>(capacity), new Data<int>(), unique)
        {
        }

        public MultiColumnBisectIndex(int keyCount, Data<IndexKey> sortedList, Data<int> nullReferencesList, bool unique)
        {
            m_keyCount = keyCount;

            m_notNullValueList = sortedList;

            m_nullReferencesList = nullReferencesList;
            
            m_unique = unique;
        }
        
        public bool IsUnique => m_unique;

        public IMultiValueIndex Clone()
        {
            return new MultiColumnBisectIndex(m_keyCount, new Data<IndexKey>(), new Data<int>(), m_unique);
        }

        public void Dispose()
        {
            Clear();
            
            m_notNullValueList?.Dispose();
            m_nullReferencesList?.Dispose();
        }

        public int KeysCount => m_keyCount;

        public IMultiValueIndex Copy()
        {
            return new MultiColumnBisectIndex(m_keyCount, new Data<IndexKey>(m_notNullValueList), new Data<int>(m_nullReferencesList), m_unique);
        }

        internal Data<IndexKey> m_notNullValueList;
        
        internal readonly Data<int> m_nullReferencesList;

        private readonly bool m_unique;

        private readonly int m_keyCount;

        internal static string ValueToString(IReadOnlyList<IComparable> values, IReadOnlyList<int> columnHandles,
            CoreDataTable table)
        {
            return string.Join(";",
                values.Select((c, i) =>
                {
                    if (c is null)
                    {
                        return "NULL";
                    }

                    var coreDataColumn = table.DataColumnInfo.Columns[columnHandles[i]];

                    return CoreDataTable.ConvertObjectToString(coreDataColumn.Type, coreDataColumn.TypeModifier, c);
                }));
        }

        internal static string ValueToStringTableLess(IReadOnlyList<IComparable> values)
        {
            return string.Join(";", values.Select(c =>
            {
                if (c is null)
                {
                    return "NULL";
                }

                var type = c.GetType();

                var columnType = CoreDataTable.GetColumnType(type);
                
                return CoreDataTable.ConvertObjectToString(columnType.type, columnType.typeModifier, c, type);
            }));
        }

        public bool Update(IComparable[] newKey, int reference, IComparable[] oldKey)
        {
            if (m_unique)
            {
                CheckAddParamValid(newKey, reference, m_keyCount);
                CheckRemoveParamValid(reference, oldKey, m_keyCount);
                
                var existingRef = BinarySearch(m_notNullValueList, newKey);

                if (newKey.SequenceEqual(oldKey))
                {
                    m_notNullValueList[existingRef].Ref = reference;

                    return true;
                }
                
                if (existingRef >= 0)
                {
                    throw new ConstraintException($"The '{ValueToStringTableLess(newKey)}' already contains in the unique multi value index.");
                }

                bool updated = Remove(oldKey, reference);

                Add(newKey, reference);

                return updated;
            }
            else
            {
                bool updated = Remove(oldKey, reference);

                Add(newKey, reference);

                return updated;
            }
        }

        internal static void CheckRemoveParamValid(int reference, IComparable[] oldValues, int keyCount)
        {
            if (oldValues == null)
            {
                throw new ArgumentNullException($"Cannot remove null key from unique multi column index for row {reference}.");
            }

            if (oldValues.Length != keyCount)
            {
                throw new ArgumentNullException(
                    $"Cannot remove partial key as reference to row {reference} to unique multi column index.");
            }
        }

        internal static void CheckAddParamValid(IComparable[] values, int reference, int keyCount)
        {
            if (values == null)
            {
                throw new ArgumentNullException(
                    $"Cannot add null value as reference to row {reference} to unique multi column index.");
            }

            if (values.Length != keyCount)
            {
                throw new ArgumentNullException(
                    $"Cannot add partial key as reference to row {reference} to unique multi column index.");
            }
        }

        public bool Remove(IComparable[] key, int reference)
        {
            if (m_unique)
            {
                CheckRemoveParamValid(reference, key, m_keyCount);
            }
            
            if (key == null)
            {
                return m_nullReferencesList.RemoveAll(reference) > 0;
            }

            var indexValues = SearchRangeCore(key).ToData();

            indexValues.Reverse();

            bool any = false;

            foreach (var indexValue in indexValues)
            {
                if (indexValue.reference == reference)
                {
                    m_notNullValueList.RemoveAt(indexValue.index);
                    any = true;
                }
            }

            return any;
        }
        
        public IEnumerable<int> SearchRange(IComparable[] value)
        {
            foreach (var tuple in SearchRangeCore(value))
            {
                yield return tuple.reference;
            }
        }

        private IEnumerable<(int index, int reference)> SearchRangeCore(IComparable[] value)
        {
            var startIndex = BinarySearchLeft(m_notNullValueList, value);

            var startReference = -1;
            var rightIndex = -1;

            var end = m_notNullValueList.Count - 1;

            if (startIndex >= 0)
            {
                startReference = m_notNullValueList.ValueByRef(startIndex).Ref;

                if (m_unique && value.Length == m_keyCount)
                {
                    yield return (startIndex, startReference);

                    yield break;
                }

                rightIndex = BinarySearchRight(m_notNullValueList, value, ref startIndex, ref end);

                if (rightIndex < 0 || rightIndex >= m_notNullValueList.Count)
                {
                    rightIndex = startIndex;
                }
            }
            else
            {
                yield break;
            }

            var readyIndexCount = Count;

            for (int i = startIndex; i <= rightIndex && i < readyIndexCount; i++)
            {
                yield return (i,m_notNullValueList.ValueByRef(i).Ref);
            }
        }

        class SequenceComparer : IEqualityComparer<IReadOnlyList<IComparable>>
        {
            public bool Equals(IReadOnlyList<IComparable> x, IReadOnlyList<IComparable> y)
            {
                if (x.Count == 0 && y.Count == 0)
                {
                    return true;
                }

                return x.SequenceEqual(y);
            }

            public int GetHashCode(IReadOnlyList<IComparable> obj)
            {
                return 0;
            }
        }
        
        private bool CheckUnique(IComparable[] values)
        {
            return BinarySearch(m_notNullValueList, values) < 0;
        }

        public int Count
        {
            get { return m_notNullValueList.Count + m_nullReferencesList.Count; }
        }

        public void Add(IComparable[] values, int reference)
        {
            if (m_unique)
            {
                CheckAddParamValid(values, reference, m_keyCount);
                
                AddNotNull(values, reference);
            }
            else
            {
                if (values == null)
                {
                    m_nullReferencesList.Add(reference);
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
                if (CheckUnique(values) == false)
                {
                    throw new ConstraintException(
                        $"Cannot add the '{ValueToStringTableLess(values)}' already contains in the unique multi value index.");
                }
            }

            AddNoCheck(values, reference);
        }

        private void AddNoCheck(IComparable[] values, int reference)
        {
            int num = BinarySearch(m_notNullValueList, values);

            if (num >= 0)
            {
                if (num >= m_notNullValueList.Count - 1)
                {
                    m_notNullValueList.Add(new IndexKey { Values = values, Ref = reference });
                }
                else
                {
                    m_notNullValueList.Insert(num, new IndexKey { Values = values, Ref = reference });
                }
            }
            else
            {
                m_notNullValueList.Insert(~num, new IndexKey { Values = values, Ref = reference });
            }
        }

        public int Search(IComparable[] predicate)
        {
            if (predicate == null)
            {
                if (m_nullReferencesList.Count > 0)
                {
                    return m_nullReferencesList[0];
                }

                return -1;
            }

            var index = BinarySearch(m_notNullValueList, predicate);

            var reference = -1;

            if (index >= 0)
            {
                reference = m_notNullValueList.ValueByRef(index).Ref;
            }

            return reference;
        }

        internal int BinarySearch(Data<IndexKey> list, IComparable[] predicate)
        {
            var startIndex = 0;
            var endIndex = list.Count - 1;

            return BinarySearch(list, predicate, ref startIndex, ref endIndex);
        }

        public void Clear()
        {
            m_notNullValueList.Clear();
            m_nullReferencesList.Clear();
        }

        internal int BinarySearch(Data<IndexKey> list, IComparable[] predicate, ref int startIndex, ref int endIndex)
        {
            int lo = startIndex;
            int hi = endIndex;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = Compare(list[index].Values, predicate);

                if (comp == 0)
                {
                    return index;
                }

                if (comp > 0)
                {
                    hi = index - 1;
                }
                else
                {
                    lo = index + 1;
                }
            }
            return ~lo;
        }

        internal int BinarySearchLeft(Data<IndexKey> list, IComparable[] predicate)
        {
            var startIndex = 0;
            var endIndex = list.Count - 1;

            return BinarySearchLeft(list, predicate, ref startIndex, ref endIndex);
        }

        internal static int BinarySearchLeft(Data<IndexKey> list, 
            IComparable[] predicate,
            ref int startIndex, 
            ref int endIndex)
        {
            //common case
            int lo = startIndex;
            int hi = endIndex;
            int res = -1;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = Compare(list[index].Values, predicate);
                
                if (comp > 0)
                {
                    hi = index - 1;
                }
                else if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    res = index;
                    hi = index - 1;
                }
            }

            return res;
        }

        internal static int BinarySearchRight(Data<IndexKey> list,    
            IComparable[] predicate,
            ref int startIndex, 
            ref int endIndex)
        {
            //common case
            int lo = startIndex;
            int hi = endIndex;
            int res = -1;

            while (lo <= hi)
            {
                int index = lo + (hi - lo >> 1);

                var comp = Compare(list[index].Values, predicate);
                
                if (comp > 0)
                {
                    hi = index - 1;
                }
                else if (comp < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    res = index;
                    lo = index + 1;
                }
            }

            return res;
        }

        public static int Compare(IComparable[] x, IComparable[] y)
        {
            bool all = false;

            for (int i = 0; i < x.Length && i < y.Length; i++)
            {
                if (x[i] is null && y[i] is null)
                {
                    continue;
                }

                if (y[i] is null)
                {
                    return 1;
                }
                
                var compareTo = x[i]?.CompareTo(y[i]) ?? -1;

                all = compareTo == 0;

                if (all)
                {
                    continue;
                }

                return compareTo;
            }

            if (all)
            {
                return 0;
            }

            return -1;
        }

        public IEnumerable<(object[] key, int reference)> GetKeyValues()
        {
            foreach (var kv in m_notNullValueList)
            {
                yield return (kv.Values, kv.Ref);
            }
        }
        
        public Data<int> CheckAllKeys(IMultiValueIndex storage, Func<int, bool> validCheck,  Func<int, bool> storageValidCheck)
        {
            var errors = new Data<int>();

            foreach (var keyValue in storage.GetKeyValues())
            {
                if (storageValidCheck(keyValue.Item2) == false)
                {
                    continue;
                }

                var items = SearchRangeCore(TypeConvertor.AsComparable(keyValue.Item1)).ToData();                       

                if (items.Count == 0)
                {
                    errors.Add(keyValue.Item2);
                    continue;
                }

                bool missingInIndex = true;

                var cnt = m_notNullValueList.Count;

                foreach (var item in items)
                {
                    if (validCheck(item.reference) && m_notNullValueList.ValueByRef(item.index).Values.SequenceEqual(keyValue.Item1))
                    {
                        missingInIndex = false;
                        break;
                    }
                }

                if (missingInIndex)
                {
                    errors.Add(keyValue.Item2);
                }
            }

            return errors;
        }
    }
}



