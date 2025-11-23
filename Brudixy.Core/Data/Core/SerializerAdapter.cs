using System;
using System.Collections.Generic;
using Brudixy.Interfaces;

namespace Brudixy
{
    public abstract class SerializerAdapter<T, V>
    {
        internal abstract T CreateElement(string name);
        internal abstract T CreateElement(string name, string value);
        internal abstract V CreateAttribute(string name, string value = null);
        internal abstract void AppendElement(T element, T anotherElement);
        internal abstract void AppendAttribute(T element, V anotherAttribute);
        internal abstract string GetElementValue(T element);
        internal abstract string GetElementName(T element);
        internal abstract T GetElement(T element, string name);
        internal abstract IEnumerable<T> GetElements(T element);
        internal abstract IEnumerable<T> GetElements(T element, string column);
        internal abstract string GetAttributeValue(T element, string name, string defaultIfNull = null);
        internal abstract IEnumerable<string> GetAttributes(T element);
        internal abstract void WriteComments(T element);
        internal abstract void ReadUserType(string value, object instance);
        public abstract string WriteUserType(TableStorageType tableStorageType,
            TableStorageTypeModifier tableStorageTypeModifier, object value);
        public abstract string ConvertObjectToString(TableStorageType tableStorageType,
            TableStorageTypeModifier tableStorageTypeModifier, object value);

    }
}