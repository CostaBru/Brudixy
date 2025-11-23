using System;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public static class TypeConvertor
    {
        public static IComparable[] AsComparable(object[] objects)
        {
            if (objects is null)
            {
                return null;
            }
            
            IComparable[] cmp;

            if (objects is IComparable[] oc)
            {
                cmp = oc;
            }
            else
            {
                cmp = new IComparable[objects.Length];

                for (int i = 0; i < cmp.Length && i < objects.Length; i++)
                {
                    cmp[i] = (IComparable)objects[i];
                }
            }

            return cmp;
        }
        
        internal static T ConvertValue<T>(object value,
            string column,
            string tableName,
            TableStorageType? storageType,
            TableStorageTypeModifier? typeModifier,
            string source)
        {
            if (value is null)
            {
                return ReturnDefault<T>();
            }

            if (value is string s && string.IsNullOrEmpty(s))
            {
                return default;
            }

            try
            {
                if (Tool.IsString<T>())
                {
                    var type = value.GetType();

                    var columnType = CoreDataTable.GetColumnType(type);
                    
                    return (T)(object)CoreDataTable.ConvertObjectToString(columnType.type, columnType.typeModifier, value, type);
                }

                return Tool.ConvertBoxed<T>(value);
            }
            catch (Exception exc)
            {
                if (storageType.HasValue && typeModifier.HasValue)
                {
                    try
                    {
                        var parsableString = CoreDataTable.ConvertObjectToString(storageType.Value, typeModifier.Value, value);

                        var type = typeof(T);

                        var columnType = CoreDataTable.GetColumnType(type);
                        
                        var stringToObject = CoreDataTable.ConvertStringToObject(columnType.type, typeModifier ?? TableStorageTypeModifier.Simple, parsableString, type);

                        var convertedValue = (T)stringToObject;

                        if (Trace.Listeners.Count > 0)
                        {
                            Trace.WriteLine(
                                $"Performance critical data type mismatching conversion occured on casting. Please sync data types to avoid this. Source '{source}'. Table = '{tableName}', Column or XProperty: '{column}', Storage type '{storageType}', Convert from: {value.GetType()}, Convert to: '{type}'");
                        }

                        return convertedValue;
                    }
                    catch (FormatException)
                    {
                        throw exc;
                    }
                }

                throw;
            }
        }
        
        public static (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull) GetColumnType(Type type)
        {
            if(CoreDataTable.ColumnTypeCache.TryGetValue(type, out var item))
            {
                return item;
            }

            return CoreDataTable.ColumnTypeCache[type] = GetColumnTypeCore(type);
        }

        private static (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull)
            GetColumnTypeCore(Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            var typeTo = nullableType ?? type;

            if (typeTo.IsEnum)
            {
                return (TableStorageType.String, TableStorageTypeModifier.Simple, true);
            }

            if (typeTo.IsArray)
            {
                var underlyingArrayType = typeTo.GetElementType();

                var underlyingType = Nullable.GetUnderlyingType(underlyingArrayType);
                
                var arrayTypeTo = underlyingType ?? underlyingArrayType;

                var arrayColType = TableStorageTypeMap.GetColumnType(arrayTypeTo);

                return (arrayColType, TableStorageTypeModifier.Array, underlyingType != null);
            }

            var tableStorageType = TableStorageTypeMap.GetColumnType(typeTo);

            if (tableStorageType == TableStorageType.UserType)
            {
                return (TableStorageType.UserType, TableStorageTypeModifier.Simple, true);
            }
            
            return (tableStorageType, TableStorageTypeModifier.Simple, nullableType != null);
        }

        public static T ReturnDefault<T>()
        { 
            if (Tool.IsObject<T>())
            {
                return default;
            }
            
            if (Tool.IsString<T>())
            {
                return (T)(object)string.Empty;
            }

            if (Tool.IsArray<T>())
            {
                return (T)(object)Array.CreateInstance(typeof(T).GetElementType(), 0);
            }
            
            if (Tool.IsRange<T>())
            {
                return (T)Activator.CreateInstance(typeof(T));
            }

            return default;
        }
        
        public static T ReturnDefaultNotNull<T>(TableStorageType tableStorageType, TableStorageTypeModifier modifier)
        {
            if (Tool.IsString<T>())
            {
                return (T)(object)string.Empty;
            }

            if (Tool.IsArray<T>())
            {
                return (T)(object)Array.CreateInstance(typeof(T).GetElementType(), 0);
            }

            if (Tool.IsObject<T>())
            {
                return (T)ReturnDefaultNotNullBoxed(typeof(T), tableStorageType, modifier);
            }

            if (modifier == TableStorageTypeModifier.Range)
            {
                return Activator.CreateInstance<T>();
            }

            if (tableStorageType != TableStorageType.UserType && modifier != TableStorageTypeModifier.Complex && Tool.IsObject<T>())
            {
                return Activator.CreateInstance<T>();
            }

            return default;
        }

        public static object ReturnDefaultNotNullBoxed([NotNull] Type dataType, TableStorageType tableStorageType, TableStorageTypeModifier modifier)
        {
            if (modifier == TableStorageTypeModifier.Simple)
            {
                switch (tableStorageType)
                {
                    case TableStorageType.String: return string.Empty;
                    case TableStorageType.Uri: return new Uri("http://test");
                    case TableStorageType.Xml: return XElement.Parse("<test/>");
                    case TableStorageType.Json: return JsonObject.Parse("{}");
                    case TableStorageType.Type: return typeof(object);
                }
            }
            
            if (dataType == null)
            {
                throw new ArgumentNullException(nameof(dataType));
            }
            
            if(modifier == TableStorageTypeModifier.Array)
            {
                return Array.CreateInstance(dataType.GetElementType(), 0);
            }
                
            return CoreDataTable.DefaultNotNull.GetOrDefault(dataType) ?? Activator.CreateInstance(dataType);
        }

        public static bool CanBeTreatedAsNull<T>(T value)
        {
            if (value is null)
            {
                return true;
            }
            
            if (value is string str)
            {
                return string.IsNullOrEmpty(str);
            }
            
            if (value is Array arr)
            {
                return arr.Length == 0;
            }
            
            if (value is IRange rng)
            {
                return rng.GetLenghtD() == 0;
            }

            return false;
        }
        
        public static bool CanBeTreatedAsNullBoxed(TableStorageType tableStorageType, TableStorageTypeModifier modifier, object value)
        {
            if (value is null)
            {
                return true;
            }
            
            if (tableStorageType == TableStorageType.String)
            {
                if (value is string str)
                {
                    return string.IsNullOrEmpty(str);
                }

                return false;
            }

            if (modifier == TableStorageTypeModifier.Array)
            {
                if (value is Array arr)
                {
                    return arr.Length == 0;
                }
                
                return false;
            }

            if (modifier == TableStorageTypeModifier.Range)
            {
                if (value is IRange rng)
                {
                    return rng.GetLenghtD() == 0;
                }

                return false;
            }

            return false;
        }
    }
}