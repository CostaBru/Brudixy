using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy
{
    [DebuggerDisplay("{ColumnName}, {Type}")]
    public class CoreDataColumnObj : ICoreTableReadOnlyColumn
    {
        public CoreDataColumnObj()
        {
        }

        internal ImmutableDictionary<string, object> XPropertiesStore { get; set; } =
            ImmutableDictionary<string, object>.Empty;

        public CoreDataColumnObj(string tableName,
            object defaultValue,
            string columnName,
            bool allowNull,
            bool isAutomaticValue,
            uint? maxLength,
            TableStorageType type,
            TableStorageTypeModifier typeModifier,
            Type dataType,
            bool isUnique,
            int columnHandle,
            bool isBuiltin,
            bool isServiceColumn,
            bool hasIndex,
            ImmutableDictionary<string, object> xPropStore)
        {
            TableName = tableName;
            DefaultValue = defaultValue;
            ColumnName = columnName;
            AllowNull = allowNull;
            IsAutomaticValue = isAutomaticValue;
            MaxLength = maxLength;
            Type = type;
            TypeModifier = typeModifier;
            DataType = dataType;
            IsUnique = isUnique;
            ColumnHandle = columnHandle;
            IsBuiltin = isBuiltin;
            IsServiceColumn = isServiceColumn;
            HasIndex = hasIndex;
            XPropertiesStore = xPropStore;
        }

        public string TableName { get; private set; }

        public CoreDataColumnObj WithTableName(string value)
        {
            if (TableName == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.TableName = value;
            return clone;
        }

        public object DefaultValue { get; private set; }

        public CoreDataColumnObj WithDefaultValue(object value)
        {
            if (DefaultValue == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.DefaultValue = value;
            return clone;
        }

        public string ColumnName { get; private set; }

        public CoreDataColumnObj WithColumnName(string value)
        {
            if (ColumnName == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.ColumnName = value;
            return clone;
        }

        public bool AllowNull { get; private set; }

        public CoreDataColumnObj WithAllowNull(bool value)
        {
            if (AllowNull == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.AllowNull = value;
            return clone;
        }


        public bool IsAutomaticValue { get; private set; }

        public CoreDataColumnObj WithIsAutomaticValue(bool value)
        {
            if (IsAutomaticValue == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.IsAutomaticValue = value;
            return clone;
        }

        public uint? MaxLength { get; private set; }

        public CoreDataColumnObj WithMaxLength(uint? value)
        {
            if (MaxLength == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.MaxLength = value;
            return clone;
        }

        public TableStorageType Type { get; private set; }

        public CoreDataColumnObj WithType(TableStorageType value)
        {
            if (Type == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.Type = value;
            return clone;
        }

        public TableStorageTypeModifier TypeModifier { get; set; } = TableStorageTypeModifier.Simple;

        public CoreDataColumnObj WithTypeModifier(TableStorageTypeModifier value)
        {
            if (TypeModifier == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.TypeModifier = value;
            return clone;
        }

        public Type DataType { get; private set; }

        public CoreDataColumnObj WithDataType(Type value)
        {
            if (DataType == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.DataType = value;
            return clone;
        }

        public Type StorageType =>
            CoreDataTable.GetDataType(this.Type, this.TypeModifier, this.AllowNull, this.DataType);

        public bool IsUnique { get; private set; }

        public CoreDataColumnObj WithIsUnique(bool value)
        {
            if (IsUnique == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.IsUnique = value;
            return clone;
        }

        public int ColumnHandle { get; private set; }

        public CoreDataColumnObj WithColumnHandle(int value)
        {
            if (ColumnHandle == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.ColumnHandle = value;
            return clone;
        }

        public bool IsBuiltin { get; private set; }

        public CoreDataColumnObj WithIsBuiltin(bool value)
        {
            if (IsBuiltin == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.IsBuiltin = value;
            return clone;
        }

        public bool IsServiceColumn { get; private set; }

        public CoreDataColumnObj WithIsServiceColumn(bool value)
        {
            if (IsServiceColumn == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.IsServiceColumn = value;
            return clone;
        }

        public bool HasIndex { get; private set; }

        public CoreDataColumnObj WithHasIndex(bool value)
        {
            if (HasIndex == value)
            {
                return this;
            }
            
            var clone = this.CloneCore();
            clone.HasIndex = value;
            return clone;
        }

        public CoreDataColumnObj WithXProperty(string key, object value)
        {
            if (XPropertiesStore.TryGetKey(key, out var val) && val.Equals(value))
            {
                return this;
            }
            
            var cloneCore = CloneCore();

            CoreDataRowContainer.CopyIfNeeded(ref value);

            cloneCore.XPropertiesStore = cloneCore.XPropertiesStore.SetItem(key, value);

            return cloneCore;
        }
        
        public CoreDataColumnObj WithXProperties(ImmutableDictionary<string,object> dictionary)
        {
            var cloneCore = CloneCore();

            cloneCore.XPropertiesStore = dictionary;

            return cloneCore;
        }
        
        public CoreDataColumnObj WithXPropertiesHandle(ImmutableDictionary<string,object> dictionary, int columnHandle)
        {
            var cloneCore = CloneCore();

            cloneCore.XPropertiesStore = dictionary;
            cloneCore.ColumnHandle = columnHandle;

            return cloneCore;
        }
  

        public bool HasXProperty(string xPropertyName)
        {
            XPropertiesStore.TryGetValue(xPropertyName, out var value);

            return value != null;
        }

        public T GetXProperty<T>(string xPropertyName)
        {
            return GetXPropValue<T>(XPropertiesStore, xPropertyName);
        }

        internal static T GetXPropValue<T>(IReadOnlyDictionary<string, object> props, string xPropertyName)
        {
            object value = null;

            props?.TryGetValue(xPropertyName, out value);

            if (value == null)
            {
                if (Tool.IsObject<T>())
                {
                    return default(T);
                }

                return TypeConvertor.ReturnDefault<T>();
            }

            var convert = XPropertyValueConverter.TryConvert<T>("Column", xPropertyName, value);

            CoreDataRowContainer.CopyIfNeeded(ref convert);

            return convert;
        }

        public IEnumerable<string> XProperties => XPropertiesStore.Keys;

        public IReadOnlyCollection<KeyValuePair<string, object>> GetXProperties()
        {
            return XPropertiesStore;
        }

        public CoreDataColumnObj Clone()
        {
            return CloneCore();
        }

        protected virtual CoreDataColumnObj CloneCore()
        {
            var clone = (CoreDataColumnObj)MemberwiseClone();

            var defaultValue = this.DefaultValue;

            CoreDataRowContainer.CopyIfNeeded(ref defaultValue);

            clone.DefaultValue = defaultValue;

            clone.XPropertiesStore = this.XPropertiesStore;

            return clone;
        }
     
        public void SerializerXProperties<T, V>(SerializerAdapter<T, V> serializer, T col, string xName)
        {
            if (XPropertiesStore != null)
            {
                Serializer.WriteXProperties<T, V>(serializer, col, xName, XPropertiesStore);
            }
        }
    }
}
