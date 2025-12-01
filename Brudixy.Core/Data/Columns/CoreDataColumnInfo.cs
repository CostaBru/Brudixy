using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class CoreDataColumnInfo
    {
        internal List<CoreDataColumn> Columns = new List<CoreDataColumn>();

        [NotNull]
        internal Map<string, CoreDataColumn> ColumnMappings = new (StringComparer.InvariantCultureIgnoreCase);

        internal KeyValuePair<string, string>[] ExtPropertiesDebug
        {
            get
            {
                var keyValuePairs = new List<KeyValuePair<string, string>>();

                foreach (var column in Columns)
                {
                    
                    var value = new StringBuilder();

                    foreach (var kv in column.GetXProperties())
                    {
                        value.Append($"{kv.Key} = '{kv.Value}' | ");
                    }

                    if (value.Length > 0)
                    {
                        keyValuePairs.Add(new KeyValuePair<string, string>(column.ColumnName, value.ToString()));
                    }
                }

                return keyValuePairs.ToArray();
            }
        }

        public int ColumnsCount => Columns.Count;
        
        [NotNull]
        public CoreDataColumn[] PrimaryKeyColumns = new CoreDataColumn[0];

        public void SetAutomaticValueColumn(int columnHandle, bool value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithIsAutomaticValue(value);
        }
        
        public void SetServiceColumn(int columnHandle, bool value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithIsServiceColumn(value);
        }

        public void SetUniqueColumn(int columnHandle, bool value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithIsUnique(value);
        }

        public void SetIndexedColumn(int columnHandle, bool value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithHasIndex(value);
        }
        
        public void SetExtProperties(CoreDataColumn dataColumn, ImmutableDictionary<string, object> dictionary)
        {
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithXProperties(dictionary);
        }

        public object GetExtProperty(int columnHandle, string propertyName)
        {
            return Columns[columnHandle].ColumnObj.GetXProperty<object>(propertyName);
        }
        
        public IReadOnlyCollection<KeyValuePair<string, object>> GetXProperties(int columnHandle, string propertyName)
        {
            return Columns[columnHandle].ColumnObj.GetXProperties();
        }

        public void SetDefaultValue(int columnHandle, object value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithDefaultValue(value);
        }
        
        public void SetHasIndex(int columnHandle, bool value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithHasIndex(value);
        }

        public void ColumnSetMaxLength(int columnHandle, uint? value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithMaxLength(value);
        }

        public bool IsDisposed;

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            ColumnMappings?.Dispose();
            PrimaryKeyColumns = Array.Empty<CoreDataColumn>();

            foreach (var column in Columns)
            {
                column.SetRemoved();
            }
            
            Columns.Clear();
        }

        public virtual void Clear()
        {
        }

        public virtual void CopyFrom(CoreDataTable owner, CoreDataColumnInfo sourceColumnInfo, bool withData)
        {
            ColumnMappings = new (StringComparer.InvariantCultureIgnoreCase);

            Columns = new List<CoreDataColumn>(sourceColumnInfo.Columns.Count);

            foreach (var dataColumn in sourceColumnInfo.Columns)
            {
                var newColumn = dataColumn.Clone(owner, withData);
                
                Columns.Add(newColumn);

                ColumnMappings[dataColumn.ColumnName] = newColumn;
            }
            
            CopyPrimaryKeyColumnsFrom(sourceColumnInfo);
        }

        private void CopyPrimaryKeyColumnsFrom(CoreDataColumnInfo source)
        {
            var containers = new CoreDataColumn[source.PrimaryKeyColumns.Length];

            for (int i = 0; i < containers.Length; i++)
            {
                var container = source.PrimaryKeyColumns[i];

                containers[i] = this.Columns[container.ColumnHandle];
            }

            PrimaryKeyColumns = containers;
        }

        public virtual void Remove(CoreDataColumn column)
        {
            ColumnMappings.Remove(column.ColumnName);
            Columns.RemoveAt(column.ColumnHandle);

            column.SetRemoved();
        }

        public static void RemoveForBitArray(int columnHandle, ref BitArr bitArr)
        {
            if (bitArr == null)
            {
                return;
            }

            var newBitArray = new BitArr(bitArr.Length);

            for (int i = 0; i < columnHandle; i++)
            {
                newBitArray.Set(i, bitArr.Get(i));
            }

            for (int i = columnHandle; i < bitArr.Length - 1; i++)
            {
                newBitArray.Set(i, bitArr.Get(i + 1));
            }

            bitArr = newBitArray;
        }

        public virtual bool CanRemove(CoreDataColumn column, bool isThrowEx, string tableName)
        {
            if (column.ColumnObj.IsBuiltin)
            {
                if (!isThrowEx)
                {
                    return false;
                }
                throw new ReadOnlyAccessViolationException($"Cannot remove '{column.ColumnName}' column from '{tableName}' data table, because it is built in.");
            }
            
            if (PrimaryKeyColumns.Any(c => c.ColumnObj.ColumnHandle == column.ColumnHandle))
            {
                var columnToRemove = column.ColumnObj.ColumnName;
                
                if (isThrowEx)
                {
                    throw new ReadOnlyAccessViolationException($"Cannot remove data '{columnToRemove}' columns from '{tableName}' data table, because it is in primary key.");
                }

                return false;
            }

            return true;
        }

        public static void ChangeHandles<T>(Map<int, int> oldToNewMap, Data<T> columns)
        {
            if (columns == null)
            {
                return;
            }

            Data<T> tempColumns = new(columns);

            foreach (var kv in oldToNewMap)
            {
                var oldColumnHandle = kv.Key;
                var newColumnHandle = kv.Value;

                if (oldColumnHandle < columns.Count && newColumnHandle < columns.Count)
                {
                    columns[newColumnHandle] = tempColumns[oldColumnHandle];
                }
            }
            
            tempColumns.Dispose();
        }
        
        public static void ChangeHandles(Map<int, int> oldToNewMap, List<CoreDataColumn> columns)
        {
            if (columns == null)
            {
                return;
            }

            List<CoreDataColumn> tempColumns = new(columns);

            foreach (var kv in oldToNewMap)
            {
                var oldColumnHandle = kv.Key;
                var newColumnHandle = kv.Value;

                if (newColumnHandle == oldColumnHandle)
                {
                    continue;
                }

                if (oldColumnHandle < columns.Count && newColumnHandle < columns.Count)
                {
                    var dataColumn = tempColumns[oldColumnHandle];
                    
                    columns[newColumnHandle] = dataColumn;

                    dataColumn.ColumnObj = dataColumn.ColumnObj.WithColumnHandle(newColumnHandle);
                }
            }
        }

        public void Serialize<T, V>(SerializerAdapter<T, V> serializer, T ele, HashSet<string> filter)
        {
            var pkCols = new Set<int>();

            if (PrimaryKeyColumns.Length > 0)
            {
                var keyEl = serializer.CreateElement("Key");

                foreach (var container in PrimaryKeyColumns)
                {
                    var column = container.ColumnObj.ColumnName;

                    pkCols.Add(container.ColumnObj.ColumnHandle);

                    var skipColumn = filter != null && filter.Contains(column) == false;

                    if (skipColumn)
                    {
                        break;
                    }

                    serializer.AppendElement(keyEl, serializer.CreateElement("Column", XmlConvert.EncodeLocalName(column)));
                }

                serializer.AppendElement(ele, keyEl);
            }

            foreach (var column in this.Columns)
            {
                var skipColumn = filter != null && filter.Contains(column.ColumnName) == false;

                if (skipColumn)
                {
                    continue;
                }

                SerializeColumn(serializer, ele, column, pkCols);
            }
            
            pkCols.Dispose();
        }

        protected virtual T SerializeColumn<T, V>(SerializerAdapter<T, V> serializer, T ele, CoreDataColumn column, Set<int> pkCols)
        {
            var col = serializer.CreateElement("Column");

            serializer.AppendAttribute(col, serializer.CreateAttribute("Name", column.ColumnName));

            serializer.AppendAttribute(col, serializer.CreateAttribute("Type", column.Type.ToString()));

            if (column.TypeModifier != TableStorageTypeModifier.Simple)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("TypeModifier", column.TypeModifier.ToString()));
            }
            
            if (column.Type == TableStorageType.UserType || column.TypeModifier == TableStorageTypeModifier.Complex)
            {
                Type type = column.DataType;

                if (type != null)
                {
                    serializer.AppendAttribute(col, serializer.CreateAttribute("DataType", type.AssemblyQualifiedName));
                }
            }

            if (column.IsUnique && pkCols.IsMissing(column.ColumnHandle))
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("Unique", "true"));
            }

            if (column.HasIndex && column.IsUnique == false)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("HasIndex", "true"));
            }

            if (column.IsAutomaticValue)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("AutoIncrement", "true"));
            }

            if (column.MaxLength.HasValue)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("MaxLength", column.MaxLength.ToString()));
            }
            
            if (column.AllowNull)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("AllowNull", "true"));
            }

            if (column.DefaultValue != null)
            {
                var defValStr = string.Empty;

                if (column.TypeModifier == TableStorageTypeModifier.Complex)
                {
                    defValStr = serializer.WriteUserType(column.Type, column.TypeModifier, column.DefaultValue);
                }
                else
                {
                    defValStr = CoreDataTable.ConvertObjectToString(column.DefaultValue);
                }

                serializer.AppendAttribute(col, serializer.CreateAttribute("DefaultValue", defValStr));
            }

            if (column.IsServiceColumn)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("ServiceColumn", "true"));
            }

            if (column.IsBuiltin)
            {
                serializer.AppendAttribute(col, serializer.CreateAttribute("Builtin", "true"));
            }

            WriteColumnExtProperties(serializer, column, col, "XProperties");

            serializer.AppendElement(ele, col);

            return col;
        }

        private void WriteColumnExtProperties<T, V>(SerializerAdapter<T, V> serializer, CoreDataColumn column, T col, string xName)
        {
            var dataColumnContainer = Columns[column.ColumnHandle];

            dataColumnContainer.ColumnObj.SerializerXProperties(serializer, col, xName);

        }

        public void DeserializeColumns<T, V>(CoreDataTable table, SerializerAdapter<T, V> serializer, T  element, bool buildinDefault)
        {
            var elements = serializer.GetElements(element, "Column");

            foreach (var colElement in elements)
            {
                var columnHandle = DeserializeColumn(table, serializer, buildinDefault, colElement);
            }

            var keyElement = serializer.GetElement(element, "Key");

            if (keyElement != null)
            {
                var colEls = serializer.GetElements(keyElement, "Column");

                var list = new Data<string>();

                foreach (var colEl in colEls)
                {
                    list.Add(serializer.GetElementValue(colEl));
                }

                table.SetPrimaryKeyColumns(list);

                list.Dispose();
            }
        }

        protected virtual CoreDataColumn DeserializeColumn<T, V>(CoreDataTable table,
            SerializerAdapter<T, V> serializer, bool buildinDefault, T colElement)
        {
            var columnName = serializer.GetAttributeValue(colElement, "Name", "Column");

            var existingColumn = table.TryGetColumn(columnName);

            if (existingColumn != null)
            {
                return existingColumn;
            }

            TableStorageType columnType = TableStorageType.String;
            TableStorageTypeModifier columnTypeModifier = TableStorageTypeModifier.Simple;

            Enum.TryParse(serializer.GetAttributeValue(colElement, "Type"), true, out columnType);
            Enum.TryParse(serializer.GetAttributeValue(colElement, "TypeModifier"), true, out columnTypeModifier);

            var maxLengthStr = serializer.GetAttributeValue(colElement, "MaxLength");
            var defaultValue = serializer.GetAttributeValue(colElement, "DefaultValue");
            var isUnique = serializer.GetAttributeValue(colElement, "Unique") == "true" ? true : new bool?();
            var hasIndex = serializer.GetAttributeValue(colElement, "HasIndex") == "true" ? true : new bool?();
            var autoIncrement = serializer.GetAttributeValue(colElement, "AutoIncrement") == "true" ? true : new bool?();
            var serviceColumn = serializer.GetAttributeValue(colElement, "ServiceColumn") == "true";
            var allowNull = serializer.GetAttributeValue(colElement, "AllowNull") == "true";

            uint? maxLength = null;

            if (string.IsNullOrEmpty(maxLengthStr) == false)
            {
                maxLength = Tool.UIntParseFast(maxLengthStr, 0, false);

                if (maxLength == 0)
                {
                    maxLength = null;
                }
            }

            var builtin = buildinDefault;

            var builtinStr = serializer.GetAttributeValue(colElement, "Builtin");

            if (string.IsNullOrEmpty(builtinStr) == false)
            {
                builtin = builtinStr == "true";
            }

            object defaultVal = null;
            Type dataType = null;

            if (columnType == TableStorageType.UserType || columnTypeModifier == TableStorageTypeModifier.Complex)
            {
                var typeNameValue = serializer.GetAttributeValue(colElement, "DataType");
                
                dataType = Serializer.RestoreUserType(table.TableName, table.Namespace, columnName, typeNameValue);

                if (columnTypeModifier == TableStorageTypeModifier.Complex && string.IsNullOrEmpty(defaultValue) == false && dataType != null)
                {
                    var instance = Activator.CreateInstance(dataType);

                    serializer.ReadUserType(defaultValue, instance);

                    defaultVal = instance;
                }
            }
            else
            {
                if (defaultValue != null)
                {
                    defaultVal = CoreDataTable.ConvertStringToObject(columnType, columnTypeModifier, defaultValue);
                }
            }
            
            var column = table.AddColumn(
                columnName,
                columnType,
                columnTypeModifier,
                dataType,
                auto: autoIncrement,
                unique: isUnique,
                columnMaxLength: maxLength,
                defaultValue: defaultVal,
                builtin: builtin,
                serviceColumn: serviceColumn,
                allowNull: allowNull
            );

            var extPropertiesElement = serializer.GetElement(colElement, "XProperties");

            if (extPropertiesElement != null)
            {
                ReadColumnExtProperties(serializer, table, extPropertiesElement, column);
            }

            if (hasIndex ?? false)
            {
                table.AddIndex(columnName, isUnique ?? false);
            }

            return column;
        }

        private void ReadColumnExtProperties<T, V>(SerializerAdapter<T, V> serializer, CoreDataTable table, T extPropertiesItem, CoreDataColumn column)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>();

            foreach (var xPropElement in serializer.GetElements(extPropertiesItem))
            {
                var xPropName = serializer.GetElementName(xPropElement);
                var xPropValue = serializer.GetElementValue(xPropElement);

                var xPropTableValue = Serializer.DeserializeXPropValue(serializer, table.Name, table.Namespace, xPropElement, xPropValue, xPropName);

                builder[xPropName] = xPropTableValue;
            }

            ImmutableDictionary<string, object> immutableDictionary = builder.ToImmutable();
            
            SetExtProperties(column, immutableDictionary);
        }

        public void SetPrimaryKeyColumn(CoreDataColumn dataColumn)
        {
            PrimaryKeyColumns = new[] { dataColumn };
        }

        public void SetPrimaryKeyColumns(IReadOnlyList<CoreDataColumn> primaryKeyColumns)
        {
            var containers = new CoreDataColumn[primaryKeyColumns.Count];

            for (int i = 0; i < containers.Length; i++)
            {
                containers[i] = primaryKeyColumns[i];
            }
            
            PrimaryKeyColumns = containers;
        }

        public void DropPrimaryKey()
        {
            PrimaryKeyColumns = Array.Empty<CoreDataColumn>();
        }

        public virtual void RemapColumnHandles(Map<int,int> oldToNewMap)
        {
            ChangeHandles(oldToNewMap, Columns);
        }

        public void SetExtProperty<T>(int columnHandle, string propertyName, T value)
        {
            var dataColumn = Columns[columnHandle];
            
            dataColumn.ColumnObj = dataColumn.ColumnObj.WithXProperty(propertyName, value);
        }
    }
}