using Brudixy.Interfaces;

namespace Brudixy.EventArgs;

public class DataTableXPropertyChangedArgs : IDataTableXPropertyChangedArgs
{
    private readonly WeakReference<DataTable> m_tableReference;

    private readonly object m_value;

    public DataTableXPropertyChangedArgs(DataTable table, string xPropertyName, object value)
    {
        m_tableReference = new WeakReference<DataTable>(table);

        XPropertyName = xPropertyName;

        m_value = value;
    }
    
    public IDataTable Table
    {
        get
        {
            if (m_tableReference.TryGetTarget(out var tbl))
            {
                return tbl;
            }

            return null;
        }
    }

    public string XPropertyName { get; }
    
    public T GetValue<T>()
    {
        if (m_value is T tv)
        {
            return tv;
        }

        var type = m_value?.GetType();

        TableStorageType? tableStorageType = null;
        TableStorageTypeModifier? tableStorageTypeModifier = null;

        if (type != null)
        {
            var storageType = CoreDataTable.GetColumnType(type);
            
            tableStorageType = storageType.type;
            tableStorageTypeModifier = storageType.typeModifier;
        }

        return Brudixy.TypeConvertor.ConvertValue<T>(m_value, XPropertyName, Table.TableName, tableStorageType, tableStorageTypeModifier,"DataTableXPropertyChanged");
    }
}