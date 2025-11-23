using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataTableTransactionCommitEvent : DataEvent<IDataTableTransactionCommitEventArgs>, IDataTableTransactionCommitEvent
{
    public DataTableTransactionCommitEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}