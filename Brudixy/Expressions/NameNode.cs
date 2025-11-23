using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class NameNode : ExpressionNode
    {
        internal char open;
        internal char close;
        internal string name;
        internal bool found;
        internal bool type;
        internal string column;

        internal NameNode(IExpressionDataSource table, char[] text, int start, int pos)
          : base(table)
        {
            name = ParseName(text, start, pos);
        }

        internal NameNode(IExpressionDataSource table, string name)
          : base(table)
        {
            this.name = name;
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            BindTable(table);
            if (table == null)
            {
                throw ExprException.UnboundName($"Cannot locate {name} name node for null table given.");
            }
           
            column = name;

            found = table.ContainsColumn(column);

            int index = 0;

            while (index < columns.Count && column != columns[index])
            {
                ++index;
            }

            if (index < columns.Count)
            {
                return;
            }

            columns.Add(column);
        }

        public TableStorageType? ColumnType => table.GetColumnType(column); 


        internal override object Eval(int? row,
            IReadOnlyDictionary<string, object> testValues = null)
        {
            if (!found)
            {
                throw ExprException.UnboundName($"'{name}' name node is not connected to the DataTable.");
            }

            if (row == null)
            {
                throw ExprException.UnboundName($"'{name}' name node cannot be evaluated for null row given.");
            }

            if (testValues != null && testValues.TryGetValue(column, out var testValue))
            {
                return testValue;
            }
            
            return table.GetRowColumnValueByHandle(row.Value, column);
        }

        internal override object Eval(Data<int> records)
        {
            throw ExprException.ComputeNotAggregate(ToString());
        }

        internal override bool IsConstant()
        {
            return false;
        }

        internal override bool IsTableConstant()
        {
        /*    if (this.column != null && this.column.Computed)
                return this.column.DataExpression.IsTableAggregate();*/
            return false;
        }

        internal override bool HasLocalAggregate()
        {
         /*   if (this.column != null && this.column.Computed)
                return this.column.DataExpression.HasLocalAggregate();*/
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
           /* if (this.column != null && this.column.Computed)
                return this.column.DataExpression.HasRemoteAggregate();*/
            return false;
        }

        internal override bool DependsOn(string column)
        {
            if (string.Equals(this.column, column, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var columnHandle = table.GetColumnHandle(this.column);

            return table?.ExpressionDependsOn(columnHandle, column) ?? false;
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }

        internal static string ParseName(char[] text, int start, int pos)
        {
            char ch = char.MinValue;
            string str = "";
            int startIndex = start;
            int num = pos;
            if (text[start] == 96)
            {
                checked { ++start; }
                checked { --pos; }
                ch = '\\';
                str = "`";
            }
            else if (text[start] == 91)
            {
                checked { ++start; }
                checked { --pos; }
                ch = '\\';
                str = "]\\";
            }
            if (ch != 0)
            {
                int index1 = start;
                for (int index2 = start; index2 < pos; ++index2)
                {
                    if (text[index2] == ch && index2 + 1 < pos && str.IndexOf(text[index2 + 1]) >= 0)
                        ++index2;
                    text[index1] = text[index2];
                    ++index1;
                }
                pos = index1;
            }

            if (pos == start)
            {
                throw ExprException.InvalidName(new string(text, startIndex, num - startIndex));
            }
            return new string(text, start, pos - start);
        }
    }
}
