using System;
using System.Collections.Generic;

namespace Brudixy.Index
{
    internal interface IIndexStorage : IDisposable
    {
        int Search(IComparable predicate);

        IEnumerable<int> SearchRange(IComparable predicate);

        void Add(IComparable value, int reference);

        int Count { get; }

        bool Update(IComparable value, int reference, IComparable oldValue);

        IComparable GetMaxNotNullValue(Func<int, bool> validCheck);

        IComparable GetMinNotNullValue(Func<int, bool> validCheck);

        void Clear();

        IIndexStorage Copy();

        IIndexStorage Clone();

        (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull) StorageType { get; }

        bool Remove(IComparable indexKey, int rowHandle);

        IReadOnlyList<int> CheckAllKeys(IIndexStorage storage, Func<int, bool> validCheck, Func<int, bool> storageValidCheck);
        
        IEnumerable<(IComparable key, bool hasValue, int reference)> GetComparableKeyValues();
        
        bool IsUnique { get; }
    }

    internal interface IIndexComparableStorageTyped<T> : IIndexStorage where T: IComparable
    {
        IEnumerable<(T key, bool hasValue, int reference)> GetKeyValues();
    }

    internal interface IIndexComparableStorageStructTyped<T> : IIndexStorage where T : struct, IComparable
    {
        int Search(T? predicate);
        
        IEnumerable<int> SearchRange(T? predicate);

        bool Update(T? value, int reference, T? oldValue);
        
        void Add(T value, int reference);
    }

    public struct SearchResult
    {
        public int InnerIndex;

        public int Reference;
    }
}