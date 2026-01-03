using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Brudixy.Converter;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class CoreDataTableSerializer<T, V>
    {
        protected readonly CoreDataTable m_table;

        protected readonly SerializerAdapter<T, V> m_serializer;
        private readonly RelationSerializer<T, V> m_relationSerializer;

        public CoreDataTableSerializer(CoreDataTable table, SerializerAdapter<T, V> serializer)
        {
            m_table = table;
            m_serializer = serializer;

            m_relationSerializer = new RelationSerializer<T, V>(serializer);
        }

        public void WriteDataTo([NotNull] T element)
        {
            foreach (var row in m_table.RowsHandles)
            {
                SerializeRow(element, row);
            }
        }

        protected virtual T SerializeRow(T element, int row)
        {
            var rowElement = m_serializer.CreateElement("Row");

            foreach (var dataColumn in m_table.DataColumnInfo.Columns)
            {
                var dataItem = dataColumn.DataStorageLink;

                if (dataItem.IsNull(row, dataColumn))
                {
                    SerializeRowCellValue(rowElement, row, dataColumn, null);
                }
                else
                {
                    var value = dataItem.GetRawData(row, dataColumn);
                
                    SerializeRowCellValue(rowElement, row, dataColumn, value);
                }
            }

            if (row < m_table.StateInfo.RowXProps.Storage.Count)
            {
                var dict = m_table.StateInfo.RowXProps.Storage[row];

                if (dict?.Count > 0)
                {
                    Serializer.WriteXProperties(m_serializer, rowElement, "XProperties", dict);
                }
            }

            m_serializer.AppendElement(element, rowElement);

            return rowElement;
        }

        internal virtual T SerializeRowCellValue([NotNull] T element, int rowHandle, CoreDataColumn colum, object value)
        {
            string objectToString;

            if (colum.Type == TableStorageType.UserType || colum.TypeModifier == TableStorageTypeModifier.Complex)
            {
                objectToString = m_serializer.WriteUserType(colum.Type, colum.TypeModifier, value);
            }
            else
            {
                objectToString = m_serializer.ConvertObjectToString(colum.Type, colum.TypeModifier, value);
            }
            
            var columnName = colum.ColumnName;

            var colEl = m_serializer.CreateElement(columnName, objectToString);

            m_serializer.AppendElement(element, colEl);

            return colEl;
        }

        public T SerializeSchema(Func<string, IEnumerable<string>, HashSet<string>> tableFieldFilter = null, bool writeComments = false)
        {
            var ele = m_serializer.CreateElement("Metadata");
            
            if (m_table.CaseSensitive)
            {
                m_serializer.AppendAttribute(ele, m_serializer.CreateAttribute("CaseSensitive", "true"));
            }

            m_serializer.AppendAttribute(ele, m_serializer.CreateAttribute("RowCapacity", m_table.Capacity.ToString()));

            if (m_table.EnforceConstraints)
            {
                m_serializer.AppendAttribute(ele, m_serializer.CreateAttribute( "EnforceConstraints", "true"));
            }
            
            if (m_table.IsReadOnly)
            {
                m_serializer.AppendAttribute(ele, m_serializer.CreateAttribute( "IsReadOnly", "true"));
            }

            if (m_table.ExtProperties != null && m_table.ExtProperties.Count > 0)
            {
                WriteTableProperties(ele, "XProperties");
            }

            if (writeComments)
            {
                m_serializer.WriteComments(ele);
            }

            HashSet<string> filter = null;
            if (tableFieldFilter != null)
            {
                filter = tableFieldFilter(m_table.Name, m_table.GetColumns().Select(col => col.ColumnName));
            }

            m_table.DataColumnInfo.Serialize(this.m_serializer, ele, filter);

            if (m_table.MultiColumnIndexInfo.HasAny)
            {
                var multiColIndex = m_serializer.CreateElement("MultiColumnIndex");

                foreach (var index in m_table.MultiColumnIndexInfo.Indexes)
                {
                    var indexInfo = m_serializer.CreateElement("IndexInfo");

                    if (index.IsUnique)
                    {
                        m_serializer.AppendAttribute(indexInfo, m_serializer.CreateAttribute("Unique", "true"));
                    }

                    foreach (var columnHandle in index.Columns)
                    {
                        m_serializer.AppendElement(indexInfo, m_serializer.CreateElement("Column", XmlConvert.EncodeLocalName(m_table.DataColumnInfo.Columns[columnHandle].ColumnName)));
                    }

                    m_serializer.AppendElement(multiColIndex, indexInfo);
                }

                m_serializer.AppendElement(ele, multiColIndex);
            }

            var parentRelations = m_table.ParentRelations.Where(c => c.ParentTableName == m_table.Name && c.ChildTableName == m_table.Name).ToData();

            if (parentRelations.Any())
            {
                var parentEl = m_serializer.CreateElement("ParentRelations");

                m_relationSerializer.WriteRelations(parentRelations, parentEl);

                m_serializer.AppendElement(ele, parentEl);
            }
            
            parentRelations.Dispose();

            var childRelations = m_table.ChildRelations.Where(c => c.ParentTableName == m_table.Name && c.ChildTableName == m_table.Name).ToData();

            if (childRelations.Any())
            {
                var childEl = m_serializer.CreateElement("ChildRelations");

                m_relationSerializer.WriteRelations(childRelations, childEl);

                m_serializer.AppendElement(ele, childEl);
            }
            
            childRelations.Dispose();

            return ele;
        }
        
        private void WriteTableProperties(T tableEl, string xName)
        {
            Serializer.WriteXProperties(m_serializer, tableEl, xName, m_table.ExtProperties);
        }

        public bool DeserializeSchema(T element, bool buildinDefault = false)
        {
            m_table.EnforceConstraints = m_serializer.GetAttributeValue(element, "EnforceConstraints") == "true";
            m_table.CaseSensitive = m_serializer.GetAttributeValue(element, "CaseSensitive") == "true";
            m_table.Capacity = Tool.IntParseFast(m_serializer.GetAttributeValue(element, "RowCapacity"), 0);

            m_table.DataColumnInfo.DeserializeColumns(m_table, m_serializer, element, buildinDefault);

            foreach (var tableColumn in m_table.DataColumnInfo.Columns)
            {
                if (tableColumn.HasIndex)
                {
                    m_table.AddIndex(tableColumn.ColumnHandle, tableColumn.IsUnique);
                }
            }
            
            var multiColIndex = m_serializer.GetElement(element, "MultiColumnIndex");

            if (multiColIndex != null)
            {
                var indexInfoEls = m_serializer.GetElements(multiColIndex, "IndexInfo");

                foreach (var infoEl in indexInfoEls)
                {
                    var unique = m_serializer.GetAttributeValue(infoEl, "Unique") == "true";

                    var colEls = m_serializer.GetElements(infoEl, "Columns");

                    var list = new Data<string>();

                    foreach (var colEl in colEls)
                    {
                        list.Add(m_serializer.GetElementValue(colEl));
                    }

                    if (list.Any())
                    {
                        m_table.AddMultiColumnIndex(list, unique);
                    }

                    list.Dispose();
                }
            }

            var parentRelations =
                m_serializer.GetElements(m_serializer.GetElement(element, "ParentRelations"), "Item") ??
                Enumerable.Empty<T>();

            foreach (var parentRelation in parentRelations)
            {
                var relation = m_relationSerializer.ParseRelation(parentRelation, tableName => m_table, true);

                if (relation == null)
                {
                    continue;
                }

                if (m_table.ParentRelationsMap == null)
                {
                    m_table.ParentRelationsMap = new Map<string, DataRelation>(StringComparer.OrdinalIgnoreCase);
                }

                m_table.ParentRelationsMap[relation.relationName] = relation;

                foreach (var col in relation.ChildColumns)
                {
                    m_table.RegisterChildRelation(relation.relationName, col.ColumnHandle);
                }
            }

            var childRelationsEl = m_serializer.GetElement(element, "ChildRelations");
            
            var childRelations = m_serializer.GetElements(childRelationsEl, "Item") ?? Enumerable.Empty<T>();

            foreach (var childRelation in childRelations)
            {
                var relation = m_relationSerializer.ParseRelation(childRelation, tableName => m_table, true);

                if (relation == null)
                {
                    continue;
                }

                if (m_table.ChildRelationsMap == null)
                {
                    m_table.ChildRelationsMap = new Map<string, DataRelation>(StringComparer.OrdinalIgnoreCase);
                }

                m_table.ChildRelationsMap[relation.relationName] = relation;

                foreach (var col in relation.ParentColumns)
                {
                    m_table.RegisterParentRelation(relation.relationName, col.ColumnHandle);
                }
            }

            var xPropertiesEl = m_serializer.GetElement(element, "XProperties");

            if (xPropertiesEl != null)
            {
                ReadTableExtProperties(xPropertiesEl);
            }

            return m_serializer.GetAttributeValue(element, "IsReadOnly") == "true";
        }

        private void ReadTableExtProperties(T extPropertiesEl)
        {
            foreach (var xPropEl in m_serializer.GetElements(extPropertiesEl))
            {
                var xPropName = m_serializer.GetElementName(xPropEl);
                var xPropValue = m_serializer.GetElementValue(xPropEl);
                
                var xPropTableValue = Serializer.DeserializeXPropValue(m_serializer, m_table.Name, m_table.Namespace, xPropEl, xPropValue, xPropName);
                
                m_table.SetXProperty(xPropName, xPropTableValue);
            }
        }
        
        public void DeserializeData([NotNull] T source)
        {
            var tableSerializer = m_table;
            
            var values = ArrayPool<object>.Shared.Rent(tableSerializer.DataColumnInfo.ColumnsCount);

            Array.Clear(values, 0, values.Length);
            
            var loadState = tableSerializer.BeginLoad();

            try
            {
                int colCount = 0;
                
                foreach (T element in m_serializer.GetElements(source, "Row"))
                {
                    bool anyValue = false;

                    var columns = new Data<CoreDataColumn>(colCount);
                    var columnValue = new Data<T>(colCount);
                    
                    var colValues = m_serializer.GetElements(element);

                    foreach (var colValue in colValues)
                    {
                        var elementName = m_serializer.GetElementName(colValue);
                        var elementValue = m_serializer.GetElementValue(colValue);

                        if (tableSerializer.DataColumnInfo.ColumnMappings.TryGetValue(elementName, out var dataColumn))
                        {
                            var storageType = dataColumn.Type;

                            if (string.IsNullOrEmpty(elementValue) == false)
                            {
                                if (storageType == TableStorageType.UserType ||
                                    dataColumn.TypeModifier == TableStorageTypeModifier.Complex &&
                                    string.IsNullOrEmpty(elementValue) == false)
                                {
                                    var dataType = dataColumn.DataType;

                                    if (dataType != null)
                                    {
                                        if (dataColumn.TypeModifier == TableStorageTypeModifier.Complex)
                                        {
                                            var val = Activator.CreateInstance(dataType);

                                            m_serializer.ReadUserType(elementValue, val);

                                            values[dataColumn.ColumnHandle] = val;
                                        }
                                        else
                                        {
                                            values[dataColumn.ColumnHandle] = CoreDataTable.ConvertStringToObject(storageType, dataColumn.TypeModifier, elementValue, dataType);
                                        }
                                    }
                                }
                                else
                                {
                                    values[dataColumn.ColumnHandle] = CoreDataTable.ConvertStringToObject(storageType, dataColumn.TypeModifier, elementValue, dataColumn.DataType);
                                }
                            }
                            else
                            {
                                values[dataColumn.ColumnHandle] = null;
                            }

                            columns.Add(dataColumn);
                            columnValue.Add(colValue);

                            anyValue = true;
                        }
                    }

                    if (anyValue)
                    {
                        ImportDeserializedRow(element, tableSerializer, values, columns, columnValue);
                    }

                    colCount = columns.Count;
                    
                    columns.Dispose();
                    columnValue.Dispose();
                }
            }
            finally
            {
                ArrayPool<object>.Shared.Return(values, true);

                loadState.EndLoad();
            }
        }

        protected virtual int ImportDeserializedRow(T element, CoreDataTable tableSerializer, object[] values,
            Data<CoreDataColumn> columnHandles, Data<T> colElements)
        {
            var xPropertiesEl = m_serializer.GetElement(element, "XProperties");

            var row = tableSerializer.ImportRowDirty(RowState.Unchanged, values);

            if (xPropertiesEl != null)
            {
                ReadRowExtProperties(xPropertiesEl, row);
            }

            return row;
        }

        protected void ReadRowExtProperties(T extPropertiesEl, int rowHandle)
        {
            foreach (var xPropEl in m_serializer.GetElements(extPropertiesEl))
            {
                var xPropName = m_serializer.GetElementName(xPropEl);
                var xPropValue = m_serializer.GetElementValue(xPropEl);
                
                var xPropTableValue = Serializer.DeserializeXPropValue(m_serializer, m_table.Name, m_table.Namespace, xPropEl, xPropValue, xPropName);
                
                m_table.StateInfo.SetRowExtendedProperty(rowHandle, xPropName, xPropTableValue, null, null);
            }
        }
    }
}