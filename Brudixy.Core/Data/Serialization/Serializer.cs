using System;
using System.Collections.Generic;
using System.Xml;
using Konsarpoo.Collections;

namespace Brudixy
{
    public static class Serializer
    {
        public static readonly Version Version = new(1, 0);

        internal static void CheckFormatIsBackwardCompatible(string value, string tableName, string format)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    $"Cannot load data from {format} to {tableName} table due to missing a format version attribute.");
            }

            if (Version.TryParse(value, out var ver) == false)
            {
                throw new FormatException(
                    $"Cannot load data from {format} to {tableName} table due to '{ver}' invalid format of version written in attribute.");
            }

            if (ver.Major > Version.Major)
            {
                throw new NotSupportedException(
                    $"Cannot load data from {format} to {tableName} table due to a backward format capability issue. Format version is '{ver}', but supported version is '{Version}'.");
            }
        }
        
        internal static RelationSerializer<T, V> RelationSerializer<T, V>(SerializerAdapter<T, V> serializer)
        {
            return new RelationSerializer<T, V>(serializer);
        }
        
        internal static DataSetSerializer<T, V> DatasetSerializer<T, V>(CoreDataTable ds, SerializerAdapter<T, V> serializer)
        {
            return new DataSetSerializer<T, V>(ds, serializer);
        }
        
        public static object DeserializeXPropValue<T, V>(SerializerAdapter<T, V> serializer, 
            string table,
            string tableNamespace,
            T xPropElement,
            string xPropValue, 
            string xPropName)
        {
            var value = serializer.GetAttributeValue(xPropElement, "T");
            var valueModifier = serializer.GetAttributeValue(xPropElement, "TM");

            object xPropTableValue = xPropValue;

            if (string.IsNullOrEmpty(value) == false)
            {
                var storageType = Enum.Parse<TableStorageType>(value);
                Enum.TryParse<TableStorageTypeModifier>(valueModifier, out var storageTypeModifier);

                if (storageType == TableStorageType.String)
                {
                    var enumType = serializer.GetAttributeValue(xPropElement, "EnumType");
                    if (string.IsNullOrEmpty(enumType) == false)
                    {
                        var enumTypeVal = Type.GetType(enumType);

                        if (enumTypeVal != null)
                        {
                            xPropTableValue = Enum.Parse(enumTypeVal, value);
                        }
                    }
                }
                else if (storageType == TableStorageType.UserType || storageTypeModifier == TableStorageTypeModifier.Complex)
                {
                    var typeNameValue = serializer.GetAttributeValue(xPropElement, "DataType");
                    
                    var dataType = RestoreUserType(table, tableNamespace, xPropName, typeNameValue);

                    if (dataType != null)
                    {
                        var instance = CoreDataTable.ConvertStringToObject(storageType, storageTypeModifier, xPropValue, dataType, CoreDataTable.SerializationType.Xml);

                        xPropTableValue = instance;
                    }
                }
                else
                {
                    xPropTableValue = CoreDataTable.ConvertStringToObject(storageType, storageTypeModifier, xPropValue);
                }
            }

            return xPropTableValue;
        }
        
        public static Type RestoreUserType(string tableName,
            string tableNamespace,
            string columnName,
            string typeNameValue)
        {
            var typeName = typeNameValue;

            var dataType = Type.GetType(typeName) ?? CoreDataTable.UserTypeRegistry.GetOrDefault(typeName);

            if (dataType == null)
            {
                var resolveComplexTypeEventArgs = new CoreDataTable.ResolveUserTypeEventArgs()
                {
                    ColumnOrXPropertyName = columnName,
                    Name = tableName,
                    NameSpace = tableNamespace,
                    TypeFullName = typeName,
                };

                CoreDataTable.OnOnResolveUserType(resolveComplexTypeEventArgs);

                if (resolveComplexTypeEventArgs.Type == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot deserialize user type for '{tableName}' and column '{columnName}'. (Type '{typeName}''). Please use the DataTable.OnResolveUserType event to resolve this type or register it using DataTable.RegisterUserType.");
                }

                dataType = resolveComplexTypeEventArgs.Type;
            }

            return dataType;
        }
        
        internal static void WriteXProperties<T, V>(SerializerAdapter<T, V> serializer, T col, string xName, IReadOnlyDictionary<string, object> dict)
        {
            if (dict.Count > 0)
            {
                var xPropsItem = serializer.CreateElement(xName);

                foreach (var kv in dict)
                {
                    if (kv.Value is null)
                    {
                        continue;
                    }

                    if (kv.Value is string str)
                    {
                        var xItem = serializer.CreateElement(XmlConvert.EncodeName(kv.Key), str);
                        
                        serializer.AppendElement(xPropsItem, xItem);
                    }
                    else
                    {
                        var xItem = SerializeNonString(serializer, kv.Key, kv.Value);

                        serializer.AppendElement(xPropsItem, xItem);
                    }
                }

                serializer.AppendElement(col, xPropsItem);
            }
        }

        public static T SerializeNonString<T, V>(SerializerAdapter<T, V> serializer, string key, object value)
        {
            var type = value.GetType();

            var storageType = CoreDataTable.GetColumnType(type);

            string strValue = string.Empty;
            
            if (storageType.typeModifier == TableStorageTypeModifier.Complex)
            {
                strValue = serializer.WriteUserType(storageType.type, storageType.typeModifier, value);
            }
            else
            {
                strValue = CoreDataTable.ConvertObjectToString(storageType.type, storageType.typeModifier, value, type);
            }

            var xItem = serializer.CreateElement(XmlConvert.EncodeName(key), strValue);
            
            var typeAttr = serializer.CreateAttribute("T", storageType.type.ToString());
            var typeModifierAttr = serializer.CreateAttribute("TM", storageType.typeModifier.ToString());
            
            serializer.AppendAttribute(xItem, typeAttr);
            serializer.AppendAttribute(xItem, typeModifierAttr);

            
            if (type.IsEnum)
            {
                var etypeAttr = serializer.CreateAttribute("EnumType", type.AssemblyQualifiedName);

                serializer.AppendAttribute(xItem, etypeAttr);
            }
            else if (storageType.type == TableStorageType.UserType || storageType.typeModifier == TableStorageTypeModifier.Complex)
            {
                var ctypeAttr = serializer.CreateAttribute("DataType", type.AssemblyQualifiedName);

                serializer.AppendAttribute(xItem, ctypeAttr);
            }
            
            return xItem;
        }

        internal static void WriteXProperties<T, V>(SerializerAdapter<T, V> serializer, T col, string xName, Map<string, ExtPropertyValue> dict)
        {
            if (dict.Count > 0)
            {
                var xPropsItem = serializer.CreateElement(xName);

                foreach (var kv in dict)
                {
                    var value = kv.Value.Current;

                    if (value is null)
                    {
                        value = kv.Value.Original;
                    }

                    if (value is string str)
                    {
                        var xItem = serializer.CreateElement(XmlConvert.EncodeName(kv.Key), str);
                        
                        serializer.AppendElement(xPropsItem, xItem);
                    }
                    else
                    {
                        var xItem = SerializeNonString(serializer, kv.Key, value);
                        
                        serializer.AppendElement(xPropsItem, xItem);
                    }
                }

                serializer.AppendElement(col, xPropsItem);
            }
        }
    }
}