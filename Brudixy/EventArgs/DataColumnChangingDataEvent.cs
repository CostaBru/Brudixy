using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataColumnChangingDataEvent : DataEvent<IDataColumnChangingEventArgs>, IDataColumnChangingDataEvent
{
    public DataColumnChangingDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}