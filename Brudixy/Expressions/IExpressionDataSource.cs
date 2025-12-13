using Brudixy.Interfaces;

namespace Brudixy.Expressions;

public interface IExpressionDataSource
{
    int GetColumnHandle(string column);
    IEnumerable<string> GetColumns();
    IDataRowReadOnlyAccessor GetRowByHandle(int rowHandle);
    IEnumerable<int> GetRowHandles();
    int ColumnCount { get;  }
    string Name { get; }
    bool IsLoading { get; }
    bool ContainsColumn(string column);
    TableStorageType? GetColumnType(string column);
    bool ExpressionDependsOn(int columnHandle, string column);
    object GetRowColumnValueByHandle(int rowHandle, string column);
    object GetRowXPropertyByHandle(int rowHandle, string xProperty);
    object GetColumnXProperty(string column, string xProperty);
    IFunctionRegistry GetFunctionRegistry();
}