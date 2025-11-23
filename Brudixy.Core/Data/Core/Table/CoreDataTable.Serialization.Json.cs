using System;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        public JElement ToJson(SerializationMode writeMode = SerializationMode.DataOnly)
        {
            var root = new JElement("Table");
            root.AddAttribute(new JAttribute("TableName", Name));
            root.AddAttribute(new JAttribute("V", Serializer.Version.ToString()));
         
            var serializer = new JsonSerializer();

            DatasetToJsonCore(writeMode, serializer, root);

            var jsonDataTableSerializer = CreateTableSerializer(serializer);

            if (writeMode is SerializationMode.Full or SerializationMode.SchemaOnly)
            {
                var simpleSchema = jsonDataTableSerializer.SerializeSchema();
                root.AddElement(new JElement("Metadata", simpleSchema));
            }

            if (writeMode != SerializationMode.SchemaOnly)
            {
                jsonDataTableSerializer.WriteDataTo(root);
            }

            return root;
        }

        public void LoadFromJson([NotNull] JElement json)
        {
            var isReadonly = MetaFromJsonCore(json);
            LoadDataFromJson(json);
            TableIsReadOnly = isReadonly;
        }

        public void LoadDataFromJson([NotNull] JElement source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var version = source.GetAttribute("V")?.ToString();

            Serializer.CheckFormatIsBackwardCompatible(version, Name, "json");

            DatasetDataFromJsonCore(source, LoadTablesMode.DataOnly);

            var serializer = CreateTableSerializer(new JsonSerializer());

            serializer.DeserializeData(source);
        }

        public void LoadMetadataFromJson([NotNull] JElement json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            TableIsReadOnly = MetaFromJsonCore(json);
        }

        private bool MetaFromJsonCore(JElement json)
        {
            var tableName = json.GetAttribute("TableName")?.ToString();
            var version = json.GetAttribute("V")?.ToString();

            Serializer.CheckFormatIsBackwardCompatible(version, tableName ?? string.Empty, "json");

            Name = tableName ?? Name;

            DatasetSchemaFromJson(json);

            var metaData = (JElement)json.GetElement("Metadata");

            if (metaData != null)
            {
                var jsonDataTableSerializer = CreateTableSerializer(new JsonSerializer());

                return jsonDataTableSerializer.DeserializeSchema(metaData);
            }

            return IsReadOnly;
        }

        void IJsonSerializable.FromJson(JElement element) => LoadFromJson(element);

        JElement IJsonSerializable.ToJson() => ToJson(SerializationMode.Full);
    }
}