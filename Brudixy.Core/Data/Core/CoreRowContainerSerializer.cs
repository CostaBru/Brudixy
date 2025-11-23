using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Brudixy.Converter;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class CoreRowContainerSerializer<T, V> 
    {
        protected readonly CoreDataRowContainer m_container;

        protected readonly SerializerAdapter<T, V> m_serializer;

        public CoreRowContainerSerializer(CoreDataRowContainer container, SerializerAdapter<T, V> serializer)
        {
            m_container = container;
            m_serializer = serializer;
        }

        public virtual T WriteDataTo([NotNull] T element)
        {
            var rowElement = m_serializer.CreateElement("Row");

            for (int colIndex = 0; colIndex < m_container.GetColumnCount(); colIndex++)
            {
                ICoreTableReadOnlyColumn col = m_container.GetColumn(colIndex);

                var value = m_container[colIndex];

                string objectToString;

                if (col.TypeModifier == TableStorageTypeModifier.Complex)
                {
                    objectToString = m_serializer.WriteUserType(col.Type, col.TypeModifier, value);
                }
                else
                {
                    objectToString = CoreDataTable.ConvertObjectToString(col.Type, col.TypeModifier, value);
                }

                WriteCellValue(col, objectToString, rowElement);
            }

            var xProperties = m_container.GetXProperties().ToData();

            if (xProperties.Count > 0)
            {
                Serializer.WriteXProperties(m_serializer, rowElement, "XProperties", m_container.ContainerDataProps.ExtProperties);
            }

            m_serializer.AppendElement(element, rowElement);

            return rowElement;
        }

        protected virtual T WriteCellValue(ICoreTableReadOnlyColumn col, string objectToString, T rowElement)
        {
            var colEl = m_serializer.CreateElement(col.ColumnName, objectToString);

            m_serializer.AppendElement(rowElement, colEl);

            return colEl;
        }

        public T SerializeSchema(Func<string, IEnumerable<string>, HashSet<string>> tableFieldFilter = null, bool writeComments = false)
        {
            var ele = m_serializer.CreateElement("Metadata");

            if (writeComments)
            {
                m_serializer.WriteComments(ele);
            }

            HashSet<string> filter = null;
            if (tableFieldFilter != null)
            {
                filter = tableFieldFilter(m_container.TableName, m_container.Columns.Select(col => col.ColumnName));
            }

            var pkCols = new HashSet<string>();

            var kpCol = m_container.PrimaryKeyColumn.ToArray();

            if (kpCol.Length > 0)
            {
                var keyEl = m_serializer.CreateElement("Key");

                foreach (var col in kpCol)
                {
                    pkCols.Add(col.ColumnName);

                    var skipColumn = filter != null && filter.Contains(col.ColumnName) == false;

                    if (skipColumn)
                    {
                        break;
                    }

                    m_serializer.AppendElement(keyEl, m_serializer.CreateElement("Column", XmlConvert.EncodeLocalName(col.ColumnName)));
                }

                m_serializer.AppendElement(ele, keyEl);
            }
            
            SerializeColumns(ele, filter);

            return ele;
        }
        
        protected void SerializeColumns(T ele, HashSet<string> filter)
        {
            var pkCols = new Set<string>();

            if (m_container.PrimaryKeyColumn.Any())
            {
                var keyEl = m_serializer.CreateElement("Key");

                foreach (var column in m_container.PrimaryKeyColumn)
                {
                    pkCols.Add(column.ColumnName);

                    var skipColumn = filter != null && filter.Contains(column.ColumnName) == false;

                    if (skipColumn)
                    {
                        break;
                    }

                    m_serializer.AppendElement(keyEl, m_serializer.CreateElement("Column", XmlConvert.EncodeLocalName(column.ColumnName)));
                }

                m_serializer.AppendElement(ele, keyEl);
            }

            foreach (var column in m_container.Columns)
            {
                var skipColumn = filter != null && filter.Contains(column.ColumnName) == false;

                if (skipColumn)
                {
                    continue;
                }

                SerializeColumn(ele, column, pkCols);
            }
            
            pkCols.Dispose();
        }

        protected virtual T SerializeColumn(T ele, CoreDataColumnContainer column, Set<string> pkCols)
        {
            var col = m_serializer.CreateElement("Column");

            m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("Name", column.ColumnName));

            m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("Type", column.Type.ToString()));

            if (column.IsUnique && pkCols.IsMissing(column.ColumnName))
            {
                m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("Unique", "true"));
            }

            if (column.IsAutomaticValue)
            {
                m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("AutoIncrement", "true"));
            }

            if (column.MaxLength.HasValue)
            {
                m_serializer.AppendAttribute(col,
                    m_serializer.CreateAttribute("MaxLength", column.MaxLength.ToString()));
            }

            if (column.DefaultValue != null)
            {
                m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("DefaultValue", column.DefaultValue.ToString()));
            }

            if (column.Type == TableStorageType.UserType || column.TypeModifier == TableStorageTypeModifier.Complex)
            {
                Type type = column.DataType;

                if (type != null)
                {
                    m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("DataType", type.AssemblyQualifiedName));
                }
            }

            WriteColumnExtProperties(column, col, "XProperties");

            m_serializer.AppendElement(ele, col);
            
            return col;
        }

        private void WriteColumnExtProperties(CoreDataColumnContainer column, T col, string xName)
        {
            column.SerializerXProperties<T, V>(m_serializer, col, xName);
        }

        public virtual void Deserialize(T element)
        {
            var propsBuilder = CreateBuilder();

            var (metadataProps, containerProps) = DeserializeCore(element, out var isReadOnly, propsBuilder);

            m_container.Init(metadataProps, containerProps);

            m_container.IsReadOnly = isReadOnly;
        }

        protected virtual CoreContainerDataPropsBuilder CreateBuilder()
        {
            return new CoreContainerDataPropsBuilder();
        }

        protected (CoreContainerMetadataProps metadataProps, CoreContainerDataProps containerProps) DeserializeCore(T element, 
            out bool isReadOnly, 
            CoreContainerDataPropsBuilder containerPropsBuilder)
        {
            var metaData = m_serializer.GetElement(element, "Metadata");
            var tableName = m_serializer.GetAttributeValue(element, "TableName");
            isReadOnly = m_serializer.GetAttributeValue(element, "IsReadOnly") == "true";

            var elements = m_serializer.GetElements(metaData, "Column");

            var columnContainers = elements
                .Select((s, i) => CreateColumnInstance(DeserializeColumn(s, tableName, i).ToImmutable()))
                .ToArray();

            var dataColumnContainers =columnContainers
                .ToFrozenDictionary(
                    c => c.ColumnName,
                    c => c,
                    StringComparer.OrdinalIgnoreCase);

            var pk = DeserializePk(metaData);

            var (data, extProps) = ReadDataFrom(tableName, element, dataColumnContainers);

            containerPropsBuilder.Data = data;
            containerPropsBuilder.ExtProperties = extProps;
            containerPropsBuilder.DataRowState = RowState.Unchanged;

            var metadataProps = new CoreContainerMetadataProps(tableName, columnContainers, dataColumnContainers, pk, 0);

            return (metadataProps, containerPropsBuilder.ToProps());
        }

        private Data<string> DeserializePk(T metaData)
        {
            var pk = new Data<string>();

            var keyElement = m_serializer.GetElement(metaData, "Key");

            if (keyElement != null)
            {
                var colEls = m_serializer.GetElements(keyElement, "Column");

                foreach (var colEl in colEls)
                {
                    var elementValue = m_serializer.GetElementValue(colEl);

                    pk.Add(elementValue);
                }
            }

            return pk;
        }

        protected virtual CoreDataColumnContainerBuilder DeserializeColumn(
            T colElement,
            string tableName, 
            int colIndex)
        {
            var columnName = m_serializer.GetAttributeValue(colElement, "Name", "Column");
            TableStorageType columnType = TableStorageType.String;
            TableStorageTypeModifier storageTypeModifier = TableStorageTypeModifier.Simple;

            TableStorageType.TryParse(m_serializer.GetAttributeValue(colElement, "Type"), true, out columnType);
            TableStorageTypeModifier.TryParse(m_serializer.GetAttributeValue(colElement, "TypeModifier"), true, out storageTypeModifier);

            var maxLength = Tool.UIntParseFast(m_serializer.GetAttributeValue(colElement, "MaxLength") ?? string.Empty, 0, false);
            var defaultValue = m_serializer.GetAttributeValue(colElement, "DefaultValue");
            var allowNull = m_serializer.GetAttributeValue(colElement, "AllowNull") == "true";
            var isUnique = m_serializer.GetAttributeValue(colElement, "Unique") == "true" ? true : new bool?();
            var autoIncrement = m_serializer.GetAttributeValue(colElement, "AutoIncrement") == "true" ? true : new bool?();

            Type userType = null;

            object defaultVal = null;

            if (columnType == TableStorageType.UserType || storageTypeModifier == TableStorageTypeModifier.Complex)
            {
                var typeName = m_serializer.GetAttributeValue(colElement, "DataType");

                userType = Type.GetType(typeName);

                if (userType == null)
                {
                    var resolveComplexTypeEventArgs = new CoreDataTable.ResolveUserTypeEventArgs()
                    {
                        ColumnOrXPropertyName = columnName,
                        Name = tableName,
                        NameSpace = tableName,
                        TypeFullName = typeName,
                    };

                    CoreDataTable.OnOnResolveUserType(resolveComplexTypeEventArgs);

                    if (resolveComplexTypeEventArgs.Type == null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot deserialize complex type for '{tableName}' row container and column '{columnName}'. (Type '{typeName}''). Please use the DataTable.OnResolveComplexType event to resolve this type.");
                    }

                    userType = resolveComplexTypeEventArgs.Type;
                }

                if (storageTypeModifier == TableStorageTypeModifier.Complex && string.IsNullOrEmpty(defaultValue) == false && userType != null)
                {
                    var instance = Activator.CreateInstance(userType);

                    m_serializer.ReadUserType(defaultValue, instance);

                    defaultVal = instance;
                }
            }
            else
            {
                if (defaultValue != null)
                {
                    defaultVal = CoreDataTable.ConvertStringToObject(columnType, storageTypeModifier, defaultValue);
                }
            }

            var col = CreateColumnBuilderInstance();
            
            col.ColumnName = columnName;
            col.ColumnHandle = colIndex;
            col.DataType = userType;
            col.Type = columnType;
            col.TypeModifier = storageTypeModifier;
            col.IsAutomaticValue = autoIncrement ?? false;
            col.IsUnique = isUnique ?? false;
            col.MaxLength = maxLength;
            col.DefaultValue = defaultVal;
            col.AllowNull = allowNull;

            var xPropertiesEl = m_serializer.GetElement(colElement, "XProperties");

            if (xPropertiesEl != null)
            {
                ReadColumnExtProperties(tableName, m_serializer, xPropertiesEl, columnName);
            }
            
            return col;
        }

         protected virtual CoreDataColumnContainerBuilder CreateColumnBuilderInstance()
         {
             return new CoreDataColumnContainerBuilder();
         }
         
         protected virtual CoreDataColumnContainer CreateColumnInstance(CoreDataColumnObj columnObj)
         {
             return new CoreDataColumnContainer(columnObj);
         }

         private void ReadColumnExtProperties(string tableName, SerializerAdapter<T, V> serializer, T extPropertiesItem, string columnName)
         {
             foreach (var xPropElement in serializer.GetElements(extPropertiesItem))
             {
                 var xPropName = serializer.GetElementName(xPropElement);
                 var xPropValue = serializer.GetElementValue(xPropElement);
                 
                 var xPropTableValue = Serializer.DeserializeXPropValue(m_serializer, tableName, tableName, xPropElement, xPropValue, xPropName);

                 m_container.GetColumn(columnName).SetXProperty(xPropName, xPropTableValue);
             }
         }

         public (Data<object> data, Map<string, ExtPropertyValue> extProps)
             ReadDataFrom(string tableName, 
                 [NotNull] T source, 
                 IReadOnlyDictionary<string, CoreDataColumnContainer> colMap)
        {
            T element = m_serializer.GetElement(source, "Row");

            if (element != null)
            {
                var values = new Data<object>();
                var extProps = new Map<string, ExtPropertyValue>();
                
                values.Ensure(colMap.Count);
                
                var colValues = m_serializer.GetElements(element);

                foreach (var colValue in colValues)
                {
                    var elementName = m_serializer.GetElementName(colValue);
                    var elementValue = m_serializer.GetElementValue(colValue);

                    if (colMap.TryGetValue(elementName, out var column) == false)
                    {
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(elementValue) == false)
                    {
                        var dataType = column.DataType;

                        if (dataType != null && (column.Type == TableStorageType.UserType || column.TypeModifier == TableStorageTypeModifier.Complex))
                        {
                            if (column.TypeModifier == TableStorageTypeModifier.Complex)
                            {
                                var val = Activator.CreateInstance(dataType);

                                m_serializer.ReadUserType(elementValue, val);

                                values[column.ColumnHandle] = val;
                            }
                            else
                            {
                                values[column.ColumnHandle] = CoreDataTable.ConvertStringToObject(column.Type, column.TypeModifier, elementValue, dataType);
                            }
                        }
                        else
                        {
                            values[column.ColumnHandle] = CoreDataTable.ConvertStringToObject(column.Type, column.TypeModifier, elementValue, column.DataType);
                        }
                    }
                }

                var xPropertiesEl = m_serializer.GetElement(element, "XProperties");

                if (xPropertiesEl != null)
                {
                    ReadRowExtProperties(tableName, xPropertiesEl, extProps);
                }

                return (values, extProps);
            }

            return (null, null);
        }
        
        protected void ReadRowExtProperties(string tableName, 
            T extPropertiesEl,
            Map<string, ExtPropertyValue> extPropertyValues)
        {
            foreach (var xPropEl in m_serializer.GetElements(extPropertiesEl))
            {
                var xPropName = m_serializer.GetElementName(xPropEl);
                var xPropValue = m_serializer.GetElementValue(xPropEl);
                
                var xPropTableValue = Serializer.DeserializeXPropValue(m_serializer, tableName, tableName, xPropEl, xPropValue, xPropName);

                extPropertyValues[xPropName] = new ExtPropertyValue() { Current = xPropTableValue };
            }
        }
    }
}
