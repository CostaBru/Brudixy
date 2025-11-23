using System;
using System.Collections.Generic;
using Brudixy.Interfaces;

namespace Brudixy.Index
{
    internal partial class RangeIndex<T> 
    {
        public void Dispose()
        {
        }

        public int Search(IComparable predicate)
        {
            return Search((Range<T>)predicate);
        }

        public IEnumerable<int> SearchRange(IComparable predicate)
        {
            return SearchRange((Range<T>)predicate);
        }

        public void Add(IComparable value, int reference)
        {
            Add((Range<T>)value, reference);
        }

        public bool Update(IComparable value, int reference, IComparable oldValue)
        {
            return Update((Range<T>)value, reference, (Range<T>)oldValue);
        }

        public IComparable GetMaxNotNullValue(Func<int, bool> validCheck)
        {
            return  m_intervalTree.GetMax().range;
        }

        public IComparable GetMinNotNullValue(Func<int, bool> validCheck)
        {
            return  m_intervalTree.GetMin().range;
        }

        public void Union(IIndexStorage dirtyIndex)
        {
        }

        public bool Remove(IComparable indexKey, int rowHandle)
        {
            return Remove((Range<T>)indexKey, rowHandle);
        }

        public IReadOnlyList<int> CheckAllKeys(IIndexStorage storage, Func<int, bool> validCheck, Func<int, bool> storageValidCheck)
        {
            throw new NotImplementedException();
        }

        public void AddRemovedItem(IComparable oldValue, int reference)
        {
        }

        public IEnumerable<int> SearchAll(IComparable predicate)
        {
            return SearchRange((Range<T>)predicate);
        }
    }
}