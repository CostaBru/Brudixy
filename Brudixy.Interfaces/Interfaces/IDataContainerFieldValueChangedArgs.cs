using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataContainerFieldValueChangedArgs 
    {
        bool IsColumnChanged(string columnName);

        object GetNewValue(string columnName);

        object GetOldValue(string columnName);

        IDataRowContainer Row { get; }

        IEnumerable<string> ChangedColumnNames { get; }
    }
}