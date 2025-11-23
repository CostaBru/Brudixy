using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataColumnCollection<T> : IEnumerable<T> where T : ICoreTableReadOnlyColumn
    {
        IDataColumnCollection<T> Add(T item);
        IDataColumnCollection<T> AddRange(IEnumerable<T> items);
        void Remove(T item);
        void RemoveRange(IEnumerable<T> items);
        void Clear();
    }
}