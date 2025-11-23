using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Brudixy.Interfaces;

namespace Brudixy
{
    public class CoreDataColumnContainerBuilder : ICoreDataTableColumn
    {
        protected ImmutableDictionary<string, object>.Builder XPropertiesStore =
            ImmutableDictionary.CreateBuilder<string, object>(StringComparer.OrdinalIgnoreCase);
        
        public CoreDataColumnContainerBuilder()
        {
        }

        public string TableName { get; set; }

        public object DefaultValue { get; set; }

        public string ColumnName { get; set; }

        public bool AllowNull { get; set; }

        public bool IsAutomaticValue { get; set; }

        public uint? MaxLength { get; set; }
        
        public T GetXProperty<T>(string xPropertyName)
        {
            return CoreDataColumnObj.GetXPropValue<T>(XPropertiesStore, xPropertyName);
        }

        public bool HasXProperty(string xPropertyName)
        {
            XPropertiesStore.TryGetValue(xPropertyName, out var value);

            return value != null;
        }

        public IEnumerable<string> XProperties => XPropertiesStore.Keys;

        public TableStorageType Type { get; set; }

        public TableStorageTypeModifier TypeModifier { get; set; } = TableStorageTypeModifier.Simple;

        public IReadOnlyCollection<KeyValuePair<string, object>> GetXProperties() => XPropertiesStore;

        public Type DataType { get; set; }
        
        public Type StorageType => CoreDataTable.GetDataType(this.Type, this.TypeModifier, this.AllowNull, this.DataType);

        public bool IsUnique { get; set; }

        public int ColumnHandle { get; set; }

        public bool IsBuiltin { get; set; }

        public bool IsServiceColumn { get; set; }

        public bool HasIndex { get; set; }
        
        public void SetXProperty<T>(string property, T value)
        {
            CoreDataRowContainer.CopyIfNeeded(ref value);

            XPropertiesStore[property] = value;
        }
        
        public void SetXProperties(ImmutableDictionary<string, object>.Builder dictionary)
        {
            XPropertiesStore = dictionary;
        }

        public virtual CoreDataColumnObj ToImmutable()
        {
            return new CoreDataColumnObj(TableName,
                DefaultValue,
                ColumnName,
                AllowNull,
                IsAutomaticValue,
                MaxLength,
                Type,
                TypeModifier,
                DataType,
                IsUnique,
                ColumnHandle,
                IsBuiltin,
                IsServiceColumn,
                HasIndex,
                XPropertiesStore.ToImmutable());
        }

        public void InitExtProperties(ICoreTableReadOnlyColumn dataColumn)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var pv in dataColumn.GetXProperties())
            {
                builder[pv.Key] = pv.Value;
            }

            XPropertiesStore = builder;
        }
    }
}