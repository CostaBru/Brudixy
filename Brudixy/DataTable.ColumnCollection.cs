using Brudixy.Interfaces;

namespace Brudixy;

public partial class DataTable : IDataColumnCollection<IDataTableColumn>
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public IDataColumnCollection<IDataTableColumn> Columns => this;
    
    public new IEnumerable<DataColumn> GetColumns()
    {
        var columnsCount = DataColumnInfo.ColumnsCount;

        for (int i = 0; i < columnsCount; i++)
        {
            yield return GetDataColumnInstance(i);
        }
    }

    protected override CoreDataColumnContainer CreateColumnContainerInstance(CoreDataColumnObj columnObj) => new DataColumnContainer((DataColumnObj)columnObj);

    protected override CoreDataColumnContainerBuilder CreateDataColumnContainerBuilder(string columnName,
        TableStorageType valueType,
        TableStorageTypeModifier valueTypeModifier,
        Type dataType,
        bool autoIncrement,
        bool unique,
        uint? columnMaxLength,
        object defaultValue,
        bool builtin,
        bool serviceColumn,
        bool allowNull,
        int columnHandle)
    {
        var dataColumnContainer = new DataColumnContainerBuilder()
        {
            ColumnName = columnName,
            Type = valueType,
            TypeModifier = valueTypeModifier,
            DataType = dataType,
            ColumnHandle = columnHandle,

            AllowNull = allowNull,
            IsAutomaticValue = autoIncrement,
            DefaultValue = defaultValue,
            HasIndex = unique,
            IsBuiltin = builtin,
            IsServiceColumn = serviceColumn,
            MaxLength = columnMaxLength,
        };
        return dataColumnContainer;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    IEnumerator<IDataTableColumn> IEnumerable<IDataTableColumn>.GetEnumerator() => this.GetColumns().GetEnumerator();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    IDataColumnCollection<IDataTableColumn> IDataColumnCollection<IDataTableColumn>.Add(IDataTableColumn item)
    {
        this.AddColumn(item);
        return this;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    IDataColumnCollection<IDataTableColumn> IDataColumnCollection<IDataTableColumn>.AddRange(IEnumerable<IDataTableColumn> items)
    {
        foreach (var item in items)
        {
            this.AddColumn(item);
        }
        return this;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    void IDataColumnCollection<IDataTableColumn>.Remove(IDataTableColumn item)
    {
        this.RemoveColumn(item.ColumnName);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    void IDataColumnCollection<IDataTableColumn>.RemoveRange(IEnumerable<IDataTableColumn> items)
    {
        foreach (var column in items.ToArray())
        {
            this.RemoveColumn(column.ColumnName);
        }
    }

    void IDataColumnCollection<IDataTableColumn>.Clear()
    {
        RemoveColumns();
    }

    private void RemoveColumns()
    {
        var columns = this.GetColumns().ToArray();

        foreach (var column in columns)
        {
            this.RemoveColumn(column.ColumnHandle);
        }
    }
}