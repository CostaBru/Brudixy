using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class RowXPropertyChangedDataEvent : DataEvent<IDataRowXPropertyChangedEventArgs>, IDataRowXPropertyChangedDataEvent
{
    public RowXPropertyChangedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}