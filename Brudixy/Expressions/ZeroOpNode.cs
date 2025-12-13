using System.Diagnostics;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class ZeroOpNode : ExpressionNode
    {
        internal readonly int op;

        internal const int zop_True = 1;
        internal const int zop_False = 0;
        internal const int zop_Null = -1;

        internal ZeroOpNode(int op) : base(null)
        {
            this.op = op;
            Debug.Assert(op == Operators.True || op == Operators.False || op == Operators.Null, "Invalid zero-op");
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
        }

        internal object EvalCore()
        {
            switch (op)
            {
                case Operators.True:
                    return true;
                case Operators.False:
                    return false;
                case Operators.Null:
                    return null;
                default:
                    Debug.Assert(op == Operators.True || op == Operators.False || op == Operators.Null, "Invalid zero-op");
                    return null;
            }
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null, bool test = false)
        {
            return EvalCore();
        }

        internal override object Eval(Data<int> recordNos)
        {
            return EvalCore();
        }

        internal override bool IsConstant()
        {
            return true;
        }

        internal override bool IsTableConstant()
        {
            return true;
        }

        internal override bool HasLocalAggregate()
        {
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            return false;
        }

        internal override ExpressionNode Optimize()
        {
            return this;
        }
    }
}
