using System.Globalization;
using Brudixy.Converter;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class ConstNode : ExpressionNode
    {
        internal readonly object m_val;

        internal ConstNode(IExpressionDataSource table, ValueType type, object constant)
            : this(table, type, constant, true)
        {
        }

        internal ConstNode(IExpressionDataSource table, ValueType type, object constant, bool fParseQuotes)
            : base(table)
        {
            switch (type)
            {
                case ValueType.Null:
                    m_val = null;
                    break;
                case ValueType.Bool:
                    m_val = Convert.ToBoolean(constant, CultureInfo.InvariantCulture) ? 1 : 0;
                    break;
                case ValueType.Numeric:
                    m_val = SmallestNumeric(constant);
                    break;
                case ValueType.Str:
                    if (fParseQuotes)
                    {
                        m_val = ((string)constant).Replace("''", "'");
                        break;
                    }

                    m_val = (string)constant;
                    break;
                case ValueType.Float:
                    m_val = Convert.ToDouble(constant, NumberFormatInfo.InvariantInfo);
                    break;
                case ValueType.Decimal:
                    m_val = SmallestDecimal(constant);
                    break;
                case ValueType.Date:
                    m_val = DateTime.SpecifyKind(
                        DateTime.Parse((string)constant, CultureInfo.InvariantCulture),
                        DateTimeKind.Unspecified);
                    break;
                default:
                    m_val = constant;
                    break;
            }
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            BindTable(table);
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null)
        {
            return m_val;
        }

        internal override object Eval(Data<int> recordNos)
        {
            return m_val;
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

        private object SmallestDecimal(object constant)
        {
            if (constant == null)
            {
                return 0.0;
            }

            if (constant is string s)
            {
                if (Decimal.TryParse(s, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var result1))
                {
                    return result1;
                }
                
                var d1 = Tool.DoubleParseVeryFast(s, Double.MinValue);
                
                if (d1 != Double.MinValue)
                {
                    return d1;
                }

                if (s == "-1.79769313486232E+308")
                {
                    return d1;
                }
            }
            else
            {
                if (constant is IConvertible convertible)
                {
                    try
                    {
                        return convertible.ToDecimal(NumberFormatInfo.InvariantInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (FormatException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (InvalidCastException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (OverflowException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }

                    try
                    {
                        return convertible.ToDouble(NumberFormatInfo.InvariantInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (FormatException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (InvalidCastException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (OverflowException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                }
            }

            return constant;
        }

        private object SmallestNumeric(object constant)
        {
            if (constant is null or "")
            {
                return 0;
            }

            if (constant is string s)
            {
                var intMinVal = "-2147483648";

                if (s.TrimStart('-').Length < intMinVal.Length)
                {
                    var int1 = Tool.IntParseFast(s, int.MinValue);
                
                    if (int1 != int.MinValue)
                    {
                        return int1;
                    }
                }

                var l1 = Tool.LongParseFast(s, long.MinValue);
                
                if (l1 != long.MinValue)
                {
                    return l1;
                }
                
                if (s == "-9223372036854775808")
                {
                    return l1;
                }

                var d1 = Tool.DoubleParseVeryFast(s, Double.MinValue);
                
                if (d1 != Double.MinValue)
                {
                    return d1;
                }

                if (s == "-1.79769313486232E+308")
                {
                    return d1;
                }
            }
            else
            {
                if (constant is IConvertible convertible)
                {
                    try
                    {
                        return convertible.ToInt32(NumberFormatInfo.InvariantInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (FormatException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (InvalidCastException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (OverflowException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }

                    try
                    {
                        return convertible.ToInt64(NumberFormatInfo.InvariantInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (FormatException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (InvalidCastException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (OverflowException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }

                    try
                    {
                        return convertible.ToDouble(NumberFormatInfo.InvariantInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (FormatException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (InvalidCastException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                    catch (OverflowException ex)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(ex);
                    }
                }
            }

            return constant;
        }
    }
}