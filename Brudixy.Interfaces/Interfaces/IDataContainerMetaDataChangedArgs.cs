namespace Brudixy.Interfaces
{
    public interface IDataContainerMetaDataChangedArgs
    {
        IDataRowContainer Row { get; }
        string Key { get; }
        string ColumnOrXProperty { get; }
        RowMetadataType MetadataType { get; }
        T GetValue<T>();
    }
}