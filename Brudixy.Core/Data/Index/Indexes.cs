using System;
using System.Collections.Generic;
using Brudixy.Constraints;
using Brudixy.Index;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal class Indexes
    {
        [NotNull] public IIndexStorage ReadyIndex;

        public Indexes(int columnHandle, IIndexStorage indexStorage)
        {
            ColumnHandle = columnHandle;

            ReadyIndex = indexStorage;
        }

        public int ColumnHandle;

        [NotNull]
        public IIndexComparableStorageStructTyped<T> GetReadyStructIndexTyped<T>() where T : struct, IComparable
        {
            return (IIndexComparableStorageStructTyped<T>)ReadyIndex;
        }

        public bool UpdateIndexHandle(IComparable oldValue, IComparable newValue, int reference)
        {
            var storageTyped = ReadyIndex;

            var skipAdding = newValue == null && storageTyped.IsUnique;

            if (skipAdding)
            {
                return storageTyped.Remove(oldValue, reference);
            }

            return storageTyped.Update(newValue, reference, oldValue);
        }

        public int GetRowHandle(IComparable key)
        {
            return ReadyIndex.Search(key);
        }
        
        public int GetRowHandleStruct<T>(ref T key) where T : struct, IComparable
        {
            return GetReadyStructIndexTyped<T>().Search(key);
        }

        public Data<int> GetRowHandles(IComparable key)
        {
            var result = new Data<int>();

            var index = ReadyIndex;

            if (index.IsUnique)
            {
                var searchResult = index.Search(key);

                if (searchResult >= 0)
                {
                    result.Add(searchResult);
                }

                return result;
            }

            return index.SearchRange(key).ToData();
        }
        
        public Data<int> GetRowHandlesStruct<T>(ref T key) where T : struct, IComparable
        {
            var result = new Data<int>();

            var index = GetReadyStructIndexTyped<T>();

            if (index.IsUnique)
            {
                var searchResult = index.Search(key);

                result.Add(searchResult);

                return result;
            }

            return index.SearchRange(key).ToData();
        }

        public void AddIndex(IComparable genericIndexKey, int reference)
        {
            if (ReadyIndex.IsUnique)
            {
                var searchResult = ReadyIndex.Search(genericIndexKey);

                if (searchResult >= 0)
                {
                    throw new ConstraintException(
                        $"Can't add '{genericIndexKey}' value to the index. Such value is already exists.");
                }
            }

            ReadyIndex.Add(genericIndexKey, reference);
        }

        public void AddIndexStruct<T>(ref T? genericIndexKey, int reference) where T : struct, IComparable
        {
            if (genericIndexKey.HasValue)
            {
                var indexTyped = GetReadyStructIndexTyped<T>();

                indexTyped.Add(genericIndexKey.Value, reference);
            }
            else
            {
                ReadyIndex.Add(null, reference);
            }
        }

        public Indexes Clone(bool withData)
        {
            if (withData)
            {
                return new Indexes(ColumnHandle, ReadyIndex.Copy());
            }

            return new Indexes(ColumnHandle, ReadyIndex.Clone());
        }

        public void ClearValues()
        {
            ReadyIndex.Clear();
        }
        
        public void RemoveIndex(int rowHandle, IComparable key)
        {
            if (ReadyIndex.IsUnique && key == null)
            {
                return;
            }
            
            ReadyIndex.Remove(key, rowHandle);
        }

        public IEnumerable<int> GetRowHandlesByStringIndex(string predicate, CoreDataTable.StringIndexLookupType type)
        {
            switch (type)
            {
                case CoreDataTable.StringIndexLookupType.StartsWith: return ((IStringIndex)ReadyIndex).StartsWith(predicate);
                case CoreDataTable.StringIndexLookupType.EndsWith: return ((IStringIndex)ReadyIndex).EndsWith(predicate);
                case CoreDataTable.StringIndexLookupType.Contains: return ((IStringIndex)ReadyIndex).Contains(predicate);
            }
            return ((IStringIndex)ReadyIndex).SearchRange(predicate).ToData();
        }
    }
}
