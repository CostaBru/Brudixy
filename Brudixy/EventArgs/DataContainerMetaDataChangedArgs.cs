using Brudixy.Interfaces;

namespace Brudixy.EventArgs;

public class DataContainerMetaDataChangedArgs : IDataContainerMetaDataChangedArgs
{
    private object m_value;

    public DataContainerMetaDataChangedArgs(IDataRowContainer rowContainer, string columnOrXProperty, string key, object value, RowMetadataType metadataType)
    {
        Row = rowContainer;
        m_value = value;
        Key = key;
        MetadataType = metadataType;
        ColumnOrXProperty = columnOrXProperty;
    }

    public IDataRowContainer Row { get; }
    public string Key { get;  }
    public RowMetadataType MetadataType { get;  }
    public string ColumnOrXProperty { get; }
      
    public T GetValue<T>()
    {
        return XPropertyValueConverter.TryConvert<T>("DataContainerMetaDataChangedArgs", ColumnOrXProperty, m_value);
    }
}