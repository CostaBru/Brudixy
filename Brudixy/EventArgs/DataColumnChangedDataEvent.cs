using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataColumnChangedDataEvent : DataEvent<IDataColumnChangedEventArgs>, IDataColumnChangedDataEvent
{
    public DataColumnChangedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}