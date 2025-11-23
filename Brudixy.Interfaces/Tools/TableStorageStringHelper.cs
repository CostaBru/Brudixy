using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Brudixy.Converter;

namespace Brudixy.Interfaces.Tools
{
    public static class TableStorageStringHelper
    {
        public static readonly
            ConcurrentDictionary<Type, (Func<object, string> convertToString, Func<string, object> convertFromString)>
            StringParsingRegistry = new();

        public static string ConvertToString(object val, TableStorageType storageType,
            TableStorageTypeModifier typeModifier)
        {
            if (val == null) return string.Empty;

            var type = val.GetType();
            
            if (StringParsingRegistry.TryGetValue(type, out var parsingSupport))
            {
                return parsingSupport.convertToString(val);
            }

            if (type.IsEnum)
            {
                return val.ToString();
            }

            if (typeModifier == TableStorageTypeModifier.Complex || typeModifier == TableStorageTypeModifier.Simple &&
                storageType == TableStorageType.UserType)
            {
                if (val is IXmlSerializable xml)
                {
                    return xml.ToXml().ToString();
                }

                if (val is IJsonSerializable json)
                {
                    return json.ToJson().ToString();
                }

                throw new NotSupportedException(
                    $"Conversion of type {type.FullName} to parsable string is not registered in DataTable.");
            }

            if (typeModifier == TableStorageTypeModifier.Simple)
            {
                return TableStorageTypeStringConvertor.ConvertToString(val, storageType);
            }

            if (typeModifier == TableStorageTypeModifier.Array)
            {
                var array = (Array)val;

                return string.Join("|",
                    Enumerable.Range(0, array.Length).Select(i =>
                        TableStorageTypeStringConvertor.ConvertToString(array.GetValue(i), storageType)));
            }

            if (typeModifier == TableStorageTypeModifier.Range)
            {
                var range = (IRange)val;

                return
                    $"{TableStorageTypeStringConvertor.ConvertToString(range.Minimum, storageType)}|{TableStorageTypeStringConvertor.ConvertToString(range.Maximum, storageType)}";
            }

            throw new NotSupportedException(
                $"Converting type {storageType}\\{typeModifier}\\{type.FullName} to parsable string is not supported.");
        }


        public static object ConvertStringToObject(TableStorageType storageType, 
            TableStorageTypeModifier typeModifier,
            string value, 
            Type type)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (type != null)
            {
                var nullableSupportType = Nullable.GetUnderlyingType(type) ?? type;

                if (nullableSupportType.IsEnum)
                {
                    return Enum.Parse(nullableSupportType, value);
                }
                 
                if (StringParsingRegistry.TryGetValue(type, out var parsingSupport))
                {
                    return parsingSupport.convertFromString(value);
                }
            }
            
            if (typeModifier == TableStorageTypeModifier.Complex || typeModifier == TableStorageTypeModifier.Simple &&
                storageType == TableStorageType.UserType)
            {
                if (type == null)
                {
                    throw new NotSupportedException($"User type was not found and cannot be restore from parable string {storageType}\\{typeModifier}. Value: '{value}'.");
                }
                
                var val = Activator.CreateInstance(type);

                if (val is IXmlSerializable xml)
                {
                    xml.FromXml(XElement.Parse(value));
                    return val;
                }

                if (val is IJsonSerializable json)
                {
                    json.FromJson(JElement.Parse(value));
                    return val;
                }

                return Tool.ConvertBoxed(value, type);
            }
            
            if (typeModifier == TableStorageTypeModifier.Simple)
            {
                return TableStorageTypeStringConvertor.ConvertFromString(value, storageType);
            }

            if (typeModifier == TableStorageTypeModifier.Array)
            {
                var strings = value.Split("|");

                var array = Array.CreateInstance(type.GetElementType(), strings.Length);

                for (var index = 0; index < strings.Length; index++)
                {
                    var str = strings[index];
                    var valueFromStr = TableStorageTypeStringConvertor.ConvertFromString(str, storageType);

                    array.SetValue(valueFromStr, index);
                }

                return array;
            }

            if (typeModifier == TableStorageTypeModifier.Range)
            {
                var strings = value.Split("|");

                var min = TableStorageTypeStringConvertor.ConvertFromString(strings[0], storageType);
                var max = TableStorageTypeStringConvertor.ConvertFromString(strings[1], storageType);

                var instance = (IRange)Activator.CreateInstance(type);

                instance.Minimum = (IComparable)min;
                instance.Maximum = (IComparable)max;

                return instance;
            }

            throw new NotSupportedException(
                $"Converting type {storageType}\\{typeModifier}\\{type.FullName} from parsable string is not supported.");
        }
    }
}