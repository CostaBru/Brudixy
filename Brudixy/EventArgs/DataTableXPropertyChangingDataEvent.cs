using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataTableXPropertyChangingDataEvent : DataEvent<IDataTableXPropertyChangingArgs>, IDataTableXPropertyChangingDataEvent
{
    public DataTableXPropertyChangingDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}