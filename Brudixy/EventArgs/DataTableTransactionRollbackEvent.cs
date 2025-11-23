using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataTableTransactionRollbackEvent : DataEvent<IDataTableTransactionRollbackEventArgs>, IDataTableTransactionRollbackEvent
{
    public DataTableTransactionRollbackEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}