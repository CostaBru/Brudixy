using System.Diagnostics;
using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class UnaryNode : ExpressionNode
    {
        internal readonly int op;

        internal ExpressionNode right;

        internal UnaryNode(IExpressionDataSource table, int op, ExpressionNode right) : base(table)
        {
            this.op = op;
            this.right = right;
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            BindTable(table);
            right.Mount(table, columns);
        }

        internal override object Eval(int? row = null, IReadOnlyDictionary<string, object> testValues = null)
        {
            return EvalUnaryOp(op, right.Eval(row, testValues));
        }

        internal override object Eval(Data<int> recordNos)
        {
            return right.Eval(recordNos);
        }

        private object EvalUnaryOp(int op, object vl)
        {
            object value = null;

            if (DataExpression.IsUnknown(vl))
                return null;

            switch (op)
            {
                case Operators.Noop:
                    return vl;
                case Operators.UnaryPlus:
                {
                    var storageType = CoreDataTable.GetColumnType(vl.GetType());
                    if (IsNumeric((storageType.type, storageType.typeModifier)))
                    {
                        return vl;
                    }

                    throw ExprException.TypeMismatch("Cannot perform addition operation with this expression node: " +
                                                     ToString());
                }
                case Operators.Negative:
                {
                    // the have to be better way for doing this..
                    var storageType = CoreDataTable.GetColumnType(vl.GetType());
                    if (IsNumeric((storageType.type, storageType.typeModifier)))
                    {
                        switch (storageType.type)
                        {
                            case TableStorageType.Byte:
                                value = -(Byte)vl;
                                break;
                            case TableStorageType.Int16:
                                value = -(Int16)vl;
                                break;
                            case TableStorageType.Int32:
                                value = -(Int32)vl;
                                break;
                            case TableStorageType.Int64:
                                value = -(Int64)vl;
                                break;
                            case TableStorageType.Single:
                                value = -(Single)vl;
                                break;
                            case TableStorageType.Double:
                                value = -(Double)vl;
                                break;
                            case TableStorageType.Decimal:
                                value = -(Decimal)vl;
                                break;
                            default:
                                Debug.Assert(false, "Missing a type conversion");
                                value = null;
                                break;
                        }

                        return value;
                    }

                    throw ExprException.TypeMismatch(
                        "Cannot perform substraction operation with this expression node: " + ToString());
                }
                case Operators.Not:
                   
                        if (DataExpression.ToBoolean(vl))
                            return false;
                        return true;
                    

                default:
                    throw ExprException.UnsupportedOperator(op);
            }
        }

        internal override bool IsConstant()
        {
            return (right.IsConstant());
        }

        internal override bool IsTableConstant()
        {
            return (right.IsTableConstant());
        }

        internal override bool HasLocalAggregate()
        {
            return (right.HasLocalAggregate());
        }

        internal override bool HasRemoteAggregate()
        {
            return (right.HasRemoteAggregate());
        }

        internal override bool DependsOn(string column)
        {
            return (right.DependsOn(column));
        }


        internal override ExpressionNode Optimize()
        {
            right = right.Optimize();

            if (IsConstant())
            {
                if (table != null)
                {
                    object val = Eval(new int?());

                    return new ConstNode(table, ValueType.Object, val, false);
                }
            }

            return this;
        }
    }
}
