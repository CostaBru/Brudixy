using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy.EventArgs;



public class DataRowMetaDataChangedArgs : IDataRowMetaDataChangedArgs
{
    public int RowHandle { get; }

    internal WeakReference<DataTable> Table;
    
    private readonly object m_value;

    public DataRowMetaDataChangedArgs(DataTable dataTable, int rowHandle, string columnOrXProperty, string key, object value, RowMetadataType metadataType)
    {
        Table = new WeakReference<DataTable>(dataTable);
        RowHandle = rowHandle;
        Key = key;
        m_value = value;
        ColumnOrXProperty = columnOrXProperty;
        MetadataType = metadataType;
    }

    IDataTable IDataTableEventArgs.Table => Table.GetReferenceOrDefault();

    public IDataTableReadOnlyRow Row => Table.GetReferenceOrDefault().GetRowByHandle(RowHandle);

    public string Key { get; }
    
    public string ColumnOrXProperty { get; }
    
    public RowMetadataType MetadataType { get; }
    
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

        return TypeConvertor.ConvertValue<T>(m_value, ColumnOrXProperty, Table.GetReferenceOrDefault()?.TableName, tableStorageType, tableStorageTypeModifier,"DataRowMetaDataChanged");
    }
}