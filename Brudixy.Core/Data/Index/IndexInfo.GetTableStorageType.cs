using System;

namespace Brudixy
{
    partial class IndexInfo
    {
        [System.CodeDom.Compiler.GeneratedCodeAttribute("Brudixy.Generators", "1.0")]
        public static TableStorageType? GetTableStorageType(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Object:
                    return TableStorageType.Object;
                case TypeCode.Boolean:
                    return TableStorageType.Boolean;
                case TypeCode.Char:
                    return TableStorageType.Char;
                case TypeCode.SByte:
                    return TableStorageType.SByte;
                case TypeCode.Byte:
                    return TableStorageType.Byte;
                case TypeCode.Int16:
                    return TableStorageType.Int16;
                case TypeCode.UInt16:
                    return TableStorageType.UInt16;
                case TypeCode.Int32:
                    return TableStorageType.Int32;
                case TypeCode.UInt32:
                    return TableStorageType.UInt32;
                case TypeCode.Int64:
                    return TableStorageType.Int64;
                case TypeCode.UInt64:
                    return TableStorageType.UInt64;
                case TypeCode.Single:
                    return TableStorageType.Single;
                case TypeCode.Double:
                    return TableStorageType.Double;
                case TypeCode.Decimal:
                    return TableStorageType.Decimal;
                case TypeCode.DateTime:
                    return TableStorageType.DateTime;
                case TypeCode.String:
                    return TableStorageType.String;

                default:
                    return null;
            }
        }
    }
}