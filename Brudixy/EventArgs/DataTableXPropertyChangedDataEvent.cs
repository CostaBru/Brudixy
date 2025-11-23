using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataTableXPropertyChangedDataEvent : DataEvent<IDataTableXPropertyChangedArgs>, IDataTableXPropertyChangedDataEvent
{
    public DataTableXPropertyChangedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}