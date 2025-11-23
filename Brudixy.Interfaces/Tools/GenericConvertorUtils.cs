using System;
using System.Globalization;
using System.Reflection.Emit;
using Brudixy.Converter;

namespace Brudixy
{
    public static class GenericConvertorUtils<T>
    {
        internal static class ToGenericConverterHolder<W>
        {
            internal static readonly Func<T, W> Value = EmitConverter();

            private static Func<T, W> EmitConverter()
            {
                var type = typeof(T);
                var targetType =  typeof(W);

                var diffTypes = type != targetType;
                
                if (diffTypes && IfNotConvertableNumber(type))
                {
                    return x => default;
                }

                var method = new DynamicMethod(string.Empty, typeof(W), new[] { type });
                var il = method.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                
                if (diffTypes)
                {
                    switch (Type.GetTypeCode(typeof(W)))
                    {
                        case TypeCode.Double:  il.Emit(OpCodes.Conv_R8); break;
                        case TypeCode.Decimal:

                            switch (Type.GetTypeCode(typeof(T)))
                            {
                                case TypeCode.Int32:
                                case TypeCode.Int64:
                                case TypeCode.Double:
                                case TypeCode.Single:
                                case TypeCode.UInt32:
                                case TypeCode.UInt64:
                                    il.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(T) }));
                                    break;
                                default:
                                    il.Emit(OpCodes.Conv_I4); 
                                    il.Emit(OpCodes.Newobj, typeof(Decimal).GetConstructor(new Type[] { typeof(int) }));
                                    break;
                            }
                            break;
                           
                        case TypeCode.Single:  il.Emit(OpCodes.Conv_R4); break;
                        case TypeCode.Int32:   il.Emit(OpCodes.Conv_I4);  break;
                        case TypeCode.Int16:   il.Emit(OpCodes.Conv_I2);  break;
                        case TypeCode.Int64:   il.Emit(OpCodes.Conv_I8);  break;
                        case TypeCode.SByte:   il.Emit(OpCodes.Conv_I1);  break;
                        case TypeCode.Byte:    il.Emit(OpCodes.Conv_U1);  break;
                        case TypeCode.Boolean: il.Emit(OpCodes.Conv_I1);  break; // bool is an int8 on the stack
                        case TypeCode.UInt32:  il.Emit(OpCodes.Conv_U4);  break;
                        case TypeCode.UInt16:  il.Emit(OpCodes.Conv_U2);  break;
                        case TypeCode.UInt64:  il.Emit(OpCodes.Conv_U8);  break;
                    }
                }
                il.Emit(OpCodes.Ret);

