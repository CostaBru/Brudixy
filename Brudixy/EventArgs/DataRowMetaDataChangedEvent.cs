using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs;

public class DataRowMetaDataChangedEvent : DataEvent<IDataRowMetaDataChangedArgs>, IDataRowMetaDataChangedEvent
{
    public DataRowMetaDataChangedEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
    {
    }
}