using Brudixy.Expressions;
using Brudixy.Interfaces;

namespace Brudixy;

public class DataRowExpressionSource : IExpressionDataSource
{
    private readonly IDataRowReadOnlyAccessor m_dataRow;
    private readonly IReadOnlyDictionary<string, object> m_testing;

    public DataRowExpressionSource(IDataRowReadOnlyAccessor dataRow, IReadOnlyDictionary<string, object> testing = null)
    {
        m_dataRow = dataRow;
        m_testing = testing;
    }
        
    public int GetColumnHandle(string column)
    {
        return m_dataRow.GetColumn(column).ColumnHandle;
    }

    public IDataRowReadOnlyAccessor GetRowByHandle(int rowHandle)
    {
        return m_dataRow;
    }

    public IEnumerable<int> GetRowHandles()
    {
        yield return m_dataRow.RowHandle;
    }

    public int ColumnCount => m_dataRow.GetColumnCount();

    public string Name => m_dataRow.GetTableName() + "."+ m_dataRow.RowHandle;

    public bool IsLoading => false;

    public bool ContainsColumn(string column)
    {
        return m_dataRow.IsExistsField(column);
    }

    public TableStorageType? GetColumnType(string column)
    {
        return m_dataRow.TryGetColumn(column)?.Type;
    }

    public bool ExpressionDependsOn(int columnHandle, string column)
    {
        return false;
    }

    public object GetRowColumnValueByHandle(int rowHandle, string column)
    {
        var value = m_testing?.GetValueOrDefault(column, m_dataRow[column]) ?? m_dataRow[column];

        if (value == null)
        {
            return string.Empty;
        }
        
        return value;
    }

    public object GetRowXPropertyByHandle(int rowHandle, string xProperty)
    {
        var rowXPropertyByHandle = m_dataRow.GetXProperty<object>(xProperty);
        
        return m_testing?.GetValueOrDefault(xProperty, rowXPropertyByHandle) ?? rowXPropertyByHandle;
    }

    public object GetColumnXProperty(string column, string xProperty)
    {
        var columnXProperty = m_dataRow.GetColumn(column).GetXProperty<object>(xProperty);
        return m_testing?.GetValueOrDefault(xProperty, columnXProperty) ?? columnXProperty;
    }
    
    public IFunctionRegistry GetFunctionRegistry()
    {
        if (m_dataRow is DataRow dt && dt.table != null)
        {
            return ((IExpressionDataSource)dt.table).GetFunctionRegistry();
        }
        
        return FunctionRegistry.Registry;
    }
}