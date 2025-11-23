using System;
using System.Collections.Generic;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Index
{

    
    internal partial class RangeIndex<T>: IIndexStorage
        where T : IComparable<T>
    {
        private IntervalTree<T, int> m_intervalTree;
        private Data<int> m_nullReferences = new Data<int>();

        public RangeIndex()
        {
            m_intervalTree = new IntervalTree<T, int>();
        }

        public void Init(TableStorageType storageType)
        {
        }

        public int Search(Range<T> predicate)
        {
            if (predicate is null)
            {
                if (m_nullReferences.Count == 0)
                {
                    return -1;
                }

                return m_nullReferences[0];
            }
            
            if (m_intervalTree.TryGetInterval(predicate, out var reference))
            {
                return reference;
            }

            return -1;
        }

        public IEnumerable<int> SearchRange(Range<T> predicate)
        {
            if (predicate is null)
            {
                foreach (var reference in m_nullReferences)
                {
                    yield return reference;
                }
                
                yield break;
            }
            
            var values = m_intervalTree.GetIntervalsOverlappingWith(predicate);

            foreach (var reference in values)
            {
                yield return reference.Value;
            }
        }

        public void Add(Range<T> value, int reference)
        {
            if (value is null)
            {
                m_nullReferences.Add(reference);
                return;
            }
            
            if (Comparer<T>.Default.Compare(value.Min, value.Max) > 0)
            {
                throw new ArgumentOutOfRangeException($"Range index cannot contain negative range: {value}");
            }
            
            m_intervalTree.Add(value, reference);
        }

        public int Count => m_intervalTree.Count + m_nullReferences.Count;
        
        public bool Update(Range<T> value, int reference, Range<T> oldValue)
        {
            if (value != null && Comparer<T>.Default.Compare(value.Min, value.Max) > 0)
            {
                throw new ArgumentOutOfRangeException($"Range index cannot contain negative range: {value}");
            }
            
            var delete = false;
            
            if (oldValue == null)
            {
                delete = m_nullReferences.RemoveAll(reference) > 0;
            }
            else
            {
                delete = m_intervalTree.Delete(oldValue);
            }

            if (value is null)
            {
                m_nullReferences.Add(reference);
            }
            else
            {
                m_intervalTree.Add(value, reference);
            }

            return delete;
        }

        public void Clear()
        {
            m_nullReferences.Clear();
            m_intervalTree.Clear();
        }

        public RangeIndex<T> Copy()
        {
            var copy = (RangeIndex<T>)MemberwiseClone();

            copy.m_intervalTree = m_intervalTree.Clone(null);
            copy.m_nullReferences = new Data<int>(m_nullReferences);

            return copy;
        }

        IIndexStorage IIndexStorage.Copy() => Copy();
        
        IIndexStorage IIndexStorage.Clone() => Clone();

        public RangeIndex<T> Clone()
        {
            var clone = (RangeIndex<T>)this.MemberwiseClone();

            clone.m_intervalTree = new IntervalTree<T, int>();
            clone.m_nullReferences = new Data<int>();
            
            return clone;
        }

        public (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull) StorageType => CoreDataTable.GetColumnType(typeof(T));
        
        public bool Remove(Range<T> indexKey, int rowHandle)
        {
            if (indexKey == null)
            {
                return m_nullReferences.RemoveAll(rowHandle) > 0;
            }
            
            return m_intervalTree.Delete(indexKey);
        }

        public IEnumerable<(Range<T>, int)> GetComparableKeyValues()
        {
            foreach (var kv in m_intervalTree.GetItems())
            {
                yield return kv;
            }

            foreach (var reference in m_nullReferences)
            {
                yield return (null, reference);
            }
        }

        IEnumerable<(IComparable key, bool hasValue, int reference)> IIndexStorage.GetComparableKeyValues()
        {
            var valueTuples = m_intervalTree.GetItems();

            foreach (var reference in m_nullReferences)
            {
                yield return (null, false, reference);
            }
            
            foreach (var vt in valueTuples)
            {
                yield return (vt.range, true, vt.value);
            }
        }

        public bool IsUnique => false;
    }
}