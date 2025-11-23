using System;
using System.Collections.Generic;
using System.Linq;

namespace Brudixy
{
    internal class DataSetSerializer<T, V>
    {
        private readonly CoreDataTable m_dataSet;

        private readonly SerializerAdapter<T, V> m_serializer;
        private readonly RelationSerializer<T, V> m_relationSerializer;
        
        public DataSetSerializer(CoreDataTable dataSet, SerializerAdapter<T, V> serializer)
        {
            m_dataSet = dataSet;
            m_serializer = serializer;

            m_relationSerializer = new RelationSerializer<T, V>(serializer);
        }
        
        public T GetSimpleSchema(Func<string, IEnumerable<string>, HashSet<string>> tableFilter = null)
        {
            var ele = m_serializer.CreateElement("TableMeta");

            HashSet<string> filter = null;
            if (tableFilter != null)
            {
                filter = tableFilter(m_dataSet.TableName, m_dataSet.Tables.Select(col => col.Name));
            }
            
            if (m_dataSet.ExtProperties != null && m_dataSet.ExtProperties.Count > 0)
            {
                WriteTableProperties(ele, "XProperties");
            }

            foreach (var table in m_dataSet.Tables)
            {
                var skipTable = filter != null && filter.Contains(table.Name) == false;

                if (skipTable)
                {
                    continue;
                }

                var tableEl = m_serializer.CreateElement("Table");

                m_serializer.AppendAttribute(tableEl, m_serializer.CreateAttribute("TableName", table.Name));

                m_serializer.AppendElement(ele, tableEl);
            }
            
            var relEl = m_serializer.CreateElement("Relations");

            m_relationSerializer.WriteRelations(m_dataSet.TableRelations, relEl);

            m_serializer.AppendElement(ele, relEl);

            return ele;
        }
        
        private void WriteTableProperties(T dataSetEl, string xName)
        {
            Serializer.WriteXProperties(m_serializer, dataSetEl, xName, m_dataSet.ExtProperties);
        }

        public void ReadSimpleSchema(T element)
        {
            var tableElements = m_serializer.GetElements(element, "Table");
            
            var xPropertiesEl = m_serializer.GetElement(element, "XProperties");

            if (xPropertiesEl != null)
            {
                ReadDataSetExtProperties(xPropertiesEl);
            }

            foreach (var tableEl in tableElements)
            {
                var tableName = m_serializer.GetAttributeValue(tableEl, "TableName");

                if (string.IsNullOrEmpty(tableName))
                {
                    continue;
                }

                if (m_dataSet.HasTable(tableName))
                {
                    continue;
                }

                m_dataSet.AddTable(tableName);
            }
            
            var relEl = m_serializer.GetElement(element, "Relations");

            var relItems = m_serializer.GetElements(relEl, "Item");
            

            foreach (var relX in relItems)
            {
                var relation = m_relationSerializer.ParseRelation(relX, tableName => m_dataSet.GetTable(tableName), false);

                if (relation == null)
                {
                    continue;
                }

                var pl = relation.ParentTable.BeginLoadCore();
                var cl = relation.ChildTable.BeginLoadCore();

                m_dataSet.AddRelationCore(relation);
                
                pl.EndLoad();
                cl.EndLoad();
            }
        }
        
        private void ReadDataSetExtProperties(T extPropertiesEl)
        {
            foreach (var xPropEl in m_serializer.GetElements(extPropertiesEl))
            {
                var xPropName = m_serializer.GetElementName(xPropEl);
                var xPropValue = m_serializer.GetElementValue(xPropEl);
                
                var xPropTableValue = Serializer.DeserializeXPropValue(m_serializer, m_dataSet.TableName, m_dataSet.Namespace, xPropEl, xPropValue, xPropName);
                
                m_dataSet.SetXProperty(xPropName, xPropTableValue);
            }
        }
    }
}