using System;
using System.Linq;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Constraints
{
    public partial class ForeignKeyConstraint
    {
        public static ForeignKeyConstraint FromXml(XElement element, CoreDataTable table)
        {
            var constraintName = element.Attribute("Name")?.Value;

            var childKeyEl = element.Element("ChildKey");

            if (childKeyEl == null)
            {
                throw new InvalidOperationException(
                    $"Nested relation deserialization error: child key wasn't found in '{element}' xml.");
            }

            var parentKeyEl = element.Element("ParentKey");

            if (parentKeyEl == null)
            {
                throw new InvalidOperationException(
                    $"Nested relation deserialization error: parent key wasn't found in '{element}' xml.");
            }

            var childKeyTableName = childKeyEl.Attribute("Table")?.Value;

            if (childKeyTableName != table.Name)
            {
                throw new InvalidOperationException(
                    $"Nested relation deserialization error: child key table name '{childKeyTableName}' should be equal table name '{table.Name}'.");
            }

            var parentKeyTableName = parentKeyEl.Attribute("Table")?.Value;

            if (parentKeyTableName != table.Name)
            {
                throw new InvalidOperationException(
                    $"Nested relation deserialization error: parent key table name '{parentKeyTableName}' should be equal table name '{table.Name}'.");
            }

            var parentColumns = new Data<CoreDataColumn>();

            TableStorageType? allColumnType = null;

            foreach (var colX in parentKeyEl.Elements("Column"))
            {
                var dataColumn = table.TryGetColumn(colX.Value);

                if (dataColumn == null)
                {
                    throw new InvalidOperationException(
                        $"Nested relation deserialization error: parent key column '{colX.Value}' wasn't found in table '{table.Name}'.");
                }

                parentColumns.Add(dataColumn);
            }

            TableStorageType? tableStorageType = parentColumns.First().Type;

            if (parentColumns.Any(c => c.Type != tableStorageType))
            {
                tableStorageType = null;
            }

            var childColumns = new Data<CoreDataColumn>();

            foreach (var colX in childKeyEl.Elements("Column"))
            {
                var dataColumn = table.TryGetColumn(colX.Value);

                if (dataColumn == null)
                {
                    throw new InvalidOperationException(
                        $"Nested relation deserialization error: child key column '{colX.Value}' wasn't found in table '{table.Name}'.");
                }

                childColumns.Add(dataColumn);
            }

            ForeignKeyConstraint constraint;

            if (tableStorageType == null)
            {
                constraint = new ForeignKeyConstraint();
            }
            else
            {
                constraint = CoreDataTable.CreateForeignKeyConstraint(tableStorageType.Value);
            }

            constraint.constraintName = constraintName;
            constraint.m_childKey = new DataKey(table, childColumns.Select(c => c.ColumnHandle).ToArray());
            constraint.m_parentKey = new DataKey(table, parentColumns.Select(c => c.ColumnHandle).ToArray());

            constraint.m_deleteRule =
                (Rule)Enum.Parse(typeof(Rule), element.Attribute("DeleteRule")?.Value ?? Rule.None.ToString());
            constraint.m_updateRule =
                (Rule)Enum.Parse(typeof(Rule), element.Attribute("UpdateRule")?.Value ?? Rule.None.ToString());
            constraint.m_acceptRejectRule = (AcceptRejectRule)Enum.Parse(typeof(AcceptRejectRule),
                element.Attribute("AcceptRejectRule")?.Value ?? AcceptRejectRule.None.ToString());

            return constraint;
        }

        public XElement ToXElement()
        {
            var fkEl = new XElement("FK", new XAttribute("Name", ConstraintName));

            if (string.IsNullOrEmpty(m_schemaName) == false)
            {
                fkEl.Add(new XAttribute("Schema", m_schemaName));
            }

            fkEl.Add(new XAttribute("DeleteRule", m_deleteRule.ToString()));
            fkEl.Add(new XAttribute("UpdateRule", m_updateRule.ToString()));
            fkEl.Add(new XAttribute("AcceptRejectRule", m_acceptRejectRule.ToString()));

            var parentTable = m_parentKey.Table;
            
            if (parentTable != null)
            {
                var parentKeyElement = new XElement("ParentKey", new XAttribute("Table", parentTable.Name));

                foreach (var column in m_parentKey.ColumnsReference)
                {
                    parentKeyElement.Add(new XElement("Column", column.ColumnName));
                }

                fkEl.Add(parentKeyElement);
            }

            var childTable = m_childKey.Table;
            
            if (childTable != null)
            {
                var childKeyElement = new XElement("ChildKey", new XAttribute("Table", childTable.Name));

                foreach (var column in m_childKey.ColumnsReference)
                {
                    childKeyElement.Add(new XElement("Column", column.ColumnName));
                }

                fkEl.Add(childKeyElement);
            }

            return fkEl;
        }
    }
}