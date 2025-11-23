using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataTableRowEnumerableReadOnly<out T> : 
        IEnumerable<T> where T : IDataTableReadOnlyRow
    {
        IDataTableRowEnumerableToConcrete<T> Where(string column);
    
    }
    
    public interface IDataTableRowEnumerable<out T> :  IDataTableRowEnumerableReadOnly<T>
         where T : IDataTableReadOnlyRow
    {
        IDataTableRowEnumerable<T> Add(IDataRowReadOnlyAccessor newRow);
        
        IDataTableRowEnumerable<T> AddRange(IEnumerable<IDataRowReadOnlyAccessor> newRows);
    }
}