                return (Func<T, W>)method.CreateDelegate(typeof(Func<T, W>));
            }

            private static bool IfNotConvertableNumber(Type type)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.Int32:
                    case TypeCode.Int16:
                    case TypeCode.Int64:
                    case TypeCode.Decimal:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Boolean:
                    case TypeCode.UInt32:
                    case TypeCode.UInt16:
                    case TypeCode.UInt64:
                        return false;
                }

                return true;
            }
        }
    }
    
    public static class GenericConverter
    {
        internal static NumberFormatInfo mNumberinfo;

        public static NumberFormatInfo GetNumberFormat()
        {
            if (mNumberinfo != null)
            {
                return mNumberinfo;
            }

            mNumberinfo = new NumberFormatInfo();
            mNumberinfo.NumberDecimalSeparator = ".";
            mNumberinfo.PositiveSign = "+";
            mNumberinfo.NegativeSign = "-";

            return mNumberinfo;
        }
        
        public static void ConvertTo<T, W>(ref T value, ref W rez)
        {
            var isNullableT = Tool.IsNullable<T>();
            var isNullableW = Tool.IsNullable<W>();
            
            if (isNullableT || isNullableW)
            {
                var convertBoxed = Tool.ConvertBoxed(value, typeof(W));

                if (convertBoxed != null)
                {
                    rez = (W)(object)convertBoxed;
                }
                
                return;
            }
            
            //todo dict: key type value delegate
            switch (value)
            {
                case string str:
                    switch (Type.GetTypeCode(typeof(W)))
                    {
                        case TypeCode.Double:
                            rez = GenericConvertorUtils<double>.ToGenericConverterHolder<W>.Value(Tool.DoubleParseVeryFast(str, 0d, throwEx: true));
                            break;
                        case TypeCode.Single:
                            rez = GenericConvertorUtils<float>.ToGenericConverterHolder<W>.Value((float)Tool.DoubleParseVeryFast(str, 0d, throwEx: true));
                            break;
                        case TypeCode.Int32:
                            rez = GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(Tool.IntParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.Int16:
                            rez = GenericConvertorUtils<short>.ToGenericConverterHolder<W>.Value((short)Tool.IntParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.Int64:
                            rez = GenericConvertorUtils<long>.ToGenericConverterHolder<W>.Value((short)Tool.LongParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.Decimal:
                            rez = GenericConvertorUtils<decimal>.ToGenericConverterHolder<W>.Value(Decimal.Parse(str, mNumberinfo ?? GetNumberFormat()));
                            break;
                        case TypeCode.SByte:
                            rez = GenericConvertorUtils<sbyte>.ToGenericConverterHolder<W>.Value((sbyte)Tool.IntParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.Byte:
                            rez = GenericConvertorUtils<byte>.ToGenericConverterHolder<W>.Value((byte)Tool.UIntParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.Boolean:
                            rez = str == "1" || Convert.ToBoolean(str)  ? GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(1) : GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(0);
                            break;
                        case TypeCode.UInt32:
                            rez = GenericConvertorUtils<uint>.ToGenericConverterHolder<W>.Value(Tool.UIntParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.UInt16:
                            rez = GenericConvertorUtils<ushort>.ToGenericConverterHolder<W>.Value((ushort)Tool.UIntParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.UInt64:
                            rez = GenericConvertorUtils<ulong>.ToGenericConverterHolder<W>.Value(Tool.ULongParseFast(str, 0, throwEx: true));
                            break;
                        case TypeCode.DateTime:
                            rez = GenericConvertorUtils<DateTime>.ToGenericConverterHolder<W>.Value(DateTime.Parse(str));
                            break;
                    }
                    break;
                case decimal d:
                    switch (Type.GetTypeCode(typeof(W)))
                    {
                        case TypeCode.Double:
                            rez = GenericConvertorUtils<double>.ToGenericConverterHolder<W>.Value(Convert.ToDouble(d));
                            break;
                        case TypeCode.Single:
                            rez = GenericConvertorUtils<float>.ToGenericConverterHolder<W>.Value(Convert.ToSingle(d));
                            break;
                        case TypeCode.Int32:
                            rez = GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(Convert.ToInt32(d));
                            break;
                        case TypeCode.Int16:
                            rez = GenericConvertorUtils<short>.ToGenericConverterHolder<W>.Value(Convert.ToInt16(d));
                            break;
                        case TypeCode.Int64:
                            rez = GenericConvertorUtils<long>.ToGenericConverterHolder<W>.Value(Convert.ToInt64(d));
                            break;
                        case TypeCode.Decimal:
                            rez = GenericConvertorUtils<decimal>.ToGenericConverterHolder<W>.Value(d);
                            break;
                        case TypeCode.SByte:
                            rez = GenericConvertorUtils<sbyte>.ToGenericConverterHolder<W>.Value(Convert.ToSByte(d));
                            break;
                        case TypeCode.Byte:
                            rez = GenericConvertorUtils<byte>.ToGenericConverterHolder<W>.Value(Convert.ToByte(d));
                            break;
                        case TypeCode.Boolean:
                            rez = Convert.ToBoolean(d) ? GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(1) : GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(0);
                            break;
                        case TypeCode.UInt32:
                            rez = GenericConvertorUtils<uint>.ToGenericConverterHolder<W>.Value(Convert.ToUInt32(d));
                            break;
                        case TypeCode.UInt16:
                            rez = GenericConvertorUtils<ushort>.ToGenericConverterHolder<W>.Value(Convert.ToUInt16(d));
                            break;
                        case TypeCode.UInt64:
                            rez = GenericConvertorUtils<ulong>.ToGenericConverterHolder<W>.Value(Convert.ToUInt64(d));
                            break;
                    }
                    break;
                case bool b when rez is decimal:
                    rez = b ? GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(1) : GenericConvertorUtils<int>.ToGenericConverterHolder<W>.Value(0);
                    break;
                case long ldt when rez is TimeSpan:
                    rez = GenericConvertorUtils<TimeSpan>.ToGenericConverterHolder<W>.Value(TimeSpan.FromTicks(ldt));
                    break;
                case double ddt when rez is TimeSpan:
                    rez = GenericConvertorUtils<TimeSpan>.ToGenericConverterHolder<W>.Value(TimeSpan.FromTicks((long)ddt));
                    break;
                case long ldt when rez is DateTime:
                    rez = GenericConvertorUtils<DateTime>.ToGenericConverterHolder<W>.Value(DateTime.SpecifyKind(new DateTime(ldt), DateTimeKind.Utc));
                    break;
                case double ddt when rez is DateTime:
                    rez = GenericConvertorUtils<DateTime>.ToGenericConverterHolder<W>.Value(DateTime.SpecifyKind(new DateTime((long)ddt), DateTimeKind.Utc));
                    break;
                case long ldt when rez is DateTimeOffset:
                    rez = GenericConvertorUtils<DateTimeOffset>.ToGenericConverterHolder<W>.Value(new DateTimeOffset(DateTime.SpecifyKind(new DateTime((long)ldt), DateTimeKind.Utc)));
                    break;
                case double ddt when rez is DateTimeOffset:
                    rez = GenericConvertorUtils<DateTimeOffset>.ToGenericConverterHolder<W>.Value(new DateTimeOffset(DateTime.SpecifyKind(new DateTime((long)ddt), DateTimeKind.Utc)));
                    break;
                case TimeSpan dt when rez is double or long:
                    rez = GenericConvertorUtils<long>.ToGenericConverterHolder<W>.Value(dt.Ticks);
                    break;
                case DateTime dt when rez is double or long:
                    rez = GenericConvertorUtils<long>.ToGenericConverterHolder<W>.Value(dt.ToUniversalTime().Ticks);
                    break;
                case DateTimeOffset dt when rez is double or long:
                    rez = GenericConvertorUtils<long>.ToGenericConverterHolder<W>.Value(dt.UtcDateTime.Ticks);
                    break;
                default:
                    rez = GenericConvertorUtils<T>.ToGenericConverterHolder<W>.Value(value);
                    break;
            }
        }
    }
}