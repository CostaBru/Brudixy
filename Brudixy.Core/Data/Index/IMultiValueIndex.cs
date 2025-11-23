using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.Index
{
    internal interface IMultiValueIndex
    {
        int KeysCount { get; }
        bool IsUnique { get; }
        
        bool Update([CanBeNull] IComparable[] newKey, int reference, [CanBeNull] IComparable[] oldKey);
        
        bool Remove([CanBeNull] IComparable[] key, int reference);
        
        int Count { get; }
        
        void Add([CanBeNull] IComparable[] values, int reference);
        
        int Search([CanBeNull] IComparable[] predicate);
        
        void Clear();
        
        IEnumerable<int> SearchRange([CanBeNull] IComparable[] predicate);
        
        IEnumerable<(object[] key, int reference)> GetKeyValues();

        Data<int> CheckAllKeys(IMultiValueIndex storage, Func<int, bool> validCheck, Func<int, bool> storageValidCheck);
        
        IMultiValueIndex Copy();
        
        IMultiValueIndex Clone();

        void Dispose();
    }
}