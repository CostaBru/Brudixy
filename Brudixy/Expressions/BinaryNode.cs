using System.Diagnostics;
using Brudixy.Exceptions;
using Brudixy.Interfaces.Tools;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal class BinaryNode : ExpressionNode
    {
        internal int op;

        internal ExpressionNode left;
        internal ExpressionNode right;

        internal BinaryNode(IExpressionDataSource table, int op, ExpressionNode left, ExpressionNode right) : base(table)
        {
            this.op = op;
            this.left = left;
            this.right = right;
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            BindTable(table);
            
            left.Mount(table, columns);
            right.Mount(table, columns);
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null, 
            bool test = false)
        {
            return EvalBinaryOp(op, left, right, row, testValues: testValues, test: test);
        }

        internal override object Eval(Data<int> recordNos)
        {
            return EvalBinaryOp(op, left, right, null, recordNos);
        }

        internal override bool IsConstant()
        {
            // 
            return (left.IsConstant() && right.IsConstant());
        }

        internal override bool IsTableConstant()
        {
            return (left.IsTableConstant() && right.IsTableConstant());
        }
        internal override bool HasLocalAggregate()
        {
            return (left.HasLocalAggregate() || right.HasLocalAggregate());
        }

        internal override bool HasRemoteAggregate()
        {
            return (left.HasRemoteAggregate() || right.HasRemoteAggregate());
        }

        internal override bool DependsOn(string column)
        {
            if (left.DependsOn(column))
            {
                return true;
            }
            return right.DependsOn(column);
        }

        internal override ExpressionNode Optimize()
        {
            left = left.Optimize();

            if (op == Operators.Is)
            {
                // only 'Is Null' or 'Is Not Null' are valid
                if (right is UnaryNode node)
                {
                    if (node.op != Operators.Not)
                    {
                        throw new InvalidExpressionException($"Binary expression {this} syntax error.");
                    }
                    op = Operators.IsNot;
                    right = node.right;
                }
                if (right is ZeroOpNode opNode)
                {
                    if (opNode.op != Operators.Null)
                    {
                        throw new InvalidExpressionException($"Binary expression {this} syntax error.");
                    }
                }
                else
                {
                    throw new InvalidExpressionException($"Binary expression {this} syntax error.");
                }
            }
            else
            {
                right = right.Optimize();
            }

            if (IsConstant())
            {
                var val = Eval();

                if (val == null)
                {
                    return new ZeroOpNode(Operators.Null);
                }

                if (val is bool)
                {
                    if ((bool) val)
                    {
                        return new ZeroOpNode(Operators.True);
                    }

                    return new ZeroOpNode(Operators.False);
                }
                return new ConstNode(table, ValueType.Object, val, false);
            }

            return this;
        }

        internal void SetTypeMismatchError(int op, object left, object right)
        {
            var leftType = left?.GetType();
            var rightType = right?.GetType();

            throw new EvaluateException($"Binary operator {Operators.ToString(op)} type mismatch left: '{left}' ({leftType}), right '{right}' ({rightType}).");
        }

        private object Eval(ExpressionNode expr, int? row, Data<int> recordNos, IReadOnlyDictionary<string, object> testValues = null, bool test = false)
        {
            if (recordNos == null)
            {
                return expr.Eval(row, testValues, test);
            }

            return expr.Eval(recordNos);
        }
        
        public T ConvertBoxed<T>(object x, string operation)
        {
            if (x is null)
            {
                return default;
            }
            
            var tableStorageType = CoreDataTable.GetColumnType(x.GetType());
            
            return TypeConvertor.ConvertValue<T>(x, operation, table.Name, tableStorageType.type, tableStorageType.typeModifier, "Binary Expression Evaluation");
        }

        internal int BinaryCompare(object vLeft,
            object vRight, 
            TableStorageType resultType,
            TableStorageTypeModifier resultTypeModifier,
            int op)
        {
            int result = 0;
            try
            {
                switch (resultType)
                {
                    case TableStorageType.SByte:
                    case TableStorageType.Int16:
                    case TableStorageType.Int32:
                    case TableStorageType.Byte:
                    case TableStorageType.UInt16:
                    case TableStorageType.Char:
                        return ConvertBoxed<int>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<int>(vRight, nameof(BinaryCompare)));
                    case TableStorageType.Int64:
                    case TableStorageType.UInt32:
                    case TableStorageType.UInt64:
                    case TableStorageType.Decimal:
                        return ConvertBoxed<decimal>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<decimal>(vRight, nameof(BinaryCompare)));
                   case TableStorageType.Double:
                        return ConvertBoxed<double>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<double>(vRight, nameof(BinaryCompare)));
                    case TableStorageType.Single:
                        return ConvertBoxed<float>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<float>(vRight, nameof(BinaryCompare)));
                    case TableStorageType.DateTime:
                        return ConvertBoxed<DateTime>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<DateTime>(vRight, nameof(BinaryCompare)));
                    case TableStorageType.DateTimeOffset:
                        return ConvertBoxed<DateTimeOffset>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<DateTimeOffset>(vRight, nameof(BinaryCompare)));
                    case TableStorageType.String:
                        return CoreDataTable.CompareStrings(CoreDataTable.ConvertObjectToString(vLeft), CoreDataTable.ConvertObjectToString(vRight));
                    case TableStorageType.Guid:
                        return ConvertBoxed<Guid>(vLeft, nameof(BinaryCompare)).CompareTo(ConvertBoxed<Guid>(vRight, nameof(BinaryCompare)));
                    case TableStorageType.Boolean:
                    {
                        if (op == Operators.EqualTo || op == Operators.NotEqual)
                        {
                            var leftType = vLeft?.GetType();
                            var rightType = vRight?.GetType();

                            if (leftType == rightType)
                            {
                                var lv = ConvertBoxed<int>(DataExpression.ToBoolean(vLeft), nameof(BinaryCompare));
                                var rv = ConvertBoxed<int>(DataExpression.ToBoolean(vRight), nameof(BinaryCompare));
                                
                                return lv.CompareTo(rv);
                            }

                            if(leftType != null && rightType != null)
                            {
                                var lStr = CoreDataTable.ConvertObjectToString(vLeft);
                                var rStr = CoreDataTable.ConvertObjectToString(vRight);

                                if (op == Operators.EqualTo)
                                {
                                    return String.Compare(lStr, rStr, StringComparison.Ordinal);
                                }
                                else
                                {
                                    return ~String.Compare(lStr, rStr, StringComparison.Ordinal);
                                }
                            }
                            else
                            {
                                this.SetTypeMismatchError(op, left, right);
                            }
                        }
                    }
                        break;
                }
            }
            catch (ArgumentException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (FormatException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (InvalidCastException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (OverflowException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            catch (EvaluateException e)
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
            }
            SetTypeMismatchError(op, vLeft, vRight);
            return result;
        }

        private object EvalBinaryOp(int op,
            ExpressionNode left, 
            ExpressionNode right,
            int? row = null,
            Data<int> recordNos = null,
            IReadOnlyDictionary<string, object> testValues = null, 
            bool test = false)
        {
            object vLeft;
            object vRight;
            TableStorageType resultType;
            TableStorageTypeModifier resultTypeModifier;

            /*
            special case for OR and AND operators: we don't want to evaluate 
            both right and left operands, because we can shortcut :
                for OR  operator If one of the operands is true the result is true 
                for AND operator If one of rhe operands is flase the result is false 
 
*/

            if (op != Operators.Or && op != Operators.And && op != Operators.In && op != Operators.Is && op != Operators.IsNot)
            {
                if (table != null)
                {
                    vLeft = Eval(left, row, recordNos, testValues, test);
                    vRight = Eval(right, row, recordNos, testValues, test);
                }
                else
                {
                    vLeft = Eval(left, null, null, testValues, test);
                    vRight = Eval(right, null, null, testValues, test);
                }
                
                //    special case of handling NULLS, currently only OR operator can work with NULLS
                if (vLeft == null)
                {
                    return null;
                }

                if (vRight == null)
                {
                    return null;
                }

                Type typeofLeft = vLeft.GetType();
                Type typeofRight = vRight.GetType();

                var leftStorage = CoreDataTable.GetColumnType(typeofLeft);
                var rightStorage = CoreDataTable.GetColumnType(typeofRight);

                var valueTuple = ResultType(leftStorage, rightStorage, (left is ConstNode), (right is ConstNode), op);
                
                resultType = valueTuple.type;
                resultTypeModifier = valueTuple.modifier;

                if (TableStorageType.Empty == resultType)
                {
                    SetTypeMismatchError(op, vLeft, vRight);
                }
            }
            else
            {
                vLeft = vRight = null;
                resultType = TableStorageType.Empty;
                resultTypeModifier = TableStorageTypeModifier.Simple;
            }

            object value = null;
            bool typeMismatch = false;

            try
            {
                switch (op)
                {
                    case Operators.Plus:
                        value = OpPlus(resultType, vLeft, vRight, ref typeMismatch, test);
                        break; // Operators.Plus

                    case Operators.Minus:
                        value = OpMinus(resultType, vLeft, vRight, ref typeMismatch, test);
                        break; // Operators.Minus 

                    case Operators.Multiply:
                        value = OnMultiply(resultType, vLeft, vRight, ref typeMismatch, test);
                        break; // Operators.Multiply

                    case Operators.Divide:
                        value = OpDivide(resultType, vLeft, vRight, ref typeMismatch, test);
                        break; // Operators.Divide 

                    case Operators.EqualTo:
                        if (vLeft == null || vRight == null)
                        {
                            return null;
                        }
                        return (0 == BinaryCompare(vLeft, vRight, resultType, resultTypeModifier, Operators.EqualTo));

                    case Operators.GreaterThen:
                        if (vLeft == null || vRight == null)
                        {
                            return null;
                        }
                        return (0 < BinaryCompare(vLeft, vRight, resultType, resultTypeModifier, op));

                    case Operators.LessThen:
                        if (vLeft == null || vRight == null)
                        {
                            return null;
                        }
                        return (0 > BinaryCompare(vLeft, vRight, resultType, resultTypeModifier, op));

                    case Operators.GreaterOrEqual:
                        if (vLeft == null || vRight == null)
                        {
                            return null;
                        }
                        return (0 <= BinaryCompare(vLeft, vRight, resultType, resultTypeModifier, op));

                    case Operators.LessOrEqual:
                        if (vLeft == null || vRight == null)
                        {
                            return null;
                        }
                        return (0 >= BinaryCompare(vLeft, vRight, resultType,  resultTypeModifier, op));

                    case Operators.NotEqual:
                        if (vLeft == null || vRight == null)
                        {
                            return null;
                        }
                        return (0 != BinaryCompare(vLeft, vRight, resultType,  resultTypeModifier, op));

                    case Operators.Is:
                        vLeft = Eval(left, row, recordNos, testValues, test);
                        return vLeft == null;

                    case Operators.IsNot:
                        vLeft = Eval(left, row, recordNos, testValues, test);
                        return vLeft != null;

                    case Operators.And:
                    {
                        /*  special case evaluating of the AND operator: we don't want to evaluate
                         both right and left operands, because we can shortcut :
                             If one of the operands is false the result is false */
                        
                        vLeft = Eval(left, row, recordNos, testValues, test);
                        
                        if (vLeft is bool lb)
                        {
                            if (lb == false)
                            {
                                value = false;

                                if (test == false)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (vLeft == null)
                            {
                                return null;
                            }
                            
                            vRight = Eval(right, row, recordNos, testValues, test);
                            typeMismatch = true;
                            
                            break;
                        }
                       
                        vRight = Eval(right, row, recordNos, testValues, test);

                        if (vRight is bool rb)
                        {
                            value = rb;
                        }
                        else
                        {
                            if (vRight == null)
                            {
                                return null;
                            }
                            
                            typeMismatch = true;
                        }

                        break;
                    }
                    case Operators.Or:
                    {
                        /* special case evaluating the OR operator: we don't want to evaluate
                        both right and left operands, because we can shortcut :
                            If one of the operands is true the result is true  */

                        vLeft = Eval(left, row, recordNos, testValues, test);

                        if (vLeft != null)
                        {
                            if (vLeft is bool lb)
                            {
                                if (lb)
                                {
                                    value = true;
                                    break;
                                }
                            }
                            else
                            {
                                vRight = Eval(right, row, recordNos, testValues, test);
                                typeMismatch = true;
                                break;
                            }
                        }

                        vRight = Eval(right, row, recordNos, testValues, test);

                        if (vRight is bool rb)
                        {
                            value = rb;
                            break;
                        }
                        else
                        {
                            if (vRight == null)
                            {
                                return vLeft;
                            }

                            if (vLeft == null)
                            {
                                return vRight;
                            }
                            
                            typeMismatch = true;
                            break;
                        }
                    }

                    case Operators.Modulo:
                        value = OpModulo(resultType, value, vLeft, vRight, resultTypeModifier, ref typeMismatch, test);
                        break;

                    case Operators.In:
                        /*
                        special case evaluating of the IN operator: the right have to be IN function node 
                        */

                        if (!(right is FunctionNode))
                        {
                            // this is more like an Assert: should never happens, so we do not care about "nice" Exceptions
                            throw new InvalidExpressionException("Function IN used without ().");
                        }

                        vLeft = Eval(left, row, recordNos, testValues, test);

                        if (vLeft == null)
                        {
                            return null;
                        }

                        /* validate IN parameters : must all be constant expressions */

                        value = false;

                        FunctionNode into = (FunctionNode)right;

                        for (int i = 0; i < into.ArgumentCount; i++)
                        {
                            vRight = into.Arguments[i].Eval(test: test);

                            if (vRight == null)
                            {
                                continue;
                            }

                            var columnType = CoreDataTable.GetColumnType(vLeft.GetType());
                            
                            resultType = columnType.type;
                            resultTypeModifier = columnType.typeModifier;

                            if (0 == BinaryCompare(vLeft, vRight, resultType, resultTypeModifier, Operators.EqualTo))
                            {
                                value = true;
                                break;
                            }
                        }
                        break;

                    default:
                        throw new InvalidExpressionException($"Unsupported optcode {op}.");
                }
            }
            catch (OverflowException e)
            {
                throw new InvalidOperationException($"Overflow exception occur for type {CoreDataTable.GetColumnType(resultType, resultTypeModifier, false, null)} and expression {this}.", e);
            }
            
            if (typeMismatch)
            {
                SetTypeMismatchError(op, vLeft, vRight);
            }

            return value;
        }

        private object OpModulo(TableStorageType resultType, object value, object vLeft, object vRight,
            TableStorageTypeModifier resultTypeModifier, ref bool typeMismatch, bool test)
        {
            if (IsInteger(resultType))
            {
                // Convert operands
                var left = ConvertBoxed<ulong>(vLeft, nameof(OpModulo));
                var right = ConvertBoxed<ulong>(vRight, nameof(OpModulo));

                if (test && right == 0UL)
                {
                    // In test mode, avoid divide-by-zero errors and return zero/default
                    value = 0UL;
                }
                else
                {
                    value = left % right;
                }
                
                if (resultType != TableStorageType.UInt64)
                {
                    value = Convert.ChangeType(value, CoreDataTable.GetColumnType(resultType, resultTypeModifier, true, null), FormatProvider);
                }
            }
            else
            {
                typeMismatch = true;
            }

            return value;
        }

        private object OpDivide(TableStorageType resultType, object vLeft, object vRight, ref bool typeMismatch,
            bool test)
        {
            var name = nameof(OpDivide);
            
            switch (resultType)
            {
                case TableStorageType.Byte:
                {
                    var l = ConvertBoxed<byte>(vLeft, name);
                    var r = ConvertBoxed<byte>(vRight, name);
                    if (test && r == 0) return (byte)0;
                    return (byte)(l / r);
                }
                case TableStorageType.SByte:
                {
                    var l = ConvertBoxed<sbyte>(vLeft, name);
                    var r = ConvertBoxed<sbyte>(vRight, name);
                    if (test && r == 0) return (sbyte)0;
                    return (sbyte)(l / r);
                }
                case TableStorageType.Int16:
                {
                    var l = ConvertBoxed<short>(vLeft, name);
                    var r = ConvertBoxed<short>(vRight, name);
                    if (test && r == 0) return (short)0;
                    return (short)(l / r);
                }
                case TableStorageType.UInt16:
                {
                    var l = ConvertBoxed<ushort>(vLeft, name);
                    var r = ConvertBoxed<ushort>(vRight, name);
                    if (test && r == 0) return (ushort)0;
                    return (ushort)(l / r);
                }
                case TableStorageType.Int32:
                {
                    var l = ConvertBoxed<int>(vLeft, name);
                    var r = ConvertBoxed<int>(vRight, name);
                    if (test && r == 0) return 0;
                    checked
                    {
                        return l / r;
                    }
                }
                        
                case TableStorageType.UInt32:
                {
                    var l = ConvertBoxed<uint>(vLeft, name);
                    var r = ConvertBoxed<uint>(vRight, name);
                    if (test && r == 0U) return 0U;
                    checked
                    {
                        return l / r;
                    }
                }
                case TableStorageType.UInt64:
                {
                    var l = ConvertBoxed<ulong>(vLeft, name);
                    var r = ConvertBoxed<ulong>(vRight, name);
                    if (test && r == 0UL) return 0UL;
                    checked
                    {
                        return l / r;
                    }
                }
                case TableStorageType.Int64:
                {
                    var l = ConvertBoxed<long>(vLeft, name);
                    var r = ConvertBoxed<long>(vRight, name);
                    if (test && r == 0L) return 0L;
                    checked
                    {
                        return l / r;
                    }
                }
                          
                case TableStorageType.Decimal:
                {
                    var l = ConvertBoxed<decimal>(vLeft, name);
                    var r = ConvertBoxed<decimal>(vRight, name);
                    if (test && r == 0M) return 0M;
                    checked
                    {
                        return l / r;
                    }
                }
                         
                case TableStorageType.Single:
                {
                    var l = ConvertBoxed<float>(vLeft, name);
                    var r = ConvertBoxed<float>(vRight, name);
                    if (test && r == 0f) return 0f;
                    checked
                    {
                        return l / r;
                    }
                }
                case TableStorageType.Double:
                {
                    var l = ConvertBoxed<double>(vLeft, name);
                    var r = ConvertBoxed<double>(vRight, name);
                    if (test && r == 0d) return 0d;
                    checked
                    {
                        return l / r;
                    }
                }
                default:
                {
                    typeMismatch = true;
                    break;
                }
            }

            return null;
        }

        private object OnMultiply(TableStorageType resultType, object vLeft, object vRight, ref bool typeMismatch,
            bool test)
        {
            var name = nameof(OnMultiply);
            
            switch (resultType)
            {
                case TableStorageType.Byte:
                {
                    var l = ConvertBoxed<byte>(vLeft, name);
                    var r = ConvertBoxed<byte>(vRight, name);
                    if (test)
                    {
                        unchecked { return (byte)(l * r); }
                    }
                    return (byte)(l * r);
                }
                        
                case TableStorageType.SByte:
                {
                    var l = ConvertBoxed<sbyte>(vLeft, name);
                    var r = ConvertBoxed<sbyte>(vRight, name);
                    if (test)
                    {
                        unchecked { return (sbyte)(l * r); }
                    }
                    return (sbyte)(l * r);
                }
                case TableStorageType.Int16:
                {
                    var l = ConvertBoxed<short>(vLeft, name);
                    var r = ConvertBoxed<short>(vRight, name);
                    if (test)
                    {
                        unchecked { return (short)(l * r); }
                    }
                    return (short)(l * r);
                }
                         
                case TableStorageType.UInt16:
                {
                    var l = ConvertBoxed<ushort>(vLeft, name);
                    var r = ConvertBoxed<ushort>(vRight, name);
                    if (test)
                    {
                        unchecked { return (ushort)(l * r); }
                    }
                    return (ushort)(l * r);
                }
                case TableStorageType.Int32:
                {
                    var l = ConvertBoxed<int>(vLeft, name);
                    var r = ConvertBoxed<int>(vRight, name);
                    if (test)
                    {
                        unchecked { return l * r; }
                    }
                    checked
                    {
                        return l * r;
                    }
                }
                         
                case TableStorageType.UInt32:
                {
                    var l = ConvertBoxed<uint>(vLeft, name);
                    var r = ConvertBoxed<uint>(vRight, name);
                    if (test)
                    {
                        unchecked { return l * r; }
                    }
                    checked
                    {
                        return l * r;
                    }
                }
                case TableStorageType.Int64:
                {
                    var l = ConvertBoxed<long>(vLeft, name);
                    var r = ConvertBoxed<long>(vRight, name);
                    if (test)
                    {
                        unchecked { return l * r; }
                    }
                    checked
                    {
                        return l * r;
                    }
                }
                         
                case TableStorageType.UInt64:
                {
                    var l = ConvertBoxed<ulong>(vLeft, name);
                    var r = ConvertBoxed<ulong>(vRight, name);
                    if (test)
                    {
                        unchecked { return l * r; }
                    }
                    checked
                    {
                        return l * r;
                    }
                }
                case TableStorageType.Decimal:
                {
                    var l = ConvertBoxed<decimal>(vLeft, name);
                    var r = ConvertBoxed<decimal>(vRight, name);
                    // Decimal overflow throws; keep checked but we can't unchecked decimal.
                    // Still, ensure arithmetic completes; no special test handling besides performing the operation.
                    checked
                    {
                        return l * r;
                    }
                }
                        
                case TableStorageType.Single:
                {
                    var l = ConvertBoxed<float>(vLeft, name);
                    var r = ConvertBoxed<float>(vRight, name);
                    checked
                    {
                        return l * r;
                    }
                }
                           
                case TableStorageType.Double:
                {
                    var l = ConvertBoxed<double>(vLeft, name);
                    var r = ConvertBoxed<double>(vRight, name);
                    checked
                    {
                        return l * r;
                    }
                }
                         
                default:
                {
                    typeMismatch = true;
                    break;
                }
            }

            return null;
        }

        private object OpMinus(TableStorageType resultType, object vLeft, object vRight, ref bool typeMismatch,
            bool test)
        {
            var name = nameof(OpMinus);
            
            switch (resultType)
            {
                case TableStorageType.Byte:
                {
                    return ConvertBoxed<byte>(vLeft, name) - ConvertBoxed<byte>(vRight, name);
                }
                case TableStorageType.SByte:
                {
                    var vb = ConvertBoxed<sbyte>(vLeft, name);
                    var vr = ConvertBoxed<sbyte>(vRight, name);
                    
                    if (test)
                    {
                        unchecked
                        {
                            return vb - vr;
                        }
                    }

                    return vb - vr;
                }
                case TableStorageType.Int16:
                {
                    return ConvertBoxed<short>(vLeft, name) - ConvertBoxed<short>(vRight, name);
                }
                case TableStorageType.UInt16:
                {
                    var vb = ConvertBoxed<ushort>(vLeft, name);
                    var vr = ConvertBoxed<ushort>(vRight, name);
                    
                    if (test)
                    {
                        unchecked
                        {
                            return vb - vr;
                        }
                    }

                    return vb - vr;
                }
                case TableStorageType.Int32:
                {
                    checked
                    {
                        return ConvertBoxed<int>(vLeft, name) - ConvertBoxed<int>(vRight, name);
                    }
                }
                case TableStorageType.UInt32:
                {
                    checked
                    {
                        var vb = ConvertBoxed<uint>(vLeft, name);
                        var vr = ConvertBoxed<uint>(vRight, name);
                        
                        if (test)
                        {
                            unchecked
                            {
                                return vb - vr;
                            }
                        }

                        return vb - vr;
                    }
                }
                case TableStorageType.Int64:
                {
                    checked
                    {
                        return ConvertBoxed<long>(vLeft, name) - ConvertBoxed<long>(vRight, name);
                    }
                }
                case TableStorageType.UInt64:
                {
                    var vb = ConvertBoxed<ulong>(vLeft, name);
                    var vr = ConvertBoxed<ulong>(vRight, name);
                    
                    if (test)
                    {
                        unchecked
                        {
                            return vb - vr;
                        }
                    }
                    checked
                    {
                        return vb - vr;
                    }
                }
                case TableStorageType.Decimal:
                {
                    checked
                    {
                        return ConvertBoxed<decimal>(vLeft, name) - ConvertBoxed<decimal>(vRight, name);
                    }
                }
                case TableStorageType.Single:
                {
                    checked
                    {
                        return ConvertBoxed<float>(vLeft, name) - ConvertBoxed<float>(vRight, name);
                    }
                }
                case TableStorageType.Double:
                {
                    checked
                    {
                        return ConvertBoxed<double>(vLeft, name) - ConvertBoxed<double>(vRight, name);
                    }
                }
                case TableStorageType.DateTime:
                {
                    return (DateTime)vLeft - (TimeSpan)vRight;
                }
                case TableStorageType.TimeSpan:
                {
                    if (vLeft is DateTime vt)
                    {
                       return vt - (DateTime)vRight;
                    }

                    return (TimeSpan)vLeft - (TimeSpan)vRight;
                }
                default:
                {
                    typeMismatch = true;
                    break;
                }
            }

            return null;
        }

        private object OpPlus(TableStorageType resultType,
            object vLeft,
            object vRight, 
            ref bool typeMismatch,
            bool test)
        {
            switch (resultType)
            {
                case TableStorageType.Byte:
                {
                    return ConvertBoxed<byte>(vLeft, nameof(OpPlus)) + ConvertBoxed<byte>(vRight, nameof(OpPlus));
                }
                case TableStorageType.SByte:
                {
                    return ConvertBoxed<sbyte>(vLeft, nameof(OpPlus)) + ConvertBoxed<sbyte>(vRight, nameof(OpPlus));
                }
                case TableStorageType.Int16:
                {
                    return ConvertBoxed<short>(vLeft, nameof(OpPlus)) + ConvertBoxed<short>(vRight, nameof(OpPlus));
                }
                case TableStorageType.UInt16:
                {
                    return ConvertBoxed<ushort>(vLeft, nameof(OpPlus)) + ConvertBoxed<ushort>(vRight, nameof(OpPlus));
                }
                case TableStorageType.Int32:
                {
                    checked
                    {
                        return ConvertBoxed<int>(vLeft, nameof(OpPlus)) + ConvertBoxed<int>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.UInt32:
                {
                    checked
                    {
                        return ConvertBoxed<uint>(vLeft, nameof(OpPlus)) + ConvertBoxed<uint>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.UInt64:
                {
                    checked
                    {
                        return ConvertBoxed<ulong>(vLeft, nameof(OpPlus)) + ConvertBoxed<ulong>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.Int64:
                {
                    checked
                    {
                        return ConvertBoxed<long>(vLeft, nameof(OpPlus)) + ConvertBoxed<long>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.Decimal:
                {
                    checked
                    {
                        return ConvertBoxed<decimal>(vLeft, nameof(OpPlus)) + ConvertBoxed<decimal>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.Single:
                {
                    checked
                    {
                        return ConvertBoxed<float>(vLeft, nameof(OpPlus)) + ConvertBoxed<float>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.Double:
                {
                    checked
                    {
                        return ConvertBoxed<double>(vLeft, nameof(OpPlus)) + ConvertBoxed<double>(vRight, nameof(OpPlus));
                    }
                }
                case TableStorageType.String:
                case TableStorageType.Char:
                {
                    return CoreDataTable.ConvertObjectToString(vLeft) + CoreDataTable.ConvertObjectToString(vRight);
                }
                case TableStorageType.DateTime:
                {
                    if (vLeft is TimeSpan lt && vRight is DateTime rt)
                    {
                        return rt + lt;
                    }

                    if (vLeft is DateTime vl && vRight is TimeSpan vr)
                    {
                        return vl + vr;
                    }

                    typeMismatch = true;
                    break;
                }
                case TableStorageType.TimeSpan:
                {
                    return (TimeSpan)vLeft + (TimeSpan)vRight;
                }
                default:
                {
                    typeMismatch = true;
                    break;
                }
            }

            return null;
        }

        // Data type precedence rules specify which data type is converted to the other. 
        // The data type with the lower precedence is converted to the data type with the higher precedence. 
        // If the conversion is not a supported implicit conversion, an error is returned.
        // When both operand expressions have the same data type, the result of the operation has that data type. 
        // This is the precedence order for the DataSet numeric data types:

        private enum DataTypePrecedence
        {
            DateTimeOffset = 24,
            DateTime = 23,
            TimeSpan = 20,
            Double = 18,
            Single = 16,
            Decimal = 14,
            UInt64 = 12,
            Int64 = 10,
            UInt32 = 9,
            Int32 = 7,
            UInt16 = 6,
            Int16 = 4,
            Byte = 3,
            SByte = 1,
            Error = 0,
            Boolean = -2,
            String = -5,
            Char = -8,
        }

        private DataTypePrecedence GetPrecedence((TableStorageType type, TableStorageTypeModifier modifier) storageType)
        {
            if (storageType.modifier != TableStorageTypeModifier.Simple)
            {
                return DataTypePrecedence.Error;
            }
            
            switch (storageType.type)
            {
                case TableStorageType.Boolean: return DataTypePrecedence.Boolean;
                case TableStorageType.Char: return DataTypePrecedence.Char;
                case TableStorageType.SByte: return DataTypePrecedence.SByte;
                case TableStorageType.Byte: return DataTypePrecedence.Byte;
                case TableStorageType.Int16: return DataTypePrecedence.Int16;
                case TableStorageType.UInt16: return DataTypePrecedence.UInt16;
                case TableStorageType.Int32: return DataTypePrecedence.Int32;
                case TableStorageType.UInt32: return DataTypePrecedence.UInt32;
                case TableStorageType.Int64: return DataTypePrecedence.Int64;
                case TableStorageType.UInt64: return DataTypePrecedence.UInt64;
                case TableStorageType.Single: return DataTypePrecedence.Single;
                case TableStorageType.Double: return DataTypePrecedence.Double;
                case TableStorageType.Decimal: return DataTypePrecedence.Decimal;
                case TableStorageType.DateTime: return DataTypePrecedence.DateTime;
                case TableStorageType.DateTimeOffset: return DataTypePrecedence.DateTimeOffset;
                case TableStorageType.TimeSpan: return DataTypePrecedence.TimeSpan;
                case TableStorageType.String: return DataTypePrecedence.String;
                case TableStorageType.UserType: return DataTypePrecedence.String;
           
                case TableStorageType.Empty:
                case TableStorageType.Object:
                default: return DataTypePrecedence.Error;
            }
        }

        private static TableStorageType GetPrecedenceType(DataTypePrecedence code)
        {
            switch (code)
            {
                case DataTypePrecedence.Error: return TableStorageType.Empty;
                case DataTypePrecedence.SByte: return TableStorageType.SByte;
                case DataTypePrecedence.Byte: return TableStorageType.Byte;
                case DataTypePrecedence.Int16: return TableStorageType.Int16;
                case DataTypePrecedence.UInt16: return TableStorageType.UInt16;
                case DataTypePrecedence.Int32: return TableStorageType.Int32;
                case DataTypePrecedence.UInt32: return TableStorageType.UInt32;
                case DataTypePrecedence.Int64: return TableStorageType.Int64;
                case DataTypePrecedence.UInt64: return TableStorageType.UInt64;
                case DataTypePrecedence.Decimal: return TableStorageType.Decimal;
                case DataTypePrecedence.Single: return TableStorageType.Single;
                case DataTypePrecedence.Double: return TableStorageType.Double;

                case DataTypePrecedence.Boolean: return TableStorageType.Boolean;
                case DataTypePrecedence.String: return TableStorageType.String;
                case DataTypePrecedence.Char: return TableStorageType.Char;

                case DataTypePrecedence.DateTimeOffset: return TableStorageType.DateTimeOffset;
                case DataTypePrecedence.DateTime: return TableStorageType.DateTime;
                case DataTypePrecedence.TimeSpan: return TableStorageType.TimeSpan;

         
                default:
                    Debug.Assert(false, "Invalid (unmapped) precedence " + code);
                    goto case DataTypePrecedence.Error;
            }
        }

        private bool IsMixed((TableStorageType type, TableStorageTypeModifier modifier) left, 
            (TableStorageType type, TableStorageTypeModifier modifier) right)
        {
            return ((IsSigned(left.type) && IsUnsigned(right.type)) ||
                    (IsUnsigned(left.type) && IsSigned(right.type)));
        }
       

        internal (TableStorageType type, TableStorageTypeModifier modifier) ResultType(
            (TableStorageType type, TableStorageTypeModifier modifier, bool allowNull) left, 
            (TableStorageType type, TableStorageTypeModifier modifier, bool allowNull) right,
            bool lc, 
            bool rc,
            int op)
        {
            if ((left.type == TableStorageType.Guid) && (right.type == TableStorageType.Guid) && Operators.IsRelational(op))
            {
                return (left.type, left.modifier);
            }

            if ((left.type == TableStorageType.String) && (right.type == TableStorageType.Guid) && Operators.IsRelational(op))
            {
                return  (left.type, left.modifier);;
            }

            if ((left.type == TableStorageType.Guid) && (right.type == TableStorageType.String) && Operators.IsRelational(op))
            {
                return (right.type, right.modifier);
            }

            if (op == Operators.EqualTo || op == Operators.NotEqual)
            {
                if (IsNumeric((left.type, left.modifier)) && right.type == TableStorageType.String)
                {
                    return (TableStorageType.Boolean, TableStorageTypeModifier.Simple);
                }

                if (IsNumeric((right.type, right.modifier)) && left.type == TableStorageType.String)
                {
                    return (TableStorageType.Boolean, TableStorageTypeModifier.Simple);
                }
            }

            int leftPrecedence = (int)GetPrecedence((left.type, left.modifier));
            if (leftPrecedence == (int)DataTypePrecedence.Error)
            {
                return (TableStorageType.Empty, TableStorageTypeModifier.Simple);
            }

            int rightPrecedence = (int)GetPrecedence((right.type, right.modifier));
            if (rightPrecedence == (int)DataTypePrecedence.Error)
            {
                return (TableStorageType.Empty, TableStorageTypeModifier.Simple);
            }

            if (Operators.IsLogical(op))
            {
                if (left.type == TableStorageType.Boolean && right.type == TableStorageType.Boolean)
                {
                    return (TableStorageType.Boolean, TableStorageTypeModifier.Simple);
                }
                return (TableStorageType.Empty, TableStorageTypeModifier.Simple);
            }
            if ((left.type == TableStorageType.DateTimeOffset) || (right.type == TableStorageType.DateTimeOffset))
            {
                // Rules to handle DateTimeOffset: 
                // we only allow Relational operations to operate only on DTO vs DTO
                // all other operations: "exception" 
                if (Operators.IsRelational(op) && left.type == TableStorageType.DateTimeOffset && right.type == TableStorageType.DateTimeOffset)
                {
                    return (TableStorageType.DateTimeOffset, TableStorageTypeModifier.Simple);
                }
                return (TableStorageType.Empty, TableStorageTypeModifier.Simple);
            }

            if ((op == Operators.Plus) && ((left.type == TableStorageType.String) || (right.type == TableStorageType.String)))
            {
                return (TableStorageType.String, TableStorageTypeModifier.Simple);
            }

            DataTypePrecedence higherPrec = (DataTypePrecedence)Math.Max(leftPrecedence, rightPrecedence);

            TableStorageType result = GetPrecedenceType(higherPrec);

            if (Operators.IsArithmetical(op))
            {
                if (result != TableStorageType.String && result != TableStorageType.Char)
                {
                    if (!IsNumeric((left.type, left.modifier)))
                    {
                        return (TableStorageType.Empty, TableStorageTypeModifier.Simple);
                    }

                    if (!IsNumeric((right.type, right.modifier)))
                    {
                        return (TableStorageType.Empty, TableStorageTypeModifier.Simple);
                    }
                }
            }

            // if the operation is a division the result should be at least a double 

            if ((op == Operators.Divide) && IsInteger(result))
            {
                return (TableStorageType.Double, TableStorageTypeModifier.Simple);
            }

            if (IsMixed((left.type, left.modifier), (right.type, right.modifier)))
            {
                // we are dealing with one signed and one unsigned type so
                // try to see if one of them is a ConstNode
                if (lc && (!rc))
                {
                    return (right.type, right.modifier);
                }

                if ((!lc) && rc)
                {
                    return (left.type, left.modifier);
                }

                if (IsUnsigned(result))
                {
                    if (higherPrec < DataTypePrecedence.UInt64)
                        // left and right are mixed integers but with the same length 
                        // so promote to the next signed type
                    {
                        result = GetPrecedenceType(higherPrec + 1);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot prepare unsigned {result} result type for {(Operators.ToString(op))} operator left type: {CoreDataTable.GetColumnType(left.type, left.modifier, left.allowNull, null)}, right {CoreDataTable.GetColumnType(right.type, right.modifier, right.allowNull, null)}.");
                    }
                }
            }

            return (result, TableStorageTypeModifier.Simple);
        }
    }
}
