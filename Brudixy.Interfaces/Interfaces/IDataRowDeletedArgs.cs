using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataRowDeletedArgs : IDataTableEventArgs
    {
        IEnumerable<IDataTableRow> DeletedRows { get; }
    }
}