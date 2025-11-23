using System;
using System.Xml.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataTable
    {
         private void DatasetToXElementCore(SerializationMode mode, XElement root, DataSetSerializer<XElement, XAttribute> dataSetSerializer)
         {
             var tableData = new XElement("TableData");

             foreach (var dataTable in Tables)
             {
                 tableData.Add(dataTable.ToXml(mode));
             }

             if (tableData.HasElements)
             {
                 root.Add(tableData);
             }
             
             if (mode is SerializationMode.SchemaOnly or SerializationMode.Full)
             {
                 root.Add(dataSetSerializer.GetSimpleSchema());
             }
         }

        public void DatasetSchemaFromXElement([NotNull] XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot deserialize schema {TableName} table dataset from XML because it is in readonly.");
            }
            
            DatasetSchemaFromXElementCore(element);
        }

        protected void DatasetSchemaFromXElementCore(XElement element)
        {
            DatasetFromXElementCore(element, LoadTablesMode.SchemaOnly);

            var dataSetSerializer = Serializer.DatasetSerializer(this, new XmlSerializer());

            var metaData = element.Element("TableMeta");

            if (metaData != null)
            {
                dataSetSerializer.ReadSimpleSchema(metaData);
            }
        }

        private void DatasetFromXElementCore(XElement element, LoadTablesMode mode)
        {
            var dataEl = element.Element("TableData");

            if (dataEl != null)
            {
                foreach (var tableEl in dataEl.Elements())
                {
                    var tableName = tableEl.Attribute("TableName")?.Value;

                    if (string.IsNullOrEmpty(tableName))
                    {
                        continue;
                    }

                    var dataTable = TryGetTable(tableName) ?? AddTable(tableName);

                    var dataLoadState = dataTable.BeginLoad();

                    switch (mode)
                    {
                        case LoadTablesMode.DataOnly:  dataTable.LoadDataFromXml(tableEl); break;
                        case LoadTablesMode.SchemaOnly:  dataTable.LoadMetadataFromXml(tableEl); break;
                        default: dataTable.LoadFromXml(tableEl); break;
                    }
                    
                    dataLoadState.EndLoad();
                }
            }
        }
    }
}