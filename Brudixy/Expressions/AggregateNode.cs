using Brudixy.Expressions.Functions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class AggregateNode : ExpressionNode
    {
        private readonly AggregateType type;
        private readonly bool local;
        private readonly string relationName;
        private readonly string columnName;
        private DataTable childTable;
        private string column;
        private DataRelation relation;
        protected AggregateFunction m_function;

        internal AggregateNode(IExpressionDataSource table, FunctionNode aggregateType, string columnName)
          : this(table, aggregateType, columnName, true, null)
        {
        }
    

        internal AggregateNode(IExpressionDataSource table, FunctionNode aggregateType, string columnName, bool local, string relationName)
          : base(table)
        {
            m_function = (AggregateFunction)aggregateType.Function;
            switch (aggregateType.Name)
            {
                case nameof(Aggregate.Sum):
                    type = AggregateType.Sum;
                    break;
                case  nameof(Aggregate.Avg):
                    type = AggregateType.Mean;
                    break;
                case  nameof(Aggregate.Min):
                    type = AggregateType.Min;
                    break;
                case  nameof(Aggregate.Max):
                    type = AggregateType.Max;
                    break;
                case  nameof(Aggregate.Count):
                    type = AggregateType.Count;
                    break;
                case nameof(Aggregate.Var):
                    type = AggregateType.Var;
                    break;
                default:
                {
                    if (aggregateType.Name != nameof(Aggregate.StDev))
                    {
                        throw new ArgumentException($"Unsupported function {aggregateType.Name} passed.");
                    }
                    type = AggregateType.StDev;
                    break;
                }
            }
            this.local = local;
            this.relationName = relationName;
            this.columnName = columnName;
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            BindTable(table);
            
            var tbl = (DataTable)table;

            if (tbl == null)
            {
                throw new InvalidOperationException($"Expression {this} is unbound to data table.");
            }

            if (local)
            {
                relation = null;
            }
            else
            {
                if (relationName == null)
                {
                    if (tbl.ChildRelationsMap != null)
                    {
                        if (tbl.ChildRelationsMap.Count > 1)
                        {
                            throw new InvalidOperationException($"Expression {this} error. {tbl.Name} table has more than one children {tbl.ChildRelationsMap.Count} relations. ");
                        }
                        if (tbl.ChildRelationsMap.Count != 1)
                        {
                            throw new InvalidOperationException($"Expression {this} error. {tbl.Name} table has no children {tbl.ChildRelationsMap.Count} relations. ");
                        }

                        relation = tbl.ChildRelationsMap.Values.First();
                    }
                }
                else
                {
                    relation = tbl.GetChildRelation(relationName);
                }
            }
            
            if (relation == null)
            {
                childTable = tbl;
            }
            else
            {
                childTable = (DataTable)relation.ParentTable;
            }

            column = columnName;

            if (column == null)
            {
                throw new InvalidOperationException($"Expression {this} on {tbl.Name} is unbound to any column.");
            }

            int index = 0;
            while (index < columns.Count && column != columns[index])
            {
                ++index;
            }

            if (index >= columns.Count)
            {
                columns.Add(column);
            }
            Bind(relation, columns);
            
            m_function.Bind(childTable, relation, local, columnName);
        }

        internal static void Bind(DataRelation relation, Data<string> columns)
        {
            if (relation == null)
            {
                return;
            }
            
            foreach (var coreDataColumn in relation.ChildColumnsReference)
            {
                var dataColumn = (DataColumn)coreDataColumn;
                if (!columns.Contains(dataColumn.ColumnName))
                {
                    columns.Add(dataColumn.ColumnName);
                }
            }
            foreach (var coreDataColumn in relation.ParentColumnsReference)
            {
                var dataColumn = (DataColumn)coreDataColumn;
                if (!columns.Contains(dataColumn.ColumnName))
                {
                    columns.Add(dataColumn.ColumnName);
                }
            }
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null)
        {
            if (childTable == null)
            {
                throw new InvalidOperationException($"Expression {this} is unbound to data table.");
            }

            return m_function.Eval(table, new Data<ExpressionNode>(), row, testValues);
        }

        internal override object Eval(Data<int> records)
        {
            return m_function.Eval(records);
        }

        internal override bool IsConstant()
        {
            return false;
        }

        internal override bool IsTableConstant()
        {
            return local;
        }

        internal override bool HasLocalAggregate()
        {
            return local;
        }

        internal override bool HasRemoteAggregate()
        {
            return !local;
        }

        internal override bool DependsOn(string column)
        {
            if (this.column == column)
            {
                return true;
            }

            var columnHandle = table.GetColumnHandle(this.column);

            return table.ExpressionDependsOn(columnHandle, column);
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }
    }
}
