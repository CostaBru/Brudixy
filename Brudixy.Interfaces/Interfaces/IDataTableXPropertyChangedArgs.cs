namespace Brudixy.Interfaces
{
    public interface IDataTableXPropertyChangedArgs : IDataTableEventArgs
    {
        string XPropertyName { get; }

        T GetValue<T>();
    }
    
    public interface IDataTableXPropertyChangingArgs : IDataTableEventArgs
    {
        string XPropertyName { get; }

        T GetNewValue<T>();

        void SetNewValue(object value);
        
        T GetOldValue<T>();
        
        bool Cancel { get; set; }
    }
    
    public enum RowMetadataType
    {
        Row,
        Column,
        XProperty
    }

    public interface IDataRowMetaDataChangedArgs : IDataTableEventArgs
    {
        public IDataTableReadOnlyRow Row { get; }
        
        public string Key { get; }
        
        T GetValue<T>();
        
        string ColumnOrXProperty { get; }
        
        RowMetadataType MetadataType { get; }
    }
}