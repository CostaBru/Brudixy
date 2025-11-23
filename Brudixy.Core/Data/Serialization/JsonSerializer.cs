using System.Collections.Generic;
using System.Text.Json.Nodes;
using Brudixy.Interfaces;

namespace Brudixy
{
    internal class JsonSerializer : SerializerAdapter<JElement, JAttribute>
    {
        internal override JElement CreateElement(string name)
        {
            var jsonObject = new JElement(name);
            return jsonObject;
        }

        internal override JElement CreateElement(string name, string value)
        {
            var jsonObject = new JElement(name, value);
            return jsonObject;
        }

        internal override JAttribute CreateAttribute(string name, string value = null)
        {
            return new JAttribute(name, value);
        }

        internal override void AppendElement(JElement element, JElement anotherElement)
        {
            element.AddElement(anotherElement);
        }

        internal override void AppendAttribute(JElement element, JAttribute anotherAttribute)
        {
            element.AddAttribute(anotherAttribute);
        }

        internal override string GetElementValue(JElement element)
        {
            return element.Value?.ToString() ?? string.Empty;
        }

        internal override string GetElementName(JElement element)
        {
            return element.Name;
        }

        internal override JElement GetElement(JElement element, string name)
        {
            if (element.Value is JElement array)
            {
                foreach (var child in array.Elements)
                {
                    if (GetElementName(child) == name)
                    {
                        return child;
                    }
                }
            }

            foreach (var property in element.Elements)
            {
                if (GetElementName(property) == name)
                {
                    return property;
                }
            }

            return null;
        }

        internal override IEnumerable<JElement> GetElements(JElement element)
        {
            foreach (var child in element.Elements)
            {
                yield return child;
            }
        }

        internal override IEnumerable<JElement> GetElements(JElement element, string column)
        {
            if (element == null)
            {
                yield break;
            }

            bool any = false;

            if (element.Value is JElement array)
            {
                foreach (var child in array.Elements)
                {
                    if (GetElementName(child) == column)
                    {
                        any = true;
                        yield return child;
                    }
                }
            }

            if (!any)
            {
                foreach (var property in element.Elements)
                {
                    if (GetElementName(property) == column)
                    {
                        yield return property;
                    }
                }
            }
        }

        internal override string GetAttributeValue(JElement element, string name, string defaultIfNull = null)
        {
            return element.GetAttribute(name)?.ToString() ?? defaultIfNull;
        }

        internal override IEnumerable<string> GetAttributes(JElement element)
        {
            foreach (var property in element.Attributes)
            {
                yield return property.Name;
            }
        }

        internal override void WriteComments(JElement element)
        {
            // No implementation needed for comments
        }

        internal override void ReadUserType(string value, object instance)
        {
            var jsonNode = JsonNode.Parse(value);

            var jElement = JElement.Parse(jsonNode);
            
            ((IJsonSerializable)instance).FromJson(jElement);
        }

        public override string WriteUserType(TableStorageType tableStorageType,
            TableStorageTypeModifier tableStorageTypeModifier, 
            object instance)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            if (instance is IJsonSerializable jsn)
            {
                return jsn.ToJson().ToString();
            }

            return CoreDataTable.ConvertObjectToString(tableStorageType, tableStorageTypeModifier, instance, serializationType: CoreDataTable.SerializationType.Json);
        }

        public override string ConvertObjectToString(TableStorageType tableStorageType, TableStorageTypeModifier tableStorageTypeModifier, object obj)
        {
            return CoreDataTable.ConvertObjectToString(tableStorageType, tableStorageTypeModifier, obj, serializationType: CoreDataTable.SerializationType.Json);
        }
    }
}