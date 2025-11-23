using System.Xml;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class DataTableSerializer<T, V> : CoreDataTableSerializer<T, V>
    {
        protected DataTable Table => (DataTable)m_table;
        
        public DataTableSerializer(DataTable table, SerializerAdapter<T, V> serializer) : base(table, serializer)
        {
        }

        protected override T SerializeRow(T element, int row)
        {
            var rowElement = base.SerializeRow(element, row);

            var dataTable = Table;

            var annotations = dataTable.m_rowAnnotations;

            if (annotations != null)
            {
                var rowInfoDataItems = annotations.RowAnnotations;

                foreach (var item in rowInfoDataItems)
                {
                    var rowMetaInfo = (string)item.Value.GetData(row);

                    if (string.IsNullOrEmpty(rowMetaInfo) == false)
                    {
                        m_serializer.AppendAttribute(rowElement, m_serializer.CreateAttribute("Row" + item.Key, rowMetaInfo));
                    }
                }
            }

            var xPropertyInfos = dataTable.m_rowXPropertyAnnotations?.Storage?.ElementAtOrDefault(row);

            if (xPropertyInfos != null)
            {
                WriteXPropertyInfos(m_serializer, rowElement, "XPropertyInfo", xPropertyInfos);
            }
            
            return rowElement;
        }
        
        internal static void WriteXPropertyInfos<T, V>(SerializerAdapter<T, V> serializer, T col, string xName, Map<string, Map<string, object>> dict)
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

                    var xPropValueAnnotations = kv.Value;

                    if (xPropValueAnnotations?.Count > 0)
                    {
                        var xItem = serializer.CreateElement(XmlConvert.EncodeName(kv.Key));

                        Serializer.WriteXProperties(serializer, xItem, "Annotations", xPropValueAnnotations);
                        
                        serializer.AppendElement(xPropsItem, xItem);
                    }
                }

                serializer.AppendElement(col, xPropsItem);
            }
        }
        
        protected IEnumerable<(string key, object value)> ReadRowXPropertyInfos(T extPropertiesEl)
        {
            foreach (var xPropEl in m_serializer.GetElements(extPropertiesEl))
            {
                var xPropName = m_serializer.GetElementName(xPropEl);
                var xPropValue = m_serializer.GetElementValue(xPropEl);
                
                var xPropTableValue = Serializer.DeserializeXPropValue(m_serializer, m_table.Name, m_table.Namespace, xPropEl, xPropValue, xPropName);

                yield return (xPropName, xPropTableValue);
            }
        }
        
        private void ReadRowXPropertyInfo(int rowHandle, T extPropertiesEl)
        {
            var dataTable = Table;

            foreach (var xPropEl in m_serializer.GetElements(extPropertiesEl))
            {
                var annotationElements = m_serializer.GetElements(xPropEl, "Annotations");

                var xPropCode = m_serializer.GetElementName(xPropEl);

                foreach (var annotationElement in annotationElements)
                {
                    foreach (var kv in ReadRowXPropertyInfos(annotationElement))
                    {
                        dataTable.SetXPropertyAnnotation(rowHandle, xPropCode, kv.key, kv.value, null);
                    }
                }
            }
        }

        internal override T SerializeRowCellValue(T element, int rowHandle, CoreDataColumn column, object value)
        {
            var dataTable = Table;

            var columnInfo = dataTable.DataColumnInfo;

            if (columnInfo.ColumnHasExpression(column.ColumnHandle))
            {
                var expressionValue = dataTable.ExpressionValuesCache.GetExpressionValue(column.ColumnHandle, rowHandle);

                if (expressionValue == null)
                {
                    value = dataTable.GetDefaultNullValue<object>(column);
                }
                else
                {
                    value = expressionValue;
                }
            }
            
            var colEl = base.SerializeRowCellValue(element, rowHandle, column, value);

            if (dataTable.RowCellAnnotations is null || column.ColumnHandle >= dataTable.RowCellAnnotations.Count)
            {
                return colEl;
            }
            
            var dataItem = dataTable.RowCellAnnotations[column.ColumnHandle];

            if (dataItem.ValueInfo.RowAnnotations != null)
            {
                foreach (var kv in dataItem.ValueInfo.RowAnnotations)
                {
                    var data = (string)kv.Value.GetData(rowHandle);
                    
                    if (string.IsNullOrEmpty(data) == false)
                    {
                        m_serializer.AppendAttribute(colEl, m_serializer.CreateAttribute("Cell" + kv.Key, data));                    
                    }
                }
            }
            
            return colEl;
        }

        protected override int ImportDeserializedRow(T element, 
            CoreDataTable tableSerializer, 
            object[] values,
            Data<CoreDataColumn> columns,
            Data<T> colElements)
        {
            var rowHandle = base.ImportDeserializedRow(element, tableSerializer, values, columns, colElements);

            var dataTable = Table;

            var rowAnnotations = m_serializer.GetAttributes(element).Where(a => a.StartsWith("Row"));

            foreach (var annotation in rowAnnotations)
            {
                var rowFault = m_serializer.GetAttributeValue(element, annotation);

                if (string.IsNullOrEmpty(rowFault))
                {
                    dataTable.SetRowAnnotation(rowHandle, rowFault, null, annotation.Substring(0, 3));
                }
            }
            
            var rowXPropertyInfo = m_serializer.GetElement(element, "XPropertyInfo");

            if (rowXPropertyInfo != null)
            {
                ReadRowXPropertyInfo(rowHandle, rowXPropertyInfo);
            }

            Map<CoreDataColumn, Map<string, string>> map = null;

            bool anyMeta = false;

            for (var index = 0; index < colElements.Count; index++)
            {
                var column = columns[index];
                var colValue = colElements[index];

                var colAnnotations = m_serializer.GetAttributes(element).Where(a => a.StartsWith("Cell"));

                foreach (var annotation in colAnnotations)
                {
                    var attributeValue = m_serializer.GetAttributeValue(colValue, annotation);

                    if (string.IsNullOrEmpty(attributeValue))
                    {
                        var key = annotation.Substring(0, 4);

                        anyMeta = true;

                        if (map == null)
                        {
                            map = new();
                        }

                        if (map.TryGetValue(column, out var cellInfo) == false)
                        {
                            map[column] = cellInfo = new Map<string, string>();
                        }

                        cellInfo[key] = attributeValue;
                    }
                }
            }

            if (anyMeta)
            {
                foreach (var kv in map)
                {
                    var dataColumn = (DataColumn)kv.Key;

                    var rowCellAnnotation = dataTable.GetRowCellAnnotation(dataColumn);

                    foreach (var typeVal in kv.Value)
                    {
                        rowCellAnnotation.SetCellAnnotation(rowHandle, typeVal.Value, null, typeVal.Key);
                    }
                }
            }

            return rowHandle;
        }
    }
}