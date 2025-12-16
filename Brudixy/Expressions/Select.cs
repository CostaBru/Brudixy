using System.Globalization;
using System.Linq.Expressions;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class Select
    {
        private readonly IExpressionDataSource table;
        private DataExpression rowFilter;
        private ExpressionNode expression;
        private int[] records;
        private int recordCount;
        private ExpressionNode linearExpression;
        private bool candidatesForIndexUse;
        private Map<int, ColumnInfo> candidateColumns;
        private int nCandidates;
        private int matchedCandidates;

        public Select(IExpressionDataSource table, string filterExpression)
        {
            this.table = table;
            if (!string.IsNullOrEmpty(filterExpression))
            {
                rowFilter = new DataExpression(this.table, filterExpression);
                rowFilter.Mount(table);
                expression = rowFilter.ExpressionNode;
            }
        }

        private bool IsIndexSearchSupportedOperator(int op)
        {
            if (op == Operators.Like)
            {
                return true;
            }
            if (op is < Operators.EqualTo or > Operators.LessOrEqual && op != Operators.Is)
            {
                return op == Operators.IsNot;
            }
            return true;
        }

        private void AnalyzeExpression(BinaryNode expr)
        {
            if (linearExpression == expression)
            {
                return;
            }
            if (expr.op == Operators.Or)
            {
                linearExpression = expression;
            }
            else if (expr.op == Operators.And)
            {
                bool flag1 = false;
                bool flag2 = false;

                if (expr.left is BinaryNode exprLeft)
                {
                    AnalyzeExpression(exprLeft);
                    if (linearExpression == expression)
                    {
                        return;
                    }
                    flag1 = true;
                }
                else
                {
                    if (expr.left is UnaryNode unaryNode)
                    {
                        while (unaryNode.op == Operators.Noop && unaryNode.right is UnaryNode { op: Operators.Noop } right)
                        {
                            unaryNode = right;
                        }

                        if (unaryNode.op == Operators.Noop && unaryNode.right is BinaryNode nodeRight)
                        {
                            AnalyzeExpression(nodeRight);

                            if (linearExpression == expression)
                            {
                                return;
                            }
                            flag1 = true;
                        }
                    }
                }
                if (expr.right is BinaryNode exprRight)
                {
                    AnalyzeExpression(exprRight);
                    if (linearExpression == expression)
                    {
                        return;
                    }
                    flag2 = true;
                }
                else
                {
                    if (expr.right is UnaryNode unaryNode)
                    {
                        while (unaryNode.op == Operators.Noop && unaryNode.right is UnaryNode { op: Operators.Noop } nodeRight)
                        {
                            unaryNode = nodeRight;
                        }

                        if (unaryNode.op == Operators.Noop && unaryNode.right is BinaryNode right)
                        {
                            AnalyzeExpression(right);
                            if (linearExpression == expression)
                            {
                                return;
                            }
                            flag2 = true;
                        }
                    }
                }
                if (flag1 & flag2)
                {
                    return;
                }
                ExpressionNode left = flag1 ? expr.right : expr.left;
                linearExpression = linearExpression == null ? left : new BinaryNode(table, Operators.And, left, linearExpression);
            }
            else
            {
                if (IsIndexSearchSupportedOperator(expr.op))
                {
                    if (expr.left is NameNode left && expr.right is ConstNode)
                    {
                        var column = left.column;

                        var columnHandle = table.GetColumnHandle(column);

                        var columnInfo = candidateColumns[columnHandle];

                        var binaryNode = columnInfo.expr == null ? expr : new BinaryNode(table, Operators.And, expr, columnInfo.expr);
                        columnInfo.expr = binaryNode;

                        if (expr.op == Operators.EqualTo)
                        {
                            columnInfo.equalsOperator = true;
                        }

                        candidatesForIndexUse = true;
                        return;
                    }
                    if (expr.right is NameNode && expr.left is ConstNode)
                    {
                        ExpressionNode exprRight = expr.left;
                        BinaryNode binaryNode = expr;
                        ExpressionNode expressionNode = binaryNode.right;
                        binaryNode.left = expressionNode;
                        expr.right = exprRight;
                        switch (expr.op)
                        {
                            case Operators.GreaterThen:
                                expr.op = Operators.LessThen;
                                break;
                            case Operators.LessThen:
                                expr.op = Operators.GreaterThen;
                                break;
                            case Operators.GreaterOrEqual:
                                expr.op = Operators.LessOrEqual;
                                break;
                            case Operators.LessOrEqual:
                                expr.op = Operators.GreaterOrEqual;
                                break;
                        }

                        var columnHandle = table.GetColumnHandle(((NameNode)expr.left).column);

                        var columnInfo1 = candidateColumns[columnHandle];
                        var columnInfo2 = columnInfo1;
                        var binaryNode2 = columnInfo2.expr == null ? expr : new BinaryNode(table, Operators.And, expr, columnInfo1.expr);
                        columnInfo2.expr = binaryNode2;
                        if (expr.op == Operators.EqualTo)
                        {
                            columnInfo1.equalsOperator = true;
                        }
                        candidatesForIndexUse = true;
                        return;
                    }
                }
                linearExpression = linearExpression == null ? expr : new BinaryNode(table, Operators.And, expr, linearExpression);
            }
        }

        private void InitCandidateColumns()
        {
            nCandidates = 0;
            candidateColumns = new Map<int, ColumnInfo>();
            if (rowFilter == null)
            {
                return;
            }

            var dependency = rowFilter.GetDependency();
            
            foreach (var column in dependency)
            {
                if (table.ContainsColumn(column))
                {
                    var columnHandle = table.GetColumnHandle(column);

                    candidateColumns[columnHandle] = new ColumnInfo();
                    nCandidates += 1;
                }
            }
        }

        public IEnumerable<T> SelectRows<T>() where T: IDataRowReadOnlyAccessor
        {
            InitCandidateColumns();
            
            if (expression is BinaryNode)
            {
                AnalyzeExpression((BinaryNode)expression);
            }

            IEnumerable<int> rows = null;
            
            //prefilter rows using indexes using const values in expression
            if (candidatesForIndexUse && table is DataTable dt && dt.RowCount > 1)
            {
                TryFilterRowsByIndexes<T>(dt, ref rows);
            }
            
            //evaluate expression fro each row in set
            UseLinearSearch();

            if (rows == null)
            {
                rows = table.GetRowHandles();
            }

            foreach (var row in rows)
            {
                var ok = Evaluate(row);

                if (ok)
                {
                    yield return (T)table.GetRowByHandle(row);
                }
            }
        }

        private void UseLinearSearch()
        {
            linearExpression = expression;

            if (linearExpression == expression)
            {
                foreach (var value in candidateColumns.Values)
                {
                    value.equalsOperator = false;
                    value.expr = null;
                }
            }
        }

        private bool TryFilterRowsByIndexes<T>(DataTable dt, ref IEnumerable<int> rows) where T : IDataRowReadOnlyAccessor
        {
            var binaryNode = (BinaryNode)expression;

            var storage = new Data<BinaryNode>();

            var q = storage.AsQueue();

            q.Enqueue(binaryNode);

            var builder = new Lazy<DataExtractor<DataRow>>(() => new DataExtractor<DataRow>(dt));

            while (q.Any)
            {
                var node = q.Dequeue();

                if (node.left is BinaryNode lb)
                {
                    q.Enqueue(lb);
                }

                if (node.right is BinaryNode rb)
                {
                    q.Enqueue(rb);
                }

                NameNode nd = null;
                ConstNode cn = null;

                if (node.left is NameNode ln && node.right is ConstNode rc)
                {
                    nd = ln;
                    cn = rc;
                }

                if (node.right is NameNode rn && node.left is ConstNode lc)
                {
                    nd = rn;
                    cn = lc;
                }

                if (nd != null && cn != null && cn.m_val is IComparable val && dt.HasIndex(nd.column))
                {
                    if (node is LikeNode liken)
                    {
                        if (liken.Kind != LikeNode.match_all && liken.GetPattern((string)((ConstNode)liken.right).m_val) is string pattern)
                        {
                            var column = dt.GetColumn(nd.column);
                            
                            var caseSensitive = column.GetXProperty<bool?>(CoreDataTable.StringIndexCaseSensitiveXProp) ?? true;

                            if (column.Type == TableStorageType.String && dt.HasIndex(nd.column))
                            {
                                builder.Value.Where(nd.column);

                                switch (liken.Kind)
                                {
                                    case LikeNode.match_exact:
                                        builder.Value.Equals(pattern, caseSensitive);
                                        break;
                                    case LikeNode.match_left:
                                        builder.Value.StartsWith(pattern, caseSensitive);
                                        break;
                                    case LikeNode.match_right:
                                        builder.Value.EndsWith(pattern, caseSensitive);
                                        break;
                                    case LikeNode.match_middle:
                                        builder.Value.Contains(pattern, caseSensitive);
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (node.op)
                        {
                            case Operators.EqualTo:
                                builder.Value.Where(nd.column);
                                builder.Value.Equals(val);
                                break;
                        }
                    }
                }
            }

            if (builder.IsValueCreated)
            {
                rows = builder.Value.Select(r => r.RowHandleCore);

                return true;
            }

            return false;
        }

        private int Eval(BinaryNode expr, int? row)
        {
            if (expr.op == Operators.And)
            {
                int leftRez = Eval((BinaryNode)expr.left, row);
                if (leftRez != 0)
                {
                    return leftRez;
                }
                int rightRez = Eval((BinaryNode)expr.right, row);
                if (rightRez != 0)
                {
                    return rightRez;
                }
                return 0;
            }

            long num = 0L;

            object vLeft = expr.left.Eval(row);

            if (expr.op != Operators.Is && expr.op != Operators.IsNot)
            {
                object vRight = expr.right.Eval(row);
                bool lc = expr.left is ConstNode;
                bool rc = expr.right is ConstNode;
                if (vLeft == null)
                {
                    return -1;
                }
                if (vRight == null)
                {
                    return 1;
                }
                var vLeftStorageType = CoreDataTable.GetColumnType(vLeft.GetType());
                if (TableStorageType.Char == vLeftStorageType.type)
                {
                    vRight = Convert.ToChar(vRight, CultureInfo.InvariantCulture);
                }

                var vRightStorageType = CoreDataTable.GetColumnType(vRight.GetType());
                var resultType =  expr.ResultType(vLeftStorageType, vRightStorageType, lc, rc, expr.op);

                if (resultType.type == TableStorageType.Empty)
                {
                    expr.SetTypeMismatchError(expr.op, vLeft, vRight);
                }

                num = expr.BinaryCompare(vLeft, vRight, resultType.type, resultType.modifier, expr.op);
            }
            switch (expr.op)
            {
                case Operators.EqualTo:
                    num = num == 0L ? 0L : (num < 0L ? -1L : 1L);
                    break;
                case Operators.GreaterThen:
                    num = num > 0L ? 0L : -1L;
                    break;
                case Operators.LessThen:
                    num = num < 0L ? 0L : 1L;
                    break;
                case Operators.GreaterOrEqual:
                    num = num >= 0L ? 0L : -1L;
                    break;
                case Operators.LessOrEqual:
                    num = num <= 0L ? 0L : 1L;
                    break;
                case Operators.Is:
                    num = vLeft == null ? 0L : -1L;
                    break;
                case Operators.IsNot:
                    num = vLeft != null ? 0L : 1L;
                    break;
            }
            return (int)num;
        }

        private bool Evaluate(int? row)
        {
            if (linearExpression != null)
            {
                object obj = linearExpression.Eval(row);

                try
                {
                    return DataExpression.ToBoolean(obj);
                }
                catch (Exception ex)
                {
                    if (ADP.IsCatchableExceptionType(ex))
                    {
                        throw ExprException.FilterConvertion(rowFilter.Expression, obj);
                    }
                    throw;
                }
            }

            int evalCount = 0;
            int validCount = 0;

            foreach (var column in candidateColumns.Values)
            {
                var binaryNode = column.expr;

                if (binaryNode == null)
                {
                    continue;
                }

                validCount++;

                int num = Eval(binaryNode, row);

                if (num == 0)
                {
                    evalCount++;
                }
            }
            return evalCount > 0 && evalCount == validCount;
        }
      
        private sealed class ColumnInfo
        {
            public bool flag;
            public bool equalsOperator;
            public BinaryNode expr;
        }
    }
}
