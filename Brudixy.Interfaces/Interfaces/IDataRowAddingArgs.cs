namespace Brudixy.Interfaces
{
    public interface IDataRowAddingArgs : IDataTableEventArgs
    {
        bool IsCancel { get; set; }
    }
}