using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataTableDisposedDataEvent : DataEvent<IDataTableDisposedEventArgs>, IDataTableDisposedDataEvent
    {
        public DataTableDisposedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class DataTableDisposedEventArgs : System.EventArgs, IDataTableDisposedEventArgs
    {
        public string TableName { get; set; }
    }
}
