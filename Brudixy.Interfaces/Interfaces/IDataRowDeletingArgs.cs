using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataRowDeletingArgs : IDataTableEventArgs
    {
        IEnumerable<IDataTableRow> Rows { get; }

        bool IsCancel { get; set; }
    }
}