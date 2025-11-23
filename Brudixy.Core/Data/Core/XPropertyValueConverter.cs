using System;
using Brudixy.Converter;

namespace Brudixy
{
    public static class XPropertyValueConverter
    {
        public static T TryConvert<T>(string source, string xPropName, object value)
        {
            if (value is T tVal)
            {
                return tVal;
            }
            
            if (value is null)
            {
                return TypeConvertor.ReturnDefault<T>();
            }
            
            var tableStorageType = CoreDataTable.GetColumnType(value.GetType());

            return TypeConvertor.ConvertValue<T>(value, xPropName, source, tableStorageType.type, tableStorageType.typeModifier, "XProperty value convertor");
        }
    }
}