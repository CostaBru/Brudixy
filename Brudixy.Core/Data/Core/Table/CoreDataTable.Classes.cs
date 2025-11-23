using System;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Brudixy.Interfaces.Tools;
using Konsarpoo.Collections;

namespace Brudixy
{
    partial class CoreDataTable
    {
        public static object ConvertStringToObject(TableStorageType typeCode, TableStorageTypeModifier typeModifier,
            string value, Type type = null, SerializationType serializationType = SerializationType.Default)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (type != null)
            {
                if (typeModifier == TableStorageTypeModifier.Complex)
                {
                    var val = Activator.CreateInstance(type);

                    if (serializationType == SerializationType.Default)
                    {
                        try
                        {
                            ((IXmlSerializable)val).FromXml(XElement.Parse(value));
                        }
                        catch
                        {
                            ((IJsonSerializable)val).FromJson(JElement.Parse(value));
                        }
                    }
                    else if (serializationType == SerializationType.Xml)
                    {
                        ((IXmlSerializable)val).FromXml(XElement.Parse(value));
                    }
                    else if (serializationType == SerializationType.Json)
                    {
                        ((IJsonSerializable)val).FromJson(JElement.Parse(value));
                    }

                    return val;

                }
                
                if (typeModifier == TableStorageTypeModifier.Simple && typeCode == TableStorageType.UserType)
                {
                    return TableStorageStringHelper.ConvertStringToObject(typeCode, typeModifier, value, type);
                }
            }


            if (type == null)
            {
                type = CoreDataTable.GetDataType(typeCode, typeModifier, false, null);
            }

            return TableStorageStringHelper.ConvertStringToObject(typeCode, typeModifier, value, type);
        }

        public static string ConvertObjectToString(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var type = value.GetType();

            var columnType = GetColumnType(type);
            
            return ConvertObjectToString(columnType.type, columnType.typeModifier, value, type);
        }
        
        public enum SerializationType
        {
            Default,
            Xml,
            Json
        }
        
        public static string ConvertObjectToString(TableStorageType typeCode, TableStorageTypeModifier typeModifierCode, object value, Type type = null, SerializationType serializationType = SerializationType.Default)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (typeModifierCode == TableStorageTypeModifier.Simple && typeCode == TableStorageType.UserType || typeModifierCode == TableStorageTypeModifier.Complex)
            {
                switch (serializationType)
                {
                    case SerializationType.Default:
                    {
                        if (value is IXmlSerializable xml)
                        {
                            return xml.ToXml().ToString();
                        }

                        break;
                    }
                    case SerializationType.Xml:
                    {
                        if (value is IXmlSerializable xml)
                        {
                            return xml.ToXml().ToString();
                        }

                        break;
                    }
                    case SerializationType.Json:
                    {
                        if (value is IJsonSerializable json)
                        {
                            return json.ToJson().ToString();
                        }

                        break;
                    }
                }
            }

            return TableStorageStringHelper.ConvertToString(value, typeCode, typeModifierCode);
        }

        private class IndexBuilder
        {
            private readonly CoreDataTable m_dataTable;
            private readonly int m_rowHandle;
            private Map<int, Map<int, IComparable>> m_values;

            public IndexBuilder(CoreDataTable dataTable, int rowHandle)
            {
                m_dataTable = dataTable;
                m_rowHandle = rowHandle;

                if (m_dataTable.MultiColumnIndexInfo.HasAny)
                {
                    m_values = new Map<int, Map<int, IComparable>>();

                    for (var i = 0; i < m_dataTable.MultiColumnIndexInfo.Indexes.Count; i++)
                    {
                        var dict = new Map<int, IComparable>();

                        var index = m_dataTable.MultiColumnIndexInfo.Indexes[i];

                        foreach (var indexColumn in index.Columns)
                        {
                            dict[indexColumn] = null;
                        }

                        m_values[i] = dict;
                    }
                }
            }

            public void SetValue(int columnHandle, object value)
            {
                m_dataTable.IndexInfo.TryAddValueToIndex(m_dataTable, columnHandle, value, m_rowHandle);

                if (m_values != null)
                {
                    foreach (var indexValue in m_values.Values)
                    {
                        if (indexValue.ContainsKey(columnHandle))
                        {
                            indexValue[columnHandle] = value as IComparable;
                        }
                    }
                }
            }

            public void SetMultiColumnIndex()
            {
                if (m_values != null)
                {
                    for (int i = 0; i < m_values.Count; i++)
                    {
                        var indexesOfMany = m_dataTable.MultiColumnIndexInfo.Indexes[i];

                        var comparables = m_values[i];

                        var values = new IComparable[indexesOfMany.Columns.Length];

                        for (var index = 0; index < indexesOfMany.Columns.Length; index++)
                        {
                            var column = indexesOfMany.Columns[index];
                            if (comparables.TryGetValue(column, out var indexValue))
                            {
                                values[index] = indexValue;
                            }
                            else
                            {
                                break;
                            }
                        }

                        m_dataTable.MultiColumnIndexInfo.AddToIndex(m_dataTable, i, values, m_rowHandle);
                    }
                }
            }

            public void Dispose()
            {
                if (m_values != null)
                {
                    foreach (var value in m_values)
                    {
                        value.Value?.Dispose();
                    }

                    m_values.Dispose();
                }
            }
        }
    }
}
