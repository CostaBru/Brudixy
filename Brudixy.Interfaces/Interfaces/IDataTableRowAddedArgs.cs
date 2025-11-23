using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataTableRowAddedArgs : IDataTableEventArgs
    {
        IDataTableRow Row { get; }

        IEnumerable<IDataTableRow> Rows { get; }

        bool IsMultipleRow { get; }
    }
}