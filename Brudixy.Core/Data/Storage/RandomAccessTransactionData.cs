using System;
using System.Collections;
using System.Collections.Generic;
using Konsarpoo.Collections;
using Konsarpoo.Collections.Persistence;

namespace Brudixy
{
    public partial class RandomAccessTransactionData<T, TChange> : IRandomAccessTransactionData<T, TChange>, IDisposable, IFileData
    {
        private IRandomAccessData<T> m_data;
        
        protected IRandomAccessData<T> Data => m_data ??= CreateStorage();

        protected virtual IRandomAccessData<T> CreateStorage()
        {
            return new Data<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Data).GetEnumerator();
        }

        public void Add(T item)
        {
            Data.Add(item);
        }

        void IRandomAccessData<T>.Clear()
        {
            Data.Clear();
        }

        public void Sort()
        {
            Data.Sort();
        }

        public void Sort(IComparer<T> comparer)
        {
            Data.Sort(comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            Data.Sort(comparison);
        }

        public int BinarySearch(T value, int startIndex, int count, IComparer<T> comparer = null)
        {
            return Data.BinarySearch(value, startIndex, count, comparer);
        }

        public void AddRange(IEnumerable<T> items)
        {
            Data.AddRange(items);
        }

        void ICollection<T>.Clear()
        {
            Data.Clear();
        }

        public bool Contains(T item)
        {
            return Data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Data.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return Data.Remove(item);
        }

        public void Append(T value)
        {
            Data.Append(value);
        }

        public int Count => Data.Count;

        public bool IsReadOnly => Data.IsReadOnly;

        int IList<T>.IndexOf(T item)
        {
            return Data.IndexOf(item);
        }

        void IRandomAccessData<T>.Insert(int index, T item)
        {
            Data.Insert(index, item);
        }

        void IRandomAccessData<T>.RemoveAt(int index)
        {
            Data.RemoveAt(index);
        }

        public void Ensure(int size)
        {
            Data.Ensure(size);
        }

        public void Ensure(int size, T defaultValue)
        {
            Data.Ensure(size, defaultValue);
        }

        int IRandomAccessData<T>.IndexOf(T item)
        {
            return Data.IndexOf(item);
        }

        void IList<T>.Insert(int index, T item)
        {
            Data.Insert(index, item);
        }

        void IList<T>.RemoveAt(int index)
        {
            Data.RemoveAt(index);
        }

        public T this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public void BeginWrite()
        {
            if (Data is FileData<T> fd)
            {
                fd.BeginWrite();
            }
        }

        public void Flush()
        {
            if (Data is FileData<T> fd)
            {
                fd.Flush();
            }
        }
        
        public void EndWrite()
        {
            if (Data is FileData<T> fd)
            {
                fd.EndWrite();
            }
        }
    }
}