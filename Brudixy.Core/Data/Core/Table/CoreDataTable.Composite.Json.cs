using System;
using System.Text.Json.Nodes;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        private JElement DatasetToJsonCore(SerializationMode mode, JsonSerializer serializer, JElement root)
        {
            var dataSetSerializer = Serializer.DatasetSerializer(this, serializer);

            var tableEl = serializer.CreateElement("TableData");
    
            var ele = serializer.CreateElement("TableData");

            foreach (var dataTable in Tables)
            {
                var jsonObject = dataTable.ToJson((SerializationMode)mode);

                serializer.AppendElement(ele, jsonObject);
            }

            tableEl.Value = ele;

            serializer.AppendElement(root, tableEl);
    
            if (mode is SerializationMode.Full or SerializationMode.SchemaOnly)
            {
                var attribute = serializer.CreateAttribute("TableMeta");

                attribute.Value = dataSetSerializer.GetSimpleSchema();

                serializer.AppendAttribute(root, attribute);
            }

            return root;
        }

        protected void DatasetSchemaFromJson([NotNull] JElement json)
        {
            DatasetDataFromJsonCore(json, LoadTablesMode.SchemaOnly);
            
            DatasetSchemaFromJsonCore(json);
        }

        private void DatasetSchemaFromJsonCore(JElement json)
        {
            var jsonSerializer = new JsonSerializer();
            
            var serializer = Serializer.DatasetSerializer(this, jsonSerializer);
            
            var metaData = json.GetAttribute("TableMeta");
            if (metaData != null)
            {
                serializer.ReadSimpleSchema((JElement)metaData);
            }
        }

        private void DatasetDataFromJsonCore(JElement json, LoadTablesMode mode)
        {
            if (json.Name != "Table")
            {
                throw new ArgumentException("Invalid json");
            }

            var serializer = new JsonSerializer();

            var datasetData = json.GetElement("TableData");

            if (datasetData != null)
            {
                var elements = serializer.GetElements((JElement)datasetData);

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
                        case LoadTablesMode.DataOnly: dataTable.LoadDataFromJson(tableEl); break;
                        case LoadTablesMode.SchemaOnly: dataTable.LoadMetadataFromJson(tableEl); break;
                        default: dataTable.LoadFromJson(tableEl); break;
                    }
            
                    dataLoadState.EndLoad();
                }
            }
        }
    }
}