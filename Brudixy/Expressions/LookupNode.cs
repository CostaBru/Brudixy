using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class LookupNode : ExpressionNode
    {
        private readonly string relationName;
        private readonly string columnName;
        private string parentTableColumn;
        private DataRelation relation;

        internal LookupNode(IExpressionDataSource table, string columnName, string relationName)
          : base(table)
        {
            this.relationName = relationName;
            this.columnName = columnName;
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            var dataTable = (DataTable)table;

            BindTable(table);
            parentTableColumn = null;
            relation = null;
            if (table == null)
            {
                throw ExprException.ExpressionUnbound(ToString());
            }
            var parentRelations = dataTable.ParentRelationsMap;

            if (dataTable.ParentRelationsMap == null)
            {
                throw new InvalidOperationException(
                    $"Parent relation collection is not initialized for {table.Name}.");
            }
            
            if (relationName == null)
            {
                if (parentRelations?.Count > 1)
                {
                    throw ExprException.UnresolvedRelation(table.Name, ToString(), $"Parent expression parent wasn't set and {parentRelations?.Count} parent relations found. Should have a single one.");
                }
                relation = parentRelations?.Values.First();
            }
            else
            {
                relation = parentRelations?[relationName];
            }

            if (relation == null)
            {
                throw ExprException.BindFailure(relationName, table.Name);
            }

            var parentTable = relation.ParentTable;

            if (parentTable == null)
            {
                throw new InvalidOperationException($"Node is detached from data table. Relation: {relationName}, Parent Table: {relation.ParentTableName}, Child Table: {relation.ChildTableName}");
            }

            parentTableColumn = parentTable.DataColumnInfo.ColumnMappings.ContainsKey(columnName) ? columnName : null;
            if (parentTableColumn == null)
            {
                throw ExprException.UnboundName($"parent table '{parentTable.Name}' doesn't contain '{columnName}' column.");
            }

            int index = 0;
            while (index < columns.Count && parentTableColumn != columns[index])
            {
                ++index;
            }

            if (index >= columns.Count)
            {
                columns.Add(parentTableColumn);
            }

            AggregateNode.Bind(relation, columns);
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null, bool test = false)
        {
            if (parentTableColumn == null || relation == null || row == null)
            {
                throw ExprException.ExpressionUnbound(ToString());
            }

            var dataRow = (DataRow)table.GetRowByHandle(row.Value);

            if (testValues != null && testValues.TryGetValue(parentTableColumn, out var testValue))
            {
                return testValue;
            }

            DataRow parentRow = dataRow.GetParentRow(relation);

            return parentRow?[parentTableColumn];
        }

        internal override object Eval(Data<int> recordNos)
        {
            throw ExprException.ComputeNotAggregate(ToString());
        }

        internal override bool IsConstant()
        {
            return false;
        }

        internal override bool IsTableConstant()
        {
            return false;
        }

        internal override bool HasLocalAggregate()
        {
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            return false;
        }

        internal override bool DependsOn(string column)
        {
            return parentTableColumn == column;
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }
    }
}
