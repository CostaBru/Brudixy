using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Brudixy.Interfaces;

namespace Brudixy
{
    internal sealed class CoreDataRowDebugView
    {
        private readonly ICoreDataRowReadOnlyAccessor m_row;

        public CoreDataRowDebugView(CoreDataRow dataRow)
        {
            if (dataRow == null)
            {
                throw new ArgumentNullException("dataRow");
            }

            m_row = dataRow;
        }
        
        public CoreDataRowDebugView(CoreDataRowContainer dataRow)
        {
            if (dataRow == null)
            {
                throw new ArgumentNullException("dataRow");
            }

            m_row = dataRow;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, object>[] Items
        {
            get
            {
                var columns = m_row.GetColumns().ToArray();

                var xProperties = m_row.GetXProperties().ToArray();

                var items = new List<KeyValuePair<string, object>>(columns.Length + xProperties.Length + 3 + 3);

                foreach (var columnContainer in columns)
                {
                    var columnDisplayName = $"{columnContainer.ColumnName} ({columnContainer.Type.ToString()})";

                    object value;

                    try
                    {
                        value = m_row.IsNull(columnContainer.ColumnName) ? null : m_row.ToString(columnContainer);
                    }
                    catch (Exception exception)
                    {
                        value = exception.Message;
                    }

                    items.Add(new KeyValuePair<string, object>(columnDisplayName, value ?? "{NULL}"));
                }

                foreach (var xProperty in xProperties)
                {
                    var value = m_row.GetXProperty<string>(xProperty);

                    items.Add(new KeyValuePair<string, object>("X:" + xProperty, string.IsNullOrEmpty(value) ? "{EMPTY}" : value));
                }

                var pkColumns = m_row.PrimaryKeyColumn.ToArray();

                if (pkColumns.Length > 0)
                {
                    items.Add(new KeyValuePair<string, object>("PK", string.Join(", ", pkColumns.Select(s => s.ColumnName))));
                }

                if (m_row is CoreDataRow coreDataRow)
                {
                    items.Add(new KeyValuePair<string, object>("Row age", coreDataRow.GetRowAge().ToString()));
                    items.Add(new KeyValuePair<string, object>("Column ages", string.Join("|", coreDataRow.GetTableColumnNames().Select(f => new {col = f, age =coreDataRow.GetColumnAge(f)}).Where(ca => ca.age > 0).Select(f => $"{f.col}={f.age}"))));
                    items.Add(new KeyValuePair<string, object>("Changed fields", string.Join(",", coreDataRow.GetChangedFields())));

                    if (coreDataRow.table?.ParentRelations != null)
                    {
                        foreach (var relation in coreDataRow.table.ParentRelations)
                        {
                            var parentRow = coreDataRow.GetParentRows(relation).FirstOrDefault();

                            items.Add(new KeyValuePair<string, object>(
                                $"Parent row of {relation.ParentTable.TableName}",
                                parentRow == null
                                    ? (object)"{NULL}"
                                    : string.Join(";",
                                        CoreDataRow.GetItemKeyStringValuePairArray(parentRow)
                                            .Where(kv => string.IsNullOrEmpty(kv.Value?.ToString()) == false)
                                            .Select(kv => $"{kv.Key}={kv.Value}"))));
                        }
                    }

                    if (coreDataRow.table?.ChildRelations != null)
                    {
                        foreach (var childRelation in coreDataRow.table.ChildRelations)
                        {
                            var list = coreDataRow.GetChildRows<CoreDataRow>(childRelation).Select((r, i) =>
                                    $"[{i.ToString()}] " + string.Join(";",
                                        CoreDataRow.GetItemKeyStringValuePairArray(r)
                                            .Where(kv => string.IsNullOrEmpty(kv.Value?.ToString()) == false)
                                            .Select(kv => $"{kv.Key}={kv.Value}")))
                                .ToList();

                            if (list.Count > 0)
                            {
                                items.Add(new KeyValuePair<string, object>(
                                    $"Child rows of {childRelation.ChildTable.TableName} ({list.Count.ToString()})",
                                    string.Join(Environment.NewLine, list)));
                            }
                        }
                    }
                }

                return items.ToArray();
            }
        }
    }
}