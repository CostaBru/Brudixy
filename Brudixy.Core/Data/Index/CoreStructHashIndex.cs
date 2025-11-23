using System;
using System.Collections.Generic;
using Brudixy.Constraints;
using Konsarpoo.Collections;

namespace Brudixy.Index
{
    internal class CoreStructHashIndex<T> : CoreHashIndex<T>, IIndexComparableStorageStructTyped<T> 
        where T : struct, IComparable
    {
        public CoreStructHashIndex()
        {
        }
        
        public CoreStructHashIndex(bool unique) : base(unique)
        {
        }

        public CoreStructHashIndex(bool unique, Map<T, int> storage) : base(unique, storage)
        {
        }
        
        public int Search(T? keyValue)
        {
            if (keyValue is null)
            {
                return m_nullReferences.Count == 0 ? -1 : m_nullReferences[0];
            }
            
            if (m_storage.TryGetValue(keyValue.Value, out var reference))
            {
                if (m_unique)
                {
                    return reference;
                }
                
                return reference < 0 ? reference : m_notUniqueReferences[reference][0];
            }

            return -1;
        }

        public IEnumerable<int> SearchRange(T? keyValue)
        {
            if (keyValue is null)
            {
                return m_nullReferences;
            }

            if (m_storage.TryGetValue(keyValue.Value, out var reference))
            {
                if (m_unique)
                {
                    return new[] {reference};
                }
                
                return reference < 0 ? Array.Empty<int>() : m_notUniqueReferences[reference];
            }

            return Array.Empty<int>();
        }
        
        public bool Update(T? value, int reference, T? oldValue)
        {
            if (value == null && m_unique)
            {
                throw new ConstraintException($"Cannot update key to the null in the unique index.");
            }
            
            if (oldValue is null)
            {
                return m_nullReferences.Remove(reference);
            }
            
            bool isRemoved = RemoveCore(reference, oldValue.Value);

            if(value.HasValue == false)
            {
                m_nullReferences.Add(reference);
            }
            else
            {
                Add(value.Value, reference);
            }

            return isRemoved;
        }
    }
}