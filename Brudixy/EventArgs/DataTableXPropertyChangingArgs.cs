using Brudixy.Interfaces;

namespace Brudixy.EventArgs;

public class DataTableXPropertyChangingArgs : IDataTableXPropertyChangingArgs
{
    private readonly WeakReference<DataTable> m_tableReference;

    private object m_value;
    private readonly object m_prevValue;

    public DataTableXPropertyChangingArgs(DataTable table, string xPropertyName, object value, object prevValue)
    {
        m_tableReference = new WeakReference<DataTable>(table);

        XPropertyName = xPropertyName;

        m_value = value;

        m_prevValue = prevValue;
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
    
    public void SetNewValue(object value)
    {
        m_value = value;
    }

    public T GetOldValue<T>()
    {
        if (m_prevValue is T tv)
        {
            return tv;
        }
        
        var type = m_prevValue?.GetType();

        TableStorageType? tableStorageType = null;
        TableStorageTypeModifier? tableStorageTypeModifier = null;

        if (type != null)
        {
            var storageType = CoreDataTable.GetColumnType(type);
            
            tableStorageType = storageType.type;
            tableStorageTypeModifier = storageType.typeModifier;
        }

        return Brudixy.TypeConvertor.ConvertValue<T>(m_prevValue, XPropertyName, Table.TableName, tableStorageType, tableStorageTypeModifier,"DataTableXPropertyChanging GetOldValue");
    }

    public bool Cancel { get; set; }

    public T GetNewValue<T>()
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

        return Brudixy.TypeConvertor.ConvertValue<T>(m_value, XPropertyName, Table.TableName, tableStorageType, tableStorageTypeModifier,"DataTableXPropertyChanging GetNewValue");
    }
}