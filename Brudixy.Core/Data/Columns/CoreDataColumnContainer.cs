using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Brudixy.Converter;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("{ColumnName}, {Type}")]
    public class CoreDataColumnContainer : ICoreDataTableColumn
    {
        public CoreDataColumnContainer()
        {
            ColumnObj = new CoreDataColumnObj();
        }

        protected CoreDataColumnObj ColumnObj;

        public CoreDataColumnContainer([NotNull] ICoreTableReadOnlyColumn dataColumn)
        {
            var bld = new CoreDataColumnContainerBuilder();
            
            bld.TableName = dataColumn.TableName;
            bld.ColumnName = dataColumn.ColumnName;
            bld.IsAutomaticValue = dataColumn.IsAutomaticValue;
            bld.DefaultValue = dataColumn.DefaultValue;
            bld.MaxLength = dataColumn.MaxLength;
            bld.Type = dataColumn.Type;
            bld.TypeModifier = dataColumn.TypeModifier;
            bld.IsUnique = dataColumn.IsUnique;
            bld.DataType = dataColumn.DataType;
            bld.ColumnHandle = dataColumn.ColumnHandle;
            bld.InitExtProperties(dataColumn);
            
            ColumnObj = bld.ToImmutable();
        }
        
        public CoreDataColumnContainer([NotNull] CoreDataColumn dataColumn)
        {
            var bld = new CoreDataColumnContainerBuilder();
            
            bld.TableName = dataColumn.TableName;
            bld.ColumnName = dataColumn.ColumnName;
            bld.AllowNull = dataColumn.AllowNull;
            bld.IsAutomaticValue = dataColumn.IsAutomaticValue;
            bld.MaxLength = dataColumn.MaxLength;
            bld.Type = dataColumn.Type;
            bld.TypeModifier = dataColumn.TypeModifier;
            bld.ColumnHandle = dataColumn.ColumnHandle;
            bld.IsUnique = dataColumn.IsUnique;
            bld.DefaultValue = dataColumn.DefaultValue;
            bld.DataType = dataColumn.DataType;
            bld.IsBuiltin = dataColumn.IsBuiltin;
            bld.HasIndex = dataColumn.HasIndex;
            
            bld.InitExtProperties(dataColumn);

            ColumnObj = bld.ToImmutable();
        }
        
        public CoreDataColumnContainer(CoreDataColumnObj obj)
        {
            ColumnObj = obj;
        }
     
        protected void InitExtProperties(ICoreTableReadOnlyColumn dataColumn)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var pv in dataColumn.GetXProperties())
            {
                builder[pv.Key] = pv.Value;
            }

            ColumnObj = ColumnObj.WithXProperties(builder.ToImmutable());
        }
        
        public void SetXProperty<T>(string property, T value)
        {
            CoreDataRowContainer.CopyIfNeeded(ref value);

            ColumnObj = ColumnObj.WithXProperty(property, value);
        }

        public bool HasXProperty(string xPropertyName)
        {
            ColumnObj.XPropertiesStore.TryGetValue(xPropertyName, out var value);

            return value != null;
        }

        public T GetXProperty<T>(string xPropertyName)
        {
            return ColumnObj.GetXProperty<T>(xPropertyName);
        }

        public IEnumerable<string> XProperties => ColumnObj.XProperties;

        
        public IReadOnlyCollection<KeyValuePair<string, object>> GetXProperties()
        {
            return ColumnObj.GetXProperties();
        }

        public string TableName
        {
            get { return ColumnObj.TableName; }
            set { ColumnObj = ColumnObj.WithTableName(value); }
        }

        public object DefaultValue
        {
            get { return ColumnObj.DefaultValue; }
            set { ColumnObj = ColumnObj.WithDefaultValue(value); }
        }

        public string ColumnName
        {
            get { return ColumnObj.ColumnName; }
            set { ColumnObj = ColumnObj.WithColumnName(value); }
        }

        public bool AllowNull
        {
            get { return ColumnObj.AllowNull; }
            set { ColumnObj = ColumnObj.WithAllowNull(value); }
        }

        public bool IsAutomaticValue
        {
            get { return ColumnObj.IsAutomaticValue; }
            set { ColumnObj = ColumnObj.WithIsAutomaticValue(value); }
        }

        public uint? MaxLength
        {
            get { return ColumnObj.MaxLength; }
            set { ColumnObj = ColumnObj.WithMaxLength(value); }
        }

        public TableStorageType Type
        {
            get { return ColumnObj.Type; }
            set { ColumnObj = ColumnObj.WithType(value); }
        }

        public TableStorageTypeModifier TypeModifier
        {
            get { return ColumnObj.TypeModifier; }
            set { ColumnObj = ColumnObj.WithTypeModifier(value); }
        }

        public Type DataType
        {
            get { return ColumnObj.DataType; }
            set { ColumnObj = ColumnObj.WithDataType(value); }
        }

        public Type StorageType => ColumnObj.StorageType;

        public bool IsUnique
        {
            get { return ColumnObj.IsUnique; }
            set { ColumnObj = ColumnObj.WithIsUnique(value); }
        }

        public int ColumnHandle
        {
            get { return ColumnObj.ColumnHandle; }
            set { ColumnObj = ColumnObj.WithColumnHandle(value); }
        }

        public bool IsBuiltin
        {
            get { return ColumnObj.IsBuiltin; }
            set { ColumnObj = ColumnObj.WithIsBuiltin(value); }
        }

        public bool IsServiceColumn
        {
            get { return ColumnObj.IsServiceColumn; }
            set { ColumnObj = ColumnObj.WithIsServiceColumn(value); }
        }

        public bool HasIndex
        {
            get { return ColumnObj.HasIndex; }
            set { ColumnObj = ColumnObj.WithHasIndex(value); }
        }

        public CoreDataColumnContainer Clone()
        {
            return CloneCore();
        }

        protected virtual CoreDataColumnContainer CloneCore()
        {
            return (CoreDataColumnContainer)MemberwiseClone();
        }

        public static CoreDataColumnContainer CreateFrom([NotNull] ICoreTableReadOnlyColumn column,
            Func<ICoreTableReadOnlyColumn, CoreDataColumnContainer> colFactory)
        {
            if (column is CoreDataColumn cd)
            {
                return colFactory(cd);
            }

            if (column is CoreDataColumnContainer container)
            {
                return container.Clone();
            }

            return colFactory(column);
        }

        public void SerializerXProperties<T, V>(SerializerAdapter<T, V> serializer, T col, string xName)
        {
            ColumnObj.SerializerXProperties<T, V>(serializer, col, xName);
        }

        public void SetXProperties(ImmutableDictionary<string, object> dictionary)
        {
            ColumnObj = ColumnObj.WithXProperties(dictionary);
        }
    }
}
