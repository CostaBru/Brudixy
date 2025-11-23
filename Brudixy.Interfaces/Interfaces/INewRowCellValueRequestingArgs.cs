namespace Brudixy.Interfaces
{
    public interface INewRowCellValueRequestingArgs : IDataTableEventArgs
    {
        string ColumnName { get; }

        object Value { get; set; }
    }
}