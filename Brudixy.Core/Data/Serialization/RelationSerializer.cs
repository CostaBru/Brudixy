using System;
using System.Collections.Generic;
using System.Xml;
using Brudixy.Constraints;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal class RelationSerializer<T, V>
    {
        private readonly SerializerAdapter<T, V> m_serializer;
        public RelationSerializer(SerializerAdapter<T, V> serializer)
        {
            m_serializer = serializer;
        }
        
        public void WriteRelations(IEnumerable<DataRelation> relations, T relationsXElement)
        {
            foreach (var relation in relations)
            {
                var relEl = m_serializer.CreateElement("Item");

                m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Name", XmlConvert.EncodeLocalName(relation.RelationName)));
                m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Parent", XmlConvert.EncodeLocalName(relation.ParentTableName)));
                m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Child", XmlConvert.EncodeLocalName(relation.ChildTableName)));
                m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Type", relation.Type.ToString()));

                var parentColumnsX = m_serializer.CreateElement("ParentColumns");
                foreach (var parentColumn in relation.ParentColumnNames)
                {
                    m_serializer.AppendElement(parentColumnsX, m_serializer.CreateElement("Item", XmlConvert.EncodeLocalName(parentColumn)));
                }

                m_serializer.AppendElement(relEl, parentColumnsX);

                var childColumnX = m_serializer.CreateElement("ChildColumns");
                foreach (var childColumn in relation.ChildColumnNames)
                {
                    m_serializer.AppendElement(childColumnX, m_serializer.CreateElement("Item", XmlConvert.EncodeLocalName(childColumn)));
                }
                
                m_serializer.AppendElement(relEl, childColumnX);

                if (relation.ChildKeyConstraint != null)
                {
                    WriteForeignKey(relation.ChildKeyConstraint, relEl);
                }

                m_serializer.AppendElement(relationsXElement, relEl);
            }
        }
        
        public void WriteForeignKey(ForeignKeyConstraint constraint, T relationXElement)
        {
            var relEl = m_serializer.CreateElement("ChildConstraint");

            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Name", XmlConvert.EncodeLocalName(constraint.ConstraintName)));
            
            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("DeleteRule", XmlConvert.EncodeLocalName(constraint.DeleteRule.ToString())));
            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("UpdateRule", XmlConvert.EncodeLocalName(constraint.UpdateRule.ToString())));
            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("AcceptRule", XmlConvert.EncodeLocalName(constraint.AcceptRejectRule.ToString())));
            
            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Child", XmlConvert.EncodeLocalName(constraint.ChildTable?.Name ?? string.Empty)));
            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Parent", XmlConvert.EncodeLocalName(constraint.RelatedTable?.Name ?? string.Empty)));
            
            TableStorageType? storageType = null;

            var parentColumnsX = m_serializer.CreateElement("ParentColumns");
            foreach (var parentColumn in constraint.ParentKey.ColumnsReference)
            {
                m_serializer.AppendElement(parentColumnsX, m_serializer.CreateElement("Item", XmlConvert.EncodeLocalName(parentColumn.ColumnName)));

                if (storageType == null)
                {
                    storageType = parentColumn.Type;
                }

                if (storageType != parentColumn.Type)
                {
                    storageType = TableStorageType.Object;
                }
            }

            m_serializer.AppendElement(relEl, parentColumnsX);
            
            var childColumnX = m_serializer.CreateElement("ChildColumns");
            foreach (var childColumn in constraint.m_childKey.ColumnsReference)
            {
                m_serializer.AppendElement(childColumnX, m_serializer.CreateElement("Item", XmlConvert.EncodeLocalName(childColumn.ColumnName)));
                
                if (storageType == null)
                {
                    storageType = childColumn.Type;
                }
                else if (storageType != childColumn.Type)
                {
                    storageType = TableStorageType.Object;
                }
            }

            m_serializer.AppendElement(relEl, childColumnX);
            
            m_serializer.AppendAttribute(relEl, m_serializer.CreateAttribute("Type", XmlConvert.EncodeLocalName(storageType.ToString())));

            m_serializer.AppendElement(relationXElement, relEl);
        }
        
        [CanBeNull]
        public DataRelation ParseRelation(T relation, Func<string, CoreDataTable> getTable, bool nested)
        {
            return ParseDataRelationCore(relation, getTable, nested, false);
        }

        private DataRelation ParseDataRelationCore(T relation, Func<string, CoreDataTable> getTable, bool nested, bool createColumnsIfAbsent)
        {
            var relationName = m_serializer.GetAttributeValue(relation, "Name");
            var parentTableName = m_serializer.GetAttributeValue(relation,"Parent");
            var childTableName = m_serializer.GetAttributeValue(relation,"Child");
            var relationType = m_serializer.GetAttributeValue(relation,"Type");

            var parentTable = getTable(parentTableName);
            var childTable = getTable(childTableName);

            if (parentTable == null || childTable == null)
            {
                return null;
            }

            var pl = parentTable.BeginLoadCore();
            var cl = childTable.BeginLoadCore();

            var parentColumns = new Data<CoreDataColumn>();

            foreach (var colName in m_serializer.GetElements(m_serializer.GetElement(relation, "ParentColumns")))
            {
                var elementValue = m_serializer.GetElementValue(colName);

                if (createColumnsIfAbsent)
                {
                    if (parentTable.ContainsColumn(elementValue) == false)
                    {
                        parentTable.AddColumn(elementValue, TableStorageType.String);
                    }
                }

                var column = parentTable.GetColumn(elementValue);

                parentColumns.Add(column);
            }

            var childColumns = new Data<CoreDataColumn>();

            foreach (var colName in m_serializer.GetElements(m_serializer.GetElement(relation, "ChildColumns")))
            {
                var elementValue = m_serializer.GetElementValue(colName);

                if (createColumnsIfAbsent)
                {
                    if (childTable.ContainsColumn(elementValue) == false)
                    {
                        childTable.AddColumn(elementValue, TableStorageType.String);
                    }
                }

                var column = childTable.GetColumn(elementValue);

                childColumns.Add(column);
            }

            var dataRelation = new DataRelation(relationName, parentColumns, childColumns)
            {
                m_type = (RelationType)Enum.Parse(typeof(RelationType), relationType)
            };

            parentColumns.Dispose();
            childColumns.Dispose();
            
            if (nested)
            {
                dataRelation.Nested = true;
            }

            var constraint = m_serializer.GetElement(relation, "ChildConstraint");

            if (constraint != null)
            {
                var foreignKeyConstraint = ParseForeignKeyConstraintCore(constraint, getTable, createColumnsIfAbsent);

                dataRelation.ChildKeyConstraint = foreignKeyConstraint;
            }
            
            pl.EndLoad();
            cl.EndLoad();

            return dataRelation;
        }
        
        private ForeignKeyConstraint ParseForeignKeyConstraintCore(T constraintEl, Func<string, CoreDataTable> getTable, bool createColumnsIfAbsent)
        {
            var constraintName = m_serializer.GetAttributeValue(constraintEl, "Name");
            var updateRule = m_serializer.GetAttributeValue(constraintEl, "UpdateRule");
            var deleteRule = m_serializer.GetAttributeValue(constraintEl, "DeleteRule");
            var acceptRule = m_serializer.GetAttributeValue(constraintEl, "AcceptRule");
            var parentTableName = m_serializer.GetAttributeValue(constraintEl, "Parent");
            var childTableName = m_serializer.GetAttributeValue(constraintEl, "Child");
            var type = m_serializer.GetAttributeValue(constraintEl,"Type");

            var parentTable = getTable(parentTableName);
            var childTable = getTable(childTableName);

            if (parentTable == null || childTable == null)
            {
                return null;
            }

            var parentColumns = new Data<CoreDataColumn>();

            foreach (var colName in m_serializer.GetElements(m_serializer.GetElement(constraintEl,"ParentColumns")))
            {
                var elementValue = m_serializer.GetElementValue(colName);
                
                if (createColumnsIfAbsent)
                {
                    if (parentTable.ContainsColumn(elementValue) == false)
                    {
                        parentTable.AddColumn(elementValue, TableStorageType.String);
                    }
                }

                var column = parentTable.GetColumn(elementValue);

                parentColumns.Add(column);
            }

            var childColumns = new Data<CoreDataColumn>();

            foreach (var colName in m_serializer.GetElements(m_serializer.GetElement(constraintEl, "ChildColumns")))
            {
                var elementValue = m_serializer.GetElementValue(colName);

                if (createColumnsIfAbsent)
                {
                    if (childTable.ContainsColumn(elementValue) == false)
                    {
                        childTable.AddColumn(elementValue, TableStorageType.String);
                    }
                }

                var column = childTable.GetColumn(elementValue);

                childColumns.Add(column);
            }

            TableStorageType storageType = TableStorageType.Object;

            if (string.IsNullOrEmpty(type) == false)
            {
                if (Enum.TryParse(type, false, out TableStorageType rule))
                {
                    storageType = rule;
                } 
            }

            var foreignKeyConstraint = CoreDataTable.CreateForeignKeyConstraint(storageType);
            
            foreignKeyConstraint.Create(constraintName, parentTable, childTable, parentColumns, childColumns);
            
            if (string.IsNullOrEmpty(updateRule) == false)
            {
                if (Enum.TryParse(updateRule, false, out Rule rule))
                {
                    foreignKeyConstraint.UpdateRule = rule;
                }
            }
            
            if (string.IsNullOrEmpty(deleteRule) == false)
            {
                if (Enum.TryParse(deleteRule, false, out Rule rule))
                {
                    foreignKeyConstraint.DeleteRule = rule;
                }
            }
            
            if (string.IsNullOrEmpty(acceptRule) == false)
            {
                if (Enum.TryParse(acceptRule, false, out AcceptRejectRule rule))
                {
                    foreignKeyConstraint.AcceptRejectRule = rule;
                }
            }

            parentColumns.Dispose();
            childColumns.Dispose();
            
            return foreignKeyConstraint;
        }
    }
}