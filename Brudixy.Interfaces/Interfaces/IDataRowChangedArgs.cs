namespace Brudixy.Interfaces
{
    public interface IDataRowChangedArgs : IDataTableEventArgs
    {
        IDataTableRow Row { get; }
    }
}