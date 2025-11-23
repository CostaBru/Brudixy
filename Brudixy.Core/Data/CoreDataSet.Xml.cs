using System;
using System.Xml.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataSet
    {
        public XElement ToXElement(DatasetSerializationMode mode = DatasetSerializationMode.DataOnly)
        {
            var dataSetSerializer = Serializer.DatasetSerializer(this, new XmlSerializer());

            var root = new XElement("Dataset", new XAttribute("DatasetName", DataSetName ?? string.Empty));

            if (EnforceConstraints)
            {
                root.Add(new XAttribute("EnforceConstraints", "true"));
            }
            
            if (IsReadOnly)
            {
                root.Add(new XAttribute("IsReadOnly", "true"));
            }

            if (mode is DatasetSerializationMode.FullSchemaOnly or DatasetSerializationMode.Full or DatasetSerializationMode.DatasetSchemaOnly)
            {
                root.Add(dataSetSerializer.GetSimpleSchema());
            }

            if (mode is DatasetSerializationMode.Full or DatasetSerializationMode.DataOnly or DatasetSerializationMode.FullSchemaOnly)
            {
                var tableData = new XElement("DatasetData");

                foreach (var dataTable in Tables)
                {
                    tableData.Add(dataTable.ToXElement((TableSerializationMode)mode));
                }

                if (tableData.HasElements)
                {
                    root.Add(tableData);
                }
            }

            return root;
        }

        public void SchemaFromXElement([NotNull] XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            
            DataFromXElementCore(element, LoadTablesMode.SchemaOnly);
            
            SchemaFromXElementCore(element);
        }

        private void SchemaFromXElementCore(XElement element)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot deserialize schema {DataSetName} dataset from XML because it is in readonly.");
            }
            
            var xmlDataTableSerializer = Serializer.DatasetSerializer(this, new XmlSerializer());
            
            DataSetName = element.Attribute("DatasetName")?.Value ?? string.Empty;
            EnforceConstraints = element.Attribute("EnforceConstraints")?.Value == "true";

            var metaData = element.Element("DatasetMeta");

            if (metaData != null)
            {
                xmlDataTableSerializer.ReadSimpleSchema(metaData);
            }
            
            m_isReadOnly = element.Attribute("IsReadOnly")?.Value == "true";
        }

        public void DataFromXElement([NotNull] XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            DataFromXElementCore(element, LoadTablesMode.DataOnly);
        }

        private void DataFromXElementCore(XElement element, LoadTablesMode mode)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot deserialize {DataSetName} dataset from XML because it is in readonly.");
            }
            
            var dataEl = element.Element("DatasetData");

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
                        case LoadTablesMode.DataOnly:  dataTable.DataFromXElement(tableEl); break;
                        case LoadTablesMode.SchemaOnly:  dataTable.SchemaFromXElement(tableEl); break;
                        default: dataTable.FromXElement(tableEl); break;
                    }
                    
                    dataLoadState.EndLoad();
                }
            }
        }

        public void FromXElement(XElement element)
        {
            DataFromXElementCore(element, LoadTablesMode.Full);

            SchemaFromXElementCore(element);
        }
    }
}