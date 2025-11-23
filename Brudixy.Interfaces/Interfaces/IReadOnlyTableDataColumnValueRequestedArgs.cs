namespace Brudixy.Interfaces
{
    public interface IReadOnlyTableDataColumnValueRequestedArgs
    {
        IReadOnlyDataTable Table { get; }

        IDataTableReadOnlyRow Row { get; }

        string ColumnName { get; }

        object Value { get; set; }

        bool IsOverriden { get; set; }
    }
}