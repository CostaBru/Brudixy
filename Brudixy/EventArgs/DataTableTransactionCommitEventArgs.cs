using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy.EventArgs;

public class DataTableTransactionCommitEventArgs : IDataTableTransactionCommitEventArgs
{
    internal WeakReference<DataTable> Table;
    
    public DataTableTransactionCommitEventArgs(DataTable dataTable)
    {
        Table = new WeakReference<DataTable>(dataTable);
    }

    IDataTable IDataTableEventArgs.Table => Table.GetReferenceOrDefault();
}