using Brudixy.Expressions;
using Brudixy.Interfaces;

namespace Brudixy;

public static class RowExtensions
{
    public static IEnumerable<IDataRowContainer> ToContainer(this IEnumerable<IDataTableRow> rows)
    {
        foreach (var row in rows)
        {
            yield return row.ToContainer();
        }
    }

    public static IEnumerable<IDataRowReadOnlyContainer> ToContainer(this IEnumerable<IDataTableReadOnlyRow> rows)
    {
        foreach (var row in rows)
        {
            yield return row.ToReadOnlyDataContainer();
        }
    }

    public static IEnumerable<IDataRowReadOnlyContainer> ToReadOnlyContainer(this IEnumerable<IDataTableReadOnlyRow> rows)
    {
        foreach (var row in rows)
        {
            yield return row.ToReadOnlyDataContainer();
        }
    }
    
    public static IEnumerable<IDataTableRow> FilterByFieldValue<T>(this IEnumerable<IDataTableRow> rowForCheck, string fieldName, IEnumerable<T> list)
    {
        return (from c1 in rowForCheck.Where(r => r.IsNull(fieldName) == false)
            join c2 in list on c1.Field<T>(fieldName) equals c2
            select c1);
    }
    
    public static IEnumerable<IDataTableRow> FilterByFieldValueNotEquals<T>(this IEnumerable<IDataTableRow> rowForCheck, string fieldName, IReadOnlyCollection<T> list)
    {
        ICoreTableReadOnlyColumn column = null;

        foreach (var dataTableRow in rowForCheck)
        {
            if (column == null)
            {
                column = dataTableRow.GetColumn(fieldName);
            }

            if (list.Contains(dataTableRow.Field<T>(column)) == false)
            {
                yield return dataTableRow;
            }
        }
    }
    
    public static IEnumerable<IDataRowReadOnlyContainer> ToReadOnlyContainer(this IEnumerable<IDataTableRow> rows)
    {
        foreach (var row in rows)
        {
            yield return row.ToContainer();
        }
    }
    
    public static IEnumerable<IDataTableRow> FilterByFieldValue<T>(this IEnumerable<IDataTableRow> rowForCheck, string fieldName, IReadOnlyCollection<T> list)
    {
        IDataTableReadOnlyColumn column = null;

        foreach (var dataTableRow in rowForCheck)
        {
            if (column == null)
            {
                column = dataTableRow.GetColumn(fieldName);
            }

            if (list.Contains(dataTableRow.Field<T>(column)))
            {
                yield return dataTableRow;
            }
        }
    }

    public static IDataTableRowEnumerableReadOnly<IDataTableRow> ToRowEnumerableEmpty(this IDataTable table)
    {
        return new DataRowExtractorStub<IDataTableRow>(table);
    }
    
    public static IDataTableRowEnumerableReadOnly<T> ToRowEnumerableEmpty<T>(this IEnumerable<T> table) where T: IDataTableRow
    {
        return new DataRowExtractorReadOnlyStub<T>();
    }
    
    public static IDataTableRowEnumerable<T> RowsOfType<T>(this IDataTable table) where T : IDataTableReadOnlyRow
    {
        if (table.RowCount == 0)
        {
            return new DataRowExtractorStub<T>(table);
        }
            
        if (table is DataTable t)
        {
            return new DataExtractor<T>(t);
        }

        return new DataRowExtractorAdapter<T>(table);
    }

    internal static IDataTableRowEnumerable<T> RowsOfTypeAdapter<T>(this IDataTable table) where T : IDataTableReadOnlyRow
    {
        return new DataRowExtractorAdapter<T>(table);
    }
    
    public static IEnumerable<T> ApplyFilterExpression<T>(this IEnumerable<T> rows, string expression)
        where T : IDataRowReadOnlyAccessor
    {
        var map = rows.ToDictionary(r => r.RowHandle);

        if (map.Count == 0)
        {
            return Array.Empty<T>();
        }

        var select = new Select(new ArrayExpressionSource<T>(map), expression);

        return select.SelectRows<T>();
    }
    
    private class ArrayExpressionSource<T> : IExpressionDataSource where T : IDataRowReadOnlyAccessor
    {
        private readonly IReadOnlyDictionary<int, T> m_rows;

        public ArrayExpressionSource(IReadOnlyDictionary<int, T> rows)
        {
            m_rows = rows;
        }
        
        public int GetColumnHandle(string column)
        {
            return m_rows.First().Value.GetColumn(column).ColumnHandle;
        }

        public IDataRowReadOnlyAccessor GetRowByHandle(int rowHandle)
        {
            return m_rows[rowHandle];
        }

        public IEnumerable<int> GetRowHandles()
        {
            return m_rows.Keys;
        }

        public int ColumnCount => m_rows.First().Value.GetColumnCount();

        public string Name => m_rows.First().Value.GetTableName() + ".RowEnumeration";

        public bool IsLoading => false;

        public bool ContainsColumn(string column)
        {
            return m_rows.First().Value.IsExistsField(column);
        }

        public TableStorageType? GetColumnType(string column)
        {
            return m_rows.First().Value.TryGetColumn(column)?.Type;
        }

        public bool ExpressionDependsOn(int columnHandle, string column)
        {
            return false;
        }

        public object GetRowColumnValueByHandle(int rowHandle, string column)
        {
            return m_rows[rowHandle][column];
        }

        public object GetRowXPropertyByHandle(int rowHandle, string xProperty)
        {
            return m_rows[rowHandle].GetXProperty<object>(xProperty);
        }

        public object GetColumnXProperty(string colum, string xProperty)
        {
            return m_rows.FirstOrDefault().Value?.TryGetColumn(colum)?.GetXProperty<object>(xProperty);
        }

        public IFunctionRegistry GetFunctionRegistry()
        {
            return FunctionRegistry.Registry;
        }
    }
}