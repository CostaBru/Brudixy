using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataRowTransactionRollbackArgs : IDataTableEventArgs
    {
        IDataTableRow Row { get; }
        
        IEnumerable<IDataTableColumn> RejectedColumns { get; }
    }
}