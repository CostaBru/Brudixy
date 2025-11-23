using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataColumnChangedEventArgs : IDataTableEventArgs
    {
        bool IsColumnChanged(string columnName);

        object GetNewValue(string columnName);

        object GetOldValue(string columnName);

        IDataTableRow Row { get; }

        IEnumerable<string> ChangedColumnNames { get; }
    }
}