using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy.EventArgs;

public class DataTableTransactionRollbackEventArgs :  IDataTableTransactionRollbackEventArgs
{
    internal WeakReference<DataTable> Table;
    
    public DataTableTransactionRollbackEventArgs(DataTable dataTable)
    {
        Table = new WeakReference<DataTable>(dataTable);
    }

    IDataTable IDataTableEventArgs.Table => Table.GetReferenceOrDefault();
}