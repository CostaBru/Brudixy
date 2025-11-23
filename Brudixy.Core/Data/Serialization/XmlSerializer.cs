using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Brudixy.Interfaces;

namespace Brudixy
{
    internal class XmlSerializer : SerializerAdapter<XElement, XAttribute>
    {
        internal override XElement CreateElement(string name) => new (name);

        internal override XElement CreateElement(string name, string value) => new (name, value);

        internal override XAttribute CreateAttribute(string name, string value) => new (name, value);

        internal override void AppendElement(XElement element, XElement anotherElement) => element.Add(anotherElement);

        internal override void AppendAttribute(XElement element, XAttribute anotherAttribute) => element.Add(anotherAttribute);
        internal override string GetElementValue(XElement element) => element?.Value;

        internal override string GetElementName(XElement element) => element.Name?.ToString() ?? string.Empty;

        internal override XElement GetElement(XElement element, string name) => element?.Element(name);

        internal override IEnumerable<XElement> GetElements(XElement element) => element?.Elements() ?? Enumerable.Empty<XElement>();

        internal override IEnumerable<XElement> GetElements(XElement element, string name) => element?.Elements(name) ?? Enumerable.Empty<XElement>();

        internal override string GetAttributeValue(XElement element, string name, string defaultIfNull = null) => element?.Attribute(name)?.Value;
        internal override IEnumerable<string> GetAttributes(XElement element) => element?.Attributes().Select(a => a.Name.LocalName);

        internal override void WriteComments(XElement ele)
        {
        }

        internal override void ReadUserType(string value, object instance)
        {
            ((IXmlSerializable)instance).FromXml(XElement.Parse(value));
        }

        public override string WriteUserType(TableStorageType tableStorageType,
            TableStorageTypeModifier tableStorageTypeModifier, object instance)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            if (instance is IXmlSerializable xml)
            {
                return xml.ToXml().ToString();
            }

            return CoreDataTable.ConvertObjectToString(tableStorageType, tableStorageTypeModifier, instance, serializationType: CoreDataTable.SerializationType.Xml);
        }

        public override string ConvertObjectToString(TableStorageType tableStorageType,
            TableStorageTypeModifier tableStorageTypeModifier, object obj)
        {
            return CoreDataTable.ConvertObjectToString(tableStorageType, tableStorageTypeModifier, obj, serializationType: CoreDataTable.SerializationType.Xml);
        }
    }
}