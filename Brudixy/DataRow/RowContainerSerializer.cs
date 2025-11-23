using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class RowContainerSerializer<T, V> : CoreRowContainerSerializer<T, V>
    {
        public DataRowContainer Container => (DataRowContainer)base.m_container;
        
        public RowContainerSerializer(DataRowContainer container, SerializerAdapter<T, V> serializer) : base(container, serializer)
        {
        }

        public override T WriteDataTo(T element)
        {
            var rowElement = base.WriteDataTo(element);

            var dr = Container;
            
            var rowFault = dr.GetRowFault();

            if (string.IsNullOrEmpty(rowFault) == false)
            {
                m_serializer.AppendAttribute(rowElement, m_serializer.CreateAttribute("Fault", rowFault));
            }

            var rowError = dr.GetRowError();

            if (string.IsNullOrEmpty(rowError) == false)
            {
                m_serializer.AppendAttribute(rowElement, m_serializer.CreateAttribute("Error", rowError));
            }

            var rowWarn = dr.GetRowWarning();

            if (string.IsNullOrEmpty(rowWarn) == false)
            {
                m_serializer.AppendAttribute(rowElement, m_serializer.CreateAttribute("Warning", rowWarn));
            }

            var rowInfo = dr.GetRowInfo();

            if (string.IsNullOrEmpty(rowInfo) == false)
            {
                m_serializer.AppendAttribute(rowElement, m_serializer.CreateAttribute("Info", rowInfo));
            }

            return rowElement;
        }

        protected override T WriteCellValue(ICoreTableReadOnlyColumn col, string objectToString, T rowElement)
        {
            var colEl = base.WriteCellValue(col, objectToString, rowElement);

            var dr = Container;

            var cellError = dr.GetCellError(col.ColumnName);

            if (string.IsNullOrEmpty(cellError) == false)
            {
                m_serializer.AppendAttribute(colEl, m_serializer.CreateAttribute("Error", cellError));
            }

            var cellWarning = dr.GetCellWarning(col.ColumnName);

            if (string.IsNullOrEmpty(cellWarning) == false)
            {
                m_serializer.AppendAttribute(colEl, m_serializer.CreateAttribute("Warning", cellWarning));
            }

            var cellInfo = dr.GetCellInfo(col.ColumnName);

            if (string.IsNullOrEmpty(cellInfo) == false)
            {
                m_serializer.AppendAttribute(colEl, m_serializer.CreateAttribute("Info", cellInfo));
            }

            return colEl;
        }

        protected override T SerializeColumn(T ele, CoreDataColumnContainer column, Set<string> pkCols)
        {
            var col = base.SerializeColumn(ele, column, pkCols);
            
            if (column is DataColumnContainer dc)
            {
                if (dc.Caption != column.ColumnName && string.IsNullOrEmpty(dc.Caption) == false)
                {
                    m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("Caption", dc.Caption));
                }

                if (string.IsNullOrEmpty(dc.Expression) == false)
                {
                    m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("Expression", dc.Expression));
                }

                if (dc.IsReadOnly && string.IsNullOrEmpty(dc.Expression))
                {
                    m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("ReadOnly", "true"));
                }

                if (dc.AllowNull == false)
                {
                    m_serializer.AppendAttribute(col, m_serializer.CreateAttribute("AllowNull", "false"));
                }
            }
            
            return col;
        }

        public override void Deserialize(T element)
        {
            var propsBuilder = CreateBuilder();

            var (metadataProps, containerProps) = DeserializeCore(element, out var isReadOnly, propsBuilder);

            var rowContainer = Container;

            var containerMetadataProps = new CoreContainerMetadataProps(
                metadataProps.TableName,
                metadataProps.Columns,
                metadataProps.ColumnMap, 
                metadataProps.KeyColumns,
                0);
            
            var containerDataProps = (ContainerDataProps)containerProps;
            
            T rowElement = m_serializer.GetElement(element, "Row");

            if (rowElement != null)
            {
                bool anyValue = false;
                bool anyMeta = false;

                var colValues = m_serializer.GetElements(element);

                foreach (var colValue in colValues)
                {
                    var elementName = m_serializer.GetElementName(colValue);

                    if (metadataProps.ColumnMap.TryGetValue(elementName, out var column) == false)
                    {
                        continue;
                    }

                    if (column != null)
                    {
                        if(containerDataProps.CellAnnotations.TryGetValue(column.ColumnName, out var colAnnMap) == false)
                        {
                            containerDataProps.CellAnnotations[column.ColumnName] = colAnnMap = new Map<string, object>();
                        }
                        
                        var celAnnotations = m_serializer.GetAttributes(colValue).Where(a => a.StartsWith("Cell"));

                        foreach (var annotation in celAnnotations)
                        {
                            var cellAnn = m_serializer.GetAttributeValue(colValue, annotation);

                            var annotationType = annotation.Substring(3);

                            colAnnMap[annotationType] = cellAnn;
                        }
                    }
                }

                containerDataProps.RowAnnotations = new();
                
                foreach (var rowAnn in m_serializer.GetAttributes(element).Where(a => a.StartsWith("Row")))
                {
                    var key = rowAnn.Substring(3);

                    containerDataProps.RowAnnotations[key] = m_serializer.GetAttributeValue(element, rowAnn);
                }
            }

            rowContainer.Init(containerMetadataProps, containerDataProps);

            rowContainer.IsReadOnly = isReadOnly;
        }

        protected override CoreContainerDataPropsBuilder CreateBuilder()
        {
            return new ContainerDataPropsBuilder();
        }

        protected override CoreDataColumnContainerBuilder CreateColumnBuilderInstance()
        {
            return new DataColumnContainerBuilder();
        }

        protected override CoreDataColumnContainer CreateColumnInstance(CoreDataColumnObj columnObj)
        {
            return new DataColumnContainer((DataColumnObj)columnObj);
        }

        protected override CoreDataColumnContainerBuilder DeserializeColumn( 
            T colElement,
            string tableName,
            int colIndex)
        {
            var columnContainer = (DataColumnContainerBuilder)base.DeserializeColumn(colElement, tableName, colIndex);
            
            var caption = m_serializer.GetAttributeValue(colElement, "Caption");
            var expression = m_serializer.GetAttributeValue(colElement,"Expression");
            var readOnly = m_serializer.GetAttributeValue(colElement,"ReadOnly") == "true" ? true : new bool?();

            columnContainer.Caption = caption;
            columnContainer.Expression = expression;

            if (readOnly ?? false)
            {
                columnContainer.IsReadOnly = true;
            }
            
            return columnContainer;
        }
    }
}