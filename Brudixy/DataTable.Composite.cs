using Brudixy.Interfaces;

namespace Brudixy;

public partial class DataTable
{
    IReadOnlyDataTable IReadOnlyDataTable.TryGetTable(string tableName) => (IDataTable)base.TryGetTable(tableName);

    IDataTable IDataTable.NewTable(string tableName = null) => (IDataTable)base.NewTable(tableName);

    IDataTable IDataTable.TryGetTable(string tableName) => (IDataTable)TryGetTable(tableName);
    
    IDataTable IDataTable.AddTable(string tableName) => (IDataTable)AddTable(tableName);

    public new DataTable NewTable(string tableName = null) => (DataTable)base.NewTable(tableName);
    
    public new DataTable AddTable(string tableName = null) => (DataTable)base.AddTable(tableName);

    public new DataTable TryGetTable(string tableName) => (DataTable)base.TryGetTable(tableName);

    IDataTable IDataTable.GetTable(string tableName) => (IDataTable)GetTable(tableName);

    IReadOnlyDataTable IReadOnlyDataTable.GetTable(string tableName) => (IReadOnlyDataTable)GetTable(tableName);

    IEnumerable<IDataTable> IDataTable.Tables => base.Tables.OfType<IDataTable>();
    
    IEnumerable<IReadOnlyDataTable> IReadOnlyDataTable.Tables => base.Tables.OfType<IReadOnlyDataTable>();
}