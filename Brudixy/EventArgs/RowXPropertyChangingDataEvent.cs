using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class RowXPropertyChangingDataEvent : DataEvent<IDataRowXPropertyChangingEventArgs>, IDataRowXPropertyChangingDataEvent
{
    public RowXPropertyChangingDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}