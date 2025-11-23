using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IChildRelationRowCollection<T> : IEnumerable<T> where T : ICoreDataRowReadOnlyAccessor
    {
        void Add(T item);
        void AddRange(IEnumerable<T> items);
        void Remove(T item);
        void RemoveRange(IEnumerable<T> items);
        void Clear();
    }
}