using System;
using System.Xml.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    partial class CoreDataTable
    {
        public bool EqualSerializedValues([NotNull]CoreDataColumn column, string serializedValue, object cellValue)
        {
            if (string.IsNullOrEmpty(serializedValue) && cellValue == null)
            {
                return true;
            }

            if (column.Type == TableStorageType.Double)
            {
                var dv = (double)cellValue;

                return Math.Abs(dv - double.Parse(serializedValue)) < 0.001;
            }
            if (column.Type == TableStorageType.Single)
            {
                var dv = (float)cellValue;

                return Math.Abs(dv - float.Parse(serializedValue)) < 0.001;
            }

            var convertObjectToXml = ConvertObjectToString(column.Type, column.TypeModifier, cellValue, serializationType: SerializationType.Default);
            
            return convertObjectToXml == serializedValue;
        }
        
        protected virtual CoreDataTableSerializer<T, V> CreateTableSerializer<T, V>(SerializerAdapter<T, V> serializer)
        {
            return new CoreDataTableSerializer<T, V>(this, serializer);
        }
      
        public XElement ToXml(SerializationMode writeMode = SerializationMode.DataOnly)
        {
            var tableSerializer = CreateTableSerializer(new XmlSerializer());
            
            var root = new XElement("Table", 
                new XAttribute("TableName", Name),
                new XAttribute("V", Serializer.Version.ToString()));
            
            var dataSetSerializer = Serializer.DatasetSerializer(this, new XmlSerializer());
            
            DatasetToXElementCore(writeMode, root, dataSetSerializer);
            
            if (writeMode is SerializationMode.Full or SerializationMode.SchemaOnly)
            {
                root.Add(tableSerializer.SerializeSchema());
            }

            if (writeMode != SerializationMode.SchemaOnly)
            {
                tableSerializer.WriteDataTo(root);
            }
            
            return root;
        }

        public void LoadFromXml([NotNull]XElement source)
        {
            var isReadOnly = SchemaFromXmlCore(source);
            LoadDataFromXml(source);
            TableIsReadOnly = isReadOnly;
        }

        public void LoadDataFromXml([NotNull] XElement source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            var version = source.Attribute("V")?.Value;

            Serializer.CheckFormatIsBackwardCompatible(version, Name, "xml");
            
            DatasetFromXElementCore(source, LoadTablesMode.DataOnly);

            var xmlDataTableSerializer = CreateTableSerializer(new XmlSerializer());

            xmlDataTableSerializer.DeserializeData(source);
        }

        public void LoadMetadataFromXml([NotNull] XElement source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            TableIsReadOnly = SchemaFromXmlCore(source);
        }

        private bool SchemaFromXmlCore(XElement source)
        {
            var version = source.Attribute("V")?.Value;
            var tableName = source.Attribute("TableName")?.Value;

            Serializer.CheckFormatIsBackwardCompatible(version, tableName ?? string.Empty, "xml");

            Name = tableName ?? Name;

            DatasetSchemaFromXElementCore(source);

            var xmlDataTableSerializer = CreateTableSerializer(new XmlSerializer());

            var metaData = source.Element("Metadata");

            if (metaData != null)
            {
                return xmlDataTableSerializer.DeserializeSchema(metaData);
            }

            return IsReadOnly;
        }

        void IXmlSerializable.FromXml(XElement element) => LoadFromXml(element);

        XElement IXmlSerializable.ToXml() => ToXml(SerializationMode.Full);
    }
}
