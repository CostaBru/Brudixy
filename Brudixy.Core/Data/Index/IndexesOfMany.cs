using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brudixy.Index;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    
    
    internal class IndexesOfMany
    {
        public bool IsUnique;
        public bool HashIndex;

        [NotNull] public IMultiValueIndex ReadyIndex;

        public IndexesOfMany(int[] columns, IMultiValueIndex indexStorage)
        {
            Columns = columns;

            ReadyIndex = indexStorage;
        }

        public int[] Columns;

        public bool UpdateIndexHandle([CanBeNull] IComparable[] oldValue, [CanBeNull] IComparable[] newValue, int reference)
        {
            var storageTyped = ReadyIndex;

            if (oldValue?.Any(a => a is null) ?? false)
            {
                oldValue = null;
            }
            
            if (newValue?.Any(a => a is null) ?? false)
            {
                newValue = null;
            }

            var skipAdding = newValue == null && storageTyped.IsUnique;
            var skipRemoving = oldValue == null && storageTyped.IsUnique;

            if (skipAdding)
            {
                if (skipRemoving)
                {
                    return false;
                }
                
                return storageTyped.Remove(oldValue, reference);
            }

            if (skipRemoving)
            {
                storageTyped.Add(newValue, reference);

                return true;
            }

            return storageTyped.Update(newValue, reference, oldValue);
        }

        public int GetRowHandle(CoreDataTable table, IComparable[] key)
        {
            if (IsUnique && key.Length == ReadyIndex.KeysCount)
            {
                var rowHandle = ReadyIndex.Search(key);

                return rowHandle;
            }

            foreach (var rowHandle in GetRowHandles(key))
            {
                if (table.StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    return rowHandle;
                }
            }

            return -1;
        }

        public Data<int> GetRowHandles(IComparable[] key)
        {
            var result = new Data<int>();

            if (IsUnique && key.Length == ReadyIndex.KeysCount)
            {
                var item = ReadyIndex.Search(key);
                
                if (item >= 0)
                {
                    result.Add(item);
                }

                return result;
            }

            result.AddRange(ReadyIndex.SearchRange(key));

            return result;
        }

        public void AddIndex(IComparable[] genericIndexKey, int reference)
        {
            ReadyIndex.Add(genericIndexKey, reference);
        }

        public IndexesOfMany Clone(bool withData)
        {
            if (withData)
            {
                return new IndexesOfMany(Columns, ReadyIndex.Copy()) { IsUnique = IsUnique, HashIndex = HashIndex};
            }

            return new IndexesOfMany(Columns, ReadyIndex.Clone()) { IsUnique = IsUnique, HashIndex = HashIndex };
        }

        public void ClearValues()
        {
            ReadyIndex.Clear();
        }

        public void RemoveIndex(int rowHandle, IComparable[] key)
        {
            if (key == null && ReadyIndex.IsUnique)
            {
                return;
            }

            ReadyIndex.Remove(key, rowHandle);
        }
    }
}
