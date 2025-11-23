using System;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Brudixy
{
    public partial class CoreDataSet
    {
        public JObject ToJson(DatasetSerializationMode mode = DatasetSerializationMode.DataOnly)
        {
            var serializer = new JsonSerializer();
            
            var dataSetSerializer = Serializer.DatasetSerializer(this, serializer);

            var root = serializer.CreateElement("Dataset");
            
            serializer.AppendAttribute(root, serializer.CreateAttribute("DatasetName", DataSetName));

            if (EnforceConstraints)
            {
                serializer.AppendAttribute(root, serializer.CreateAttribute("EnforceConstraints", "true"));
            }
            
            if (IsReadOnly)
            {
                serializer.AppendAttribute(root, serializer.CreateAttribute("IsReadOnly", "true"));
            }
            
            if (mode is DatasetSerializationMode.FullSchemaOnly or DatasetSerializationMode.Full or DatasetSerializationMode.DatasetSchemaOnly)
            {
                var attribute = serializer.CreateAttribute("DatasetMeta");

                attribute.Value = dataSetSerializer.GetSimpleSchema();

                serializer.AppendAttribute(root, attribute);
            }

            if (mode is DatasetSerializationMode.Full or DatasetSerializationMode.DataOnly or DatasetSerializationMode.FullSchemaOnly)
            {
                var tableEl = serializer.CreateElement("DatasetData");
                
                foreach (var dataTable in Tables)
                {
                    var jObject = dataTable.ToJson((TableSerializationMode)mode);
                    
                    serializer.AppendElement(tableEl, jObject);
                }
                
                var dataAttribute = serializer.CreateAttribute("DatasetData");

                dataAttribute.Value = tableEl;
                
                serializer.AppendAttribute(root, dataAttribute);
            }

            return (JObject)root;
        }

        public void FromJson([NotNull] JObject json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            
            DataFromJsonCore(json, LoadTablesMode.Full);

            SchemaFromJsonCore(json);
        }

        public void SchemaFromJson([NotNull] JObject json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            
            DataFromJsonCore(json, LoadTablesMode.SchemaOnly);
            
            SchemaFromJsonCore(json);
        }

        private void SchemaFromJsonCore(JObject json)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot deserialize schema {DataSetName} dataset from JSON because it is in readonly.");
            }
            
            var jsonSerializer = new JsonSerializer();
            
            var serializer = Serializer.DatasetSerializer(this, jsonSerializer);

            DataSetName = jsonSerializer.GetAttributeValue(json, "DatasetName");
            EnforceConstraints = jsonSerializer.GetAttributeValue(json, "EnforceConstraints") == "true";
            m_isReadOnly = jsonSerializer.GetAttributeValue(json, "IsReadOnly") == "true";
            
            var metaData = json["DatasetMeta"];
            if (metaData != null)
            {
                serializer.ReadSimpleSchema(metaData);
            }
        }

        public void DataFromJson([NotNull] JObject json)
        {
            DataFromJsonCore(json, LoadTablesMode.DataOnly);
        }

        private void DataFromJsonCore(JObject json, LoadTablesMode mode)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot deserialize {DataSetName} dataset from JSON because it is in readonly.");
            }
            
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Property(JsonSerializer.JsonNodeName)?.Value.ToString() != "Dataset")
            {
                throw new ArgumentException("Invalid json");
            }

            var serializer = new JsonSerializer();

            var datasetData = json["DatasetData"];

            if (datasetData != null)
            {
                var elements = serializer.GetElements(datasetData);

                foreach (var tableEl in elements)
                {
                    var tableName = serializer.GetAttributeValue(tableEl, "TableName");

                    if (string.IsNullOrEmpty(tableName))
                    {
                        continue;
                    }

                    var dataTable = TryGetTable(tableName) ?? AddTable(tableName);

                    var dataLoadState = dataTable.BeginLoad();
                    
                    switch (mode)
                    {
                        case LoadTablesMode.DataOnly: dataTable.DataFromJson((JObject)tableEl); break;
                        case LoadTablesMode.SchemaOnly: dataTable.SchemaFromJson((JObject)tableEl); break;
                        default: dataTable.FromJson((JObject)tableEl); break;
                    }
                    
                    dataLoadState.EndLoad();
                }
            }
        }
    }
}