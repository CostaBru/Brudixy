using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class BuiltinTableAggregateFunction : AggregateFunction
{
    public AggregateType Type { get; }

    internal BuiltinTableAggregateFunction(AggregateType type, string name, Type result, bool isValidateArguments, bool isVariantArgumentList, int argumentCount, Type a1, Type a2, Type a3) : base(name, result, isValidateArguments, isVariantArgumentList, argumentCount, a1, a2, a3)
    {
        Type = type;
    }
    
    protected override object AggregateValue(Data<DataRow> rows, DataTable tbl)
    {
        var handles = rows.Select(c => c.RowHandleCore).ToData();

        rows.Dispose();

        var dataColumn = tbl.GetColumn(m_columnName);

        //todo aggregate on expression columns
        
        var dataItem = dataColumn.DataStorageLink;

        var aggregateValue = dataItem.GetAggregateValue(handles, Type, dataColumn);

        handles.Dispose();

        return aggregateValue;
    }

    protected override object AggregateValue(Data<int> rows, DataTable tbl)
    {
        if (m_childTable == null)
        {
            throw new InvalidOperationException($"Expression {this} is unbound to data table.");
        }

        if (!m_localFunc)
        {
            throw new InvalidOperationException($"Cannot evaluate non local expression {this} for {tbl.Name} table.");
        }

        var dataColumn = tbl.DataColumnInfo.ColumnMappings[m_columnName];

        var dataItem = dataColumn.DataStorageLink;

        return dataItem.GetAggregateValue(rows, Type, dataColumn);
    }
}