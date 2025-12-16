using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public abstract class AggregateFunction : Function
{
    private Type m_nameNodeType = typeof(NameNode);
    
    internal AggregateFunction(string name, Type result, bool isValidateArguments, bool IsVariantArgumentList, int argumentCount, Type a1, Type a2, Type a3) : base(name, result, isValidateArguments, IsVariantArgumentList, argumentCount, a1, a2, a3)
    {
    }

    protected DataTable m_childTable;
    protected DataRelation m_relation;
    protected bool m_localFunc;
    protected string m_columnName;

    public void Bind(DataTable childTable, DataRelation relation, bool local, string column)
    {
       this.m_childTable = childTable;
       this.m_relation = relation;
       this.m_localFunc = local;
       this.m_columnName = column;
    }
    
    public override void BindArguments(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments, Data<string> columns)
    {
        if (arguments.Count != 1)
        {
            throw ExprException.FunctionArgumentCount(this.name, 1, arguments.Count);
        }
        
        if (expressionDataSource is DataTable dt)
        {
            this.m_localFunc = true;
            this.m_childTable = dt;
        }

        var expressionNode = arguments[0];
                
        if (expressionNode is NameNode nn)
        {
            this.m_columnName = nn.name;
        }

        expressionNode.Mount(expressionDataSource, columns);
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource,
        Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (m_childTable == null)
        {
            throw new InvalidOperationException($"Expression {this} is unbound to data table.");
        }

        Data<DataRow> rows;

        var tbl = (DataTable)expressionDataSource;
            
        if (m_localFunc)
        {
            rows = m_childTable.Rows.ToData();
        }
        else
        {
            if (row == null)
            {
                throw new InvalidOperationException($"Expression {this} is unbound to data table row.");
            }

            if (m_relation == null)
            {
                throw new InvalidOperationException($"Expression {this} is unbound to data relation.");
            }

            var dataRow = tbl.GetRowByHandle(row.Value);

            rows = dataRow.GetChildRowsCore(m_relation).ToData();
        }

        return AggregateValue(rows, tbl);
    }
    
    public object Eval(Data<int> records)
    {
        if (m_childTable == null)
        {
            throw new InvalidOperationException($"Expression {this} is unbound to data table.");
        }

        return AggregateValue(records, m_childTable);
    }

    protected abstract object AggregateValue(Data<DataRow> rows, DataTable tbl);
    protected abstract object AggregateValue(Data<int> rows, DataTable tbl);
}