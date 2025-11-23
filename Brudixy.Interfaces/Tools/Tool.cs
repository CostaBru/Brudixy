using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Brudixy.Interfaces;

namespace Brudixy.Converter
{
    public static class Tool
    {
        public static class MetadataTool
        {
            public static bool GetIsRecord(Type classType)
            {
                return classType.GetMethod("<Clone>$") != null;
            }

            public static bool HasAttribute(Type classType, Type attrType)
            {
                return classType.GetCustomAttributes(attrType, inherit: true).Any();
            }

            public static bool GetIsBuiltingImmutable(Type type)
            {
                var genericType = GetGenericType(type);

                if (genericType == typeof(ImmutableArray<>))
                {
                    return true;
                }

                if (genericType == typeof(ImmutableList<>))
                {
                    return true;
                }

                if (genericType == typeof(ImmutableDictionary<,>))
                {
                    return true;
                }

                if (genericType == typeof(ImmutableQueue<>))
                {
                    return true;
                }

                if (genericType == typeof(ImmutableStack<>))
                {
                    return true;
                }


                if (genericType == typeof(ImmutableHashSet<>))
                {
                    return true;
                }

                if (genericType == typeof(ImmutableSortedDictionary<,>))
                {
                    return true;
                }

                if (genericType == typeof(ImmutableSortedSet<>))
                {
                    return true;
                }

                return false;
            }

            public static Type GetGenericType(Type type)
            {
                if (type.IsGenericType)
                {
                    return type.GetGenericTypeDefinition();
                }

                return null;
            }
        }

        static class Metadata<T>
        {
            static public readonly Type NullableType = Nullable.GetUnderlyingType(typeof(T));

            static public readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(T)) != null;
            static public readonly bool IsString = typeof(T) == typeof(string);
            static public readonly bool IsDateTime = typeof(T) == typeof(DateTime);
            static public readonly bool IsNullableDateTime = typeof(T) == typeof(DateTime?);
            static public readonly bool IsEnum = typeof(T).IsEnum;
            static public readonly bool IsObject = typeof(T) == typeof(object);
            static public readonly bool IsValueType = typeof(T).IsValueType;

            static public readonly bool IsReadonlySupported = typeof(T).GetInterface(nameof(IReadonlySupported)) != null;

            static public readonly bool IsCloneableSupported = typeof(T).GetInterface(nameof(ICloneable)) != null;
            static public readonly bool IsArray = typeof(T).IsArray;
            static public readonly bool IsRange = MetadataTool.GetGenericType(typeof(T)) == typeof(Range<>);

            static public readonly bool IsImmutable = Metadata<T>.IsValueType || 
                                                      Metadata<T>.IsString ||
                                                      Metadata<T>.IsEnum || 
                                                      MetadataTool.GetIsRecord(typeof(T)) ||
                                                      MetadataTool.HasAttribute(typeof(T), typeof(ImmutableObjectAttribute)) ||
                                                      MetadataTool.GetIsBuiltingImmutable(typeof(T));
        }

        public static T GetReferenceOrDefault<T>(this WeakReference<T> reference) where T : class
        {
            return reference.TryGetTarget(out var target) ? target : default;
        }

        [DebuggerStepThrough]
        public static bool IsNullable<T>()
        {
            return Metadata<T>.IsNullable;
        }

        [DebuggerStepThrough]
        public static bool IsArray<T>()
        {
            return Metadata<T>.IsArray;
        }
        
        [DebuggerStepThrough]
        public static bool IsRange<T>()
        {
            return Metadata<T>.IsRange;
        }

        [DebuggerStepThrough]
        public static bool IsReadonlySupported<T>()
        {
            return Metadata<T>.IsReadonlySupported;
        }
        
        public static bool IsImmutable<T>()
        {
            return Metadata<T>.IsNullable;
        }

        public static bool IsImmutable(Type type)
        {
            if (type.IsValueType || type.IsEnum)
            {
                return true;
            }
            
            if (type == typeof(string))
            {
                return true;
            }
            
            if (MetadataTool.GetIsRecord(type))
            {
                return true;
            }
            
            if (MetadataTool.HasAttribute(type, typeof(ImmutableObjectAttribute)))
            {
                return true;
            }
            
            if (MetadataTool.GetIsBuiltingImmutable(type))
            {
                return true;
            }

            return false;
        }

        [DebuggerStepThrough]
        public static bool IsCloneableSupported<T>()
        {
            return Metadata<T>.IsCloneableSupported;
        }

        public static T ConvertBoxed<T>(object x)
        {
            if (x is null)
            {
                return default;
            }

            if (x is T xt)
            {
                return xt;
            }

            var type = Metadata<T>.NullableType ?? typeof(T);

            if (type.IsEnum)
            {
                if (x is string asString)
                {
                    return (T)Enum.Parse(type, asString);
                }
            }

            return (T)Convert.ChangeType(x, type);
        }

        public static readonly Type TimeSpanType = typeof(TimeSpan);
        public static readonly Type GuidType = typeof(Guid);
        public static readonly Type UriType = typeof(Uri);
        public static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
        public static readonly Type ObjectType = typeof(object);

        public static object ConvertBoxed(object x, Type type)
        {
            if (x is null)
            {
                return default;
            }

            if (ObjectType == type)
            {
                return x;
            }

            var targetType = Nullable.GetUnderlyingType(type) ?? type;
           
            
            if (targetType == TimeSpanType)
            {
                if (x is Int32 i32)
                {
                    return new TimeSpan(i32);
                }
                if (x is Int64 i64)
                {
                    return new TimeSpan(i64);
                }
                if (x is string str1)
                {
                    return XmlConvert.ToTimeSpan(str1);
                }
            }

            if (x is string value)
            {
                TypeCode typeCode = Type.GetTypeCode(targetType);

                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        if ("1" == value) return true;
                        if ("0" == value) return false;
                        break;
                    case TypeCode.Char:
                        return ((IConvertible)value).ToChar(Tool.GetNumberFormat());
                    case TypeCode.SByte:
                        return ((IConvertible)value).ToSByte(GetNumberFormat());
                    case TypeCode.Byte:
                        return ((IConvertible)value).ToByte(GetNumberFormat());
                    case TypeCode.Int16:
                        return ((IConvertible)value).ToInt16(GetNumberFormat());
                    case TypeCode.UInt16:
                        return ((IConvertible)value).ToUInt16(GetNumberFormat());
                    case TypeCode.Int32:
                        return ((IConvertible)value).ToInt32(GetNumberFormat());
                    case TypeCode.UInt32:
                        return ((IConvertible)value).ToUInt32(GetNumberFormat());
                    case TypeCode.Int64:
                        return ((IConvertible)value).ToInt64(GetNumberFormat());
                    case TypeCode.UInt64:
                        return ((IConvertible)value).ToUInt64(GetNumberFormat());
                    case TypeCode.Single:
                        return ((IConvertible)value).ToSingle(GetNumberFormat());
                    case TypeCode.Double:
                        return ((IConvertible)value).ToDouble(GetNumberFormat());
                    case TypeCode.Decimal:
                        return ((IConvertible)value).ToDecimal(GetNumberFormat());
                    case TypeCode.DateTime:
                        return ((IConvertible)value).ToDateTime(GetNumberFormat());
                    case TypeCode.String:
                        return value;
                    default:
                        if(targetType == TimeSpanType)
                            return XmlConvert.ToTimeSpan(value);
                        if(targetType == GuidType)
                            return new Guid(value);
                        if(targetType == UriType)
                            return new Uri(value);
                        if(targetType == DateTimeOffsetType)
                            return DateTimeOffset.Parse(value);
                        break;
                }
            }
            
            return Convert.ChangeType(x, targetType);
        }
        
        public static bool ArrayDeepEquals(Array x, Array y)
        {
            var ax = x;
            var ay = y;
                    
            if ((ax == null) != (ay == null))
            {
                return false;
            }

            if (ax == null)
            {
                return true;
            }
                    
            if ((ax.Length != ay.Length))
            {
                return false;
            }

            for (int i = 0; i < ax.Length; i++)
            {
                var axv = ax.GetValue(i);
                var ayv = ay.GetValue(i);

                if ((axv == null) != (ayv == null))
                {
                    return false;
                }

                if (axv == null)
                {
                    continue;
                }

                if (axv.Equals(ayv) == false)
                {
                    return false;
                }
            }
                    
            return true;
        }


        public static bool IsString<T>()
        {
            return Metadata<T>.IsString;
        }
        
        public static bool IsDateTime<T>()
        {
            return Metadata<T>.IsDateTime;
        }
        
        public static bool IsNullableDateTime<T>()
        {
            return Metadata<T>.IsNullableDateTime;
        }

        public static bool IsEnum<T>()
        {
            return Metadata<T>.IsEnum;
        }

        public static bool IsObject<T>()
        {
            return Metadata<T>.IsObject;
        }
        
        public static bool IsValueType<T>()
        {
            return Metadata<T>.IsValueType;
        }


        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(this T[] array)
        {
            Array.Clear(array, 0, array.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(ref T var1, ref T var2)
        {
            (var1, var2) = (var2, var1);
        }

        [DebuggerStepThrough]
        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] SingleToArray<T>(this T row)
        {
            return new[] { row };
        }

        public static int GetInt32(this object value, int defaultValue)
        {
            if (value is int i)
                return i;

            if (value == null)
                return defaultValue;

            if (value is bool b)
            {
                return b ? 1 : 0;
            }

            if (value is string sv)
            {
                return IntParseFast(sv, defaultValue);
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        #region Parse Fast


        public static int IntParseFast(string value, int defaultValue, bool throwEx = false)
        {
            var result = 0;
            var positive = true;

            var length = value.Length;

            if (length == 0)
            {
                if (throwEx)
                {
                    throw new InvalidCastException($"Cannot parse {value} into integer value.");
                }

                return defaultValue;
            }

            var start = 0;
            if (length >= 2)
            {
                var letter = value[0];

                if (letter == '-')
                {
                    positive = false;
                    start++;
                }
            }

            for (var i = start; i < value.Length; i++)
            {
                var letter = value[i];

                var i1 = (int)letter;

                var isNumber = i1 >= 48 && i1 <= 57;

                if (isNumber == false)
                {
                    if (char.IsNumber(letter) == false)
                    {
                        if (throwEx)
                        {
                            throw new InvalidCastException($"Cannot parse {value} into integer value.");
                        }

                        return defaultValue;
                    }
                }

                result = 10 * result + (i1 - 48);
            }

            return positive == false ? -result : result;
        }

        public static uint UIntParseFast(string value, uint defaultValue, bool throwEx = false)
        {
            uint result = 0;

            var length = value.Length;

            if (length == 0)
            {
                if (throwEx)
                {
                    throw new InvalidCastException($"Cannot parse {value} into uint value.");
                }

                return defaultValue;
            }

            var start = 0;

            if (length >= 2)
            {
                var letter = value[0];

                if (letter == '-')
                {
                    start++;
                }
            }

            for (var i = start; i < value.Length; i++)
            {
                var letter = value[i];

                var i1 = (int)letter;

                var isNumber = i1 >= 48 && i1 <= 57;

                if (isNumber == false)
                {
                    if (char.IsNumber(letter) == false)
                    {
                        if (throwEx)
                        {
                            throw new InvalidCastException($"Cannot parse {value} into uint value.");
                        }

                        return defaultValue;
                    }
                }

                result = 10 * result + (uint)(i1 - 48);
            }

            return result;
        }

        public static long LongParseFast(string value, long defaultValue, bool throwEx = false)
        {
            long result = 0;
            var positive = true;

            var length = value.Length;

            if (length == 0)
            {
                if (throwEx)
                {
                    throw new InvalidCastException($"Cannot parse {value} into long value.");
                }

                return defaultValue;
            }

            int start = 0;
            if (length >= 2)
            {
                var letter = value[0];

                if (letter == '-')
                {
                    positive = false;
                    start++;
                }
            }

            for (int i = start; i < value.Length; i++)
            {
                var letter = value[i];

                var letter1 = (int)letter;

                var isNumber = letter1 >= 48 && letter1 <= 57;

                if (isNumber == false)
                {
                    if (char.IsNumber(letter) == false)
                    {
                        if (throwEx)
                        {
                            throw new InvalidCastException($"Cannot parse {value} into long value.");
                        }

                        return defaultValue;
                    }
                }

                result = 10 * result + (letter1 - 48);
            }

            return positive == false ? -result : result;
        }

        public static double DoubleParseVeryFast(string value, double defaultValue, bool throwEx = false)
        {
            ulong result1 = 0;
            ulong result2 = 0;

            var positive = true;

            var length = value.Length;

            if (length == 0)
            {
                if (throwEx)
                {
                    throw new InvalidCastException($"Cannot parse {value} into double value.");
                }

                return defaultValue;
            }

            var start = 0;

            if (length >= 2)
            {
                var _letter = value[0];

                if (_letter == '-')
                {
                    start++;
                    positive = false;
                }
            }

            var getInt = true;
            var precision = 0;
            var eqnt = 0;

            for (var i = start; i < value.Length; i++)
            {
                var letter = value[i];

                if (getInt && letter == '.')
                {
                    getInt = false;
                    continue;
                }

                var letter1 = (int)letter;

                var isNumber = letter1 >= 48 && letter1 <= 57;

                if (isNumber == false)
                {
                    if (char.IsNumber(letter) == false)
                    {
                        if (letter == 'E' && i + 3 < length)
                        {
                            eqnt = IntParseFast(new string(new[] { value[i + 2], value[i + 3] }), 0);
                            break;
                        }

                        if (throwEx)
                        {
                            throw new InvalidCastException($"Cannot parse {value} into double value.");
                        }

                        return defaultValue;
                    }
                }

                if (getInt)
                {
                    result1 = 10 * result1 + (ulong)(letter1 - 48);
                }
                else
                {
                    result2 = 10 * result2 + (ulong)(letter1 - 48);
                    precision++;
                }
            }

            if (eqnt > 0)
            {
                var delta = getDelta(eqnt);

                if (getInt)
                {
                    return positive ? delta * result1 : -delta * result1;
                }

                var result = result1 + getDelta(precision) * result2 + 0.0000000000000000000000000001;

                return positive ? delta * result : -delta * result;
            }
            else
            {
                if (getInt)
                {
                    return positive ? result1 : -(double)result1;
                }

                var result = result1 + getDelta(precision) * result2 + 0.0000000000000000000000000001;

                return positive ? result : -result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumber(char c)
        {
            if (c >= 48 && c <= 57)
            {
                return true;
            }

            return Char.IsNumber(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double getDelta(int precision)
        {
            switch (precision)
            {
                case 1:
                    return 0.1;
                case 2:
                    return 0.01;
                case 3:
                    return 0.001;
                case 4:
                    return 0.0001;
                case 5:
                    return 0.00001;
                case 6:
                    return 0.000001;
                case 7:
                    return 0.0000001;
                case 8:
                    return 0.00000001;
                case 9:
                    return 0.000000001;
                case 10:
                    return 0.0000000001;
                case 11:
                    return 0.00000000001;
                case 12:
                    return 0.000000000001;
                case 13:
                    return 0.0000000000001;
                case 14:
                    return 0.00000000000001;
                case 15:
                    return 0.000000000000001;
                case 16:
                    return 0.0000000000000001;
                case 17:
                    return 0.00000000000000001;
                case 18:
                    return 0.000000000000000001;
                case 19:
                    return 0.0000000000000000001;
                case 20:
                    return 0.00000000000000000001;
                case 21:
                    return 0.000000000000000000001;
                case 22:
                    return 0.0000000000000000000001;
                case 23:
                    return 0.00000000000000000000001;
                case 24:
                    return 0.000000000000000000000001;
                case 25:
                    return 0.0000000000000000000000001;
                case 26:
                    return 0.00000000000000000000000001;
                case 27:
                    return 0.000000000000000000000000001;
                case 28:
                    return 0.0000000000000000000000000001;
                default:
                    return Math.Pow(0.1, precision);
            }
        }

        public static ulong ULongParseFast(string value, ulong defaultValue, bool throwEx = false)
        {
            ulong result = 0;

            var valueLength = value.Length;

            if (valueLength == 0)
            {
                if (throwEx)
                {
                    throw new InvalidCastException($"Cannot parse {value} into ulong value.");
                }

                return defaultValue;
            }

            var start = 0;
            if (valueLength >= 2)
            {
                var letter = value[0];

                if (letter == '-')
                {
                    start++;
                }
            }

            for (var i = start; i < valueLength; i++)
            {
                var letter = value[i];
                var i1 = (int)letter;

                var isNumber = i1 >= 48 && i1 <= 57;

                if (isNumber == false)
                {
                    if (char.IsNumber(letter) == false)
                    {
                        if (throwEx)
                        {
                            throw new InvalidCastException($"Cannot parse {value} into ulong value.");
                        }

                        return defaultValue;
                    }
                }

                result = 10 * result + (ulong)(letter - 48);
            }

            return result;
        }

        #endregion


        public static string GetDump(this object obj)
        {
            return getDump(obj, 0);
        }

        private static string getDump(object obj, int recursion)
        {
            const string spaces = "|   ";
            const string trail = "|...";

            if (recursion < 7)
            {
                var res = new StringBuilder();

                var type = obj.GetType();

                var str = obj.ToString();

                if (str != obj.ToString())
                {
                    return str;
                }

                if (type != typeof(DateTime) && type != typeof(Color) && type != typeof(TimeSpan))
                {
                    var props = type.GetProperties();

                    foreach (var property in props)
                    {
                        try
                        {
                            var value = property.GetValue(obj, null);

                            if (value is Type tp)
                            {
                                return tp.ToString();
                            }
                            
                            var indent = String.Empty;

                            if (recursion > 0)
                            {
                                indent = new StringBuilder(trail).Insert(0, spaces, recursion - 1).ToString();
                            }

                            if (value is string s)
                            {
                                res.AppendFormat("{0}{1} = '{2}'\n", indent, property.Name, s);

                                continue;
                            }

                            if (value != null)
                            {
                                var displayValue = value.ToString();

                                if (value is string)
                                {
                                    displayValue = String.Concat('"', displayValue, '"');
                                }

                                res.AppendFormat("{0}{1} = {2}\n", indent, property.Name, displayValue);

                                try
                                {
                                    if (value is IEnumerable en)
                                    {
                                        var elementCount = 0;
                                        foreach (var element in en)
                                        {
                                            var name = $"{property.Name}[{elementCount}]";
                                            indent = new StringBuilder(trail).Insert(0, spaces, recursion).ToString();
                                            res.AppendFormat("{0}{1} = {2}\n", indent, name,
                                                element == null ? "NULL" : element.ToString());
                                            res.Append(getDump(element, recursion + 2));
                                            elementCount++;
                                        }

                                        res.Append(getDump(value, recursion + 1));
                                    }
                                    else
                                    {
                                        res.Append(getDump(value, recursion + 1));
                                    }
                                }
                                catch
                                {
                                }
                            }
                            else
                            {
                                res.AppendFormat("{0}{1} = {2}\n", indent, property.Name, "null");
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                return res.ToString();
            }

            return "...MORE...";
        }

        private static char[] trimzeros = { '0' };

        private static char[] trimzerosdot = { '.' };

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

        public static string DoubleToString(object value, string format)
        {
            var str = ((double)value).ToString(format, mNumberinfo ?? GetNumberFormat());
            if (str.IndexOf('.') < 0)
            {
                return str;
            }

            if (str.IndexOf('E') >= 0)
            {
                return str;
            }

            return str.TrimEnd(trimzeros).TrimEnd(trimzerosdot);
        }

        public static string DecimalToString(object value, string format)
        {
            var str = ((decimal)value).ToString(format, mNumberinfo ?? GetNumberFormat());
            if (str.IndexOf('.') < 0)
            {
                return str;
            }

            if (str.IndexOf('E') >= 0)
            {
                return str;
            }

            return str.TrimEnd(trimzeros).TrimEnd(trimzerosdot);
        }

        public static string FloatToString(object value, string format)
        {
            var str = ((float)value).ToString(format, mNumberinfo ?? GetNumberFormat());
            if (str.IndexOf('.') < 0)
            {
                return str;
            }

            if (str.IndexOf('E') >= 0)
            {
                return str;
            }

            return str.TrimEnd(trimzeros).TrimEnd(trimzerosdot);
        }

        public static bool ArraysDeepEqual<TSource>(TSource[] first, TSource[] second,
            Func<TSource, TSource, bool> comparison)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            for (int i = 0; i < first.Length && i < second.Length; i++)
            {
                if (!comparison(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}