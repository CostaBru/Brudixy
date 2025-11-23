using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        internal static void OnOnResolveUserType(ResolveUserTypeEventArgs e)
        {
            OnResolveUserType?.Invoke(null, e);
        }
        
        public class ResolveUserTypeEventArgs : System.EventArgs
        {
            public string NameSpace { get; set; }

            public string Name { get; set; }
            
            public string ColumnOrXPropertyName { get; set; }
            
            public string TypeFullName { get; set; }
            
            public Type Type { get; set; }
        }

        public static event EventHandler<ResolveUserTypeEventArgs> OnResolveUserType;

        public static Type GetColumnType(TableStorageType columnType, TableStorageTypeModifier columnTypeModifier, bool allowNull, Type type)
        {
            return GetDataType(columnType, columnTypeModifier, allowNull, type);
        }

        public static object GetDefaultNotNull(TableStorageType columnType, TableStorageTypeModifier modifier)
        {
            var dataType = GetDataType(columnType, modifier, false, null);

            if (dataType != null)
            {
                return TypeConvertor.ReturnDefaultNotNullBoxed(dataType, columnType, modifier);
            }

            throw new InvalidOperationException($"Type didn't find {columnType}\\{modifier}.");
        }

        internal static readonly ConcurrentDictionary<Type, (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull)> ColumnTypeCache = new();
        internal static readonly ConcurrentDictionary<Type, object> DefaultNotNull = new();

        public static void RegisterUserDefaultNotNull<T>([NotNull] object value)
        {
            DefaultNotNull[typeof(T)] = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public static (TableStorageType type, TableStorageTypeModifier typeModifier, bool allowNull) GetColumnType(Type type)
        {
            return TypeConvertor.GetColumnType(type);
        }
    }
}