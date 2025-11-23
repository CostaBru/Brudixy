using System;
using System.Collections.Generic;
using Brudixy.Constraints;
using Konsarpoo.Collections;

namespace Brudixy.Index
{
    internal interface IHashIndexInit
    {
        void Init(bool unique);
    }
    
    internal class CoreHashIndex<T> : IIndexStorage,  IIndexComparableStorageTyped<T>, IHashIndexInit
        where T : IComparable
    {
        protected bool m_unique;

        protected Map<T, int> m_storage;
        protected Map<int, Data<int>> m_notUniqueReferences = new();
        protected Data<int> m_nullReferences = new();
        
        public int Count { get; private set; }

        public CoreHashIndex()
        {
            m_storage = new Map<T, int>();
        }

        public CoreHashIndex(bool unique)
        {
            m_unique = unique;
            m_storage = new Map<T, int>();
        }
        
        public CoreHashIndex(bool unique, Map<T, int> storage)
        {
            m_unique = unique;
            m_storage = storage;
        }

        void IHashIndexInit.Init(bool unique)
        {
            m_unique = unique;
        }
        
        public (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull) StorageType => CoreDataTable.GetColumnType(typeof(T));
       
        public int Search(IComparable predicate)
        {
            if (predicate is null)
            {
                return m_nullReferences.Count == 0 ? -1 : m_nullReferences[0];
            }

            T keyValue;

            if (predicate is T tv)
            {
                keyValue = tv;
            }
            else
            {
                var storageType = StorageType;
                
                keyValue = Brudixy.TypeConvertor.ConvertValue<T>(predicate, string.Empty, string.Empty, storageType.type, storageType.typeModifier,  "Index search");
            }

            if (m_storage.TryGetValue(keyValue, out var storeReferencePointer))
            {
                if (m_unique)
                {
                    return storeReferencePointer;
                }
                
                return storeReferencePointer < 0 ? storeReferencePointer : m_notUniqueReferences[storeReferencePointer][0];
            }

            return -1;
        }

        public IEnumerable<int> SearchRange(IComparable predicate)
        {
            if (predicate is null)
            {
                return m_nullReferences;
            }

            T keyValue;

            if (predicate is T tv)
            {
                keyValue = tv;
            }
            else
            {
                var storageType = StorageType;
                
                keyValue = Brudixy.TypeConvertor.ConvertValue<T>(predicate, string.Empty, string.Empty, storageType.type, storageType.typeModifier, "Index SearchRange");
            }

            if (m_storage.TryGetValue(keyValue, out var storeReferencePointer))
            {
                if (m_unique)
                {
                    return new[] {storeReferencePointer};
                }
                
                return storeReferencePointer < 0 ? Array.Empty<int>() : m_notUniqueReferences[storeReferencePointer];
            }

            return Array.Empty<int>();
        }

        public void Add(IComparable value, int reference)
        {
            if (value is null)
            {
                if (m_unique)
                {
                    throw new ConstraintException($"Cannot add null value to the unique index.");
                }

                m_nullReferences.Add(reference);

                Count++;
                return;
            }
            
            T keyValue;

            if (value is T tv)
            {
                keyValue = tv;
            }
            else
            {
                var storageType = StorageType;
                
                keyValue = Brudixy.TypeConvertor.ConvertValue<T>(value, string.Empty, string.Empty, storageType.type, storageType.typeModifier, "Index Add");
            }
            
            AddCore(keyValue, reference);
        }

        private void AddCore(T keyValue, int reference)
        {
            if (m_unique)
            {
                if (Search(keyValue) >= 0)
                {
                    throw new ConstraintException($"The '{keyValue}' is already exist in the index.");
                }
            }

            if (m_unique)
            {
                var storeReferencePointer = reference;
                m_storage[keyValue] = storeReferencePointer;
            }
            else
            {
                if (m_storage.TryGetValue(keyValue, out var storeReferencePointer))
                {
                    m_notUniqueReferences
                        .GetOrAdd(storeReferencePointer, () => new Data<int>())
                        .Add(reference);
                }
                else
                {
                    storeReferencePointer = reference;
                    
                    m_storage[keyValue] = storeReferencePointer;

                    m_notUniqueReferences
                        .GetOrAdd(storeReferencePointer, () => new Data<int>())
                        .Add(reference);
                }
            }

            Count++;
        }

        public bool Update(IComparable value, int reference, IComparable oldValue)
        {
            if(value is null && m_unique)
            {
                throw new ConstraintException($"Cannot update key to the null in the unique index.");
            }
            
            bool isRemoved = Remove(oldValue, reference);

            Add(value, reference);

            return isRemoved;
        }

        public IComparable GetMaxNotNullValue(Func<int, bool> validCheck)
        {
            return default;
        }

        public IComparable GetMinNotNullValue(Func<int, bool> validCheck)
        {
            return default;
        }

     
        public void Clear()
        {
            m_storage.Clear();

            foreach (var kv in m_notUniqueReferences)
            {
                kv.Value.Dispose();
            }
            m_notUniqueReferences.Clear();
            m_nullReferences.Clear();
            
            Count = 0;
        }

        public IIndexStorage Copy()
        {
            var index = CloneCore();

            var allKeysAndReferences = GetKeyValues();

            foreach (var tuple in allKeysAndReferences)
            {
                if (tuple.hasValue)
                {
                    index.Add(tuple.key, tuple.reference);
                }
                else
                {
                    index.AddNull(tuple.reference);
                }
                
            }

            return index;
        }

        private CoreHashIndex<T> CloneCore()
        {
            var index = (CoreHashIndex<T>)this.MemberwiseClone();
            index.m_notUniqueReferences = new Map<int, Data<int>>();
            index.m_nullReferences = new Data<int>();
            index.m_storage = new Map<T, int>();
            index.Count = 0;
            return index;
        }

        public IIndexStorage Clone()
        {
            return CloneCore();
        }

        public bool Remove(IComparable indexKey, int rowHandle)
        {
            if (indexKey is null)
            {
                var remove = m_nullReferences.Remove(rowHandle);
                
                if(remove)
                {
                    Count--;
                }
                
                return remove;
            }

            T keyValue;

            if (indexKey is T tv)
            {
                keyValue = tv;
            }
            else
            {
                var storageType = StorageType;
                
                keyValue = Brudixy.TypeConvertor.ConvertValue<T>(indexKey, string.Empty, string.Empty, storageType.type, storageType.typeModifier, "Index Remove");
            }

            return RemoveCore(rowHandle, keyValue); 
        }

        protected bool RemoveCore(int rowHandle, T keyValue)
        {
            if (m_storage.TryGetValue(keyValue, out var storeReferencePointer))
            {
                if (m_unique)
                {
                    m_storage.Remove(keyValue);

                    Count--;

                    return true;
                }

                if (m_notUniqueReferences.TryGetValue(storeReferencePointer, out var referenceList))
                {
                    if (referenceList.Remove(rowHandle))
                    {
                        Count--;

                        if (referenceList.Count == 0)
                        {
                            m_storage.Remove(keyValue);
                        }
                        else
                        {
                            if (rowHandle == storeReferencePointer)
                            {
                                m_notUniqueReferences.Remove(storeReferencePointer);

                                var newReferencePointer = referenceList[0];

                                m_notUniqueReferences[newReferencePointer] = referenceList;

                                m_storage[keyValue] = newReferencePointer;
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public IReadOnlyList<int> CheckAllKeys(IIndexStorage storage, Func<int, bool> validCheck, Func<int, bool> storageValidCheck)
        {
            var errorReferences = new Data<int>();

            var typedIndex = (IIndexComparableStorageTyped<T>)storage;

            foreach (var kv in typedIndex.GetComparableKeyValues())
            {
                if (kv.hasValue == false)
                {
                    continue;
                }
                
                if(storageValidCheck(kv.reference) == false) 
                { 
                    continue;
                }

                var range = this.SearchRange(kv.Item1);  
                       
                bool missingInIndex = true;                      

                foreach(var reference in range)
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
            foreach (var kv in GetKeyValues())
            {
                yield return (kv.key, kv.hasValue, kv.reference);
            }
        }
        
        public IEnumerable<(T key, bool hasValue, int reference)> GetKeyValues()
        {
            foreach (var nullReference in m_nullReferences)
            {
                yield return (default, false, nullReference);
            }

            foreach (var kv in m_storage)
            {
                var storeReferencePointer = kv.Value;
                
                if (m_unique)
                {
                    yield return (kv.Key, true, storeReferencePointer);
                }
                else
                {
                    var references = m_notUniqueReferences[storeReferencePointer];

                    foreach (var reference in references)
                    {
                        yield return (kv.Key, true, reference);
                    }
                }
            }
        }

        public bool IsUnique => m_unique;

        public void Dispose()
        {
            Clear();
            
            m_storage.Dispose();
            m_nullReferences.Dispose();
            m_notUniqueReferences.Dispose();
        }

       
        public void Add(T value, int reference)
        {
            AddCore(value, reference);
        }
        
        public void AddNull(int reference)
        {
           m_nullReferences.Add(reference);
           Count++;
        }
    }
}