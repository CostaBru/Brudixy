using System.Diagnostics;
using Brudixy.Interfaces;

namespace Brudixy
{
    public sealed class DataRowDebugView
    {
        private readonly IDataRowReadOnlyAccessor m_row;

        public DataRowDebugView(DataRow dataRow)
        {
            if (dataRow == null)
            {
                throw new ArgumentNullException("dataRow");
            }

            m_row = dataRow;
        }

        public DataRowDebugView(DataRowContainer dataRow)
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
                var items = new List<KeyValuePair<string, object>>();
            
                if (m_row is DataRowContainer rowContainer)
                {
                    if (rowContainer.Editing != null)
                    {
                        items.Add(new KeyValuePair<string, object>("Editing", true));
                    }
                    
                    var keyValuePairs = new CoreDataRowDebugView(rowContainer).Items;
                    
                    items.AddRange(keyValuePairs);
                    
                    items.Add(new KeyValuePair<string, object>("Age", rowContainer.GetRowAge().ToString()));

                    if (rowContainer.m_dataFieldChangedEvent != null && rowContainer.m_dataFieldChangedEvent.HasAny())
                    {
                        items.Add(new KeyValuePair<string, object>("Changed subscription", true.ToString()));
                    }

                    if (rowContainer.m_dataFieldChangingEvent != null && rowContainer.m_dataFieldChangingEvent.HasAny())
                    {
                        items.Add(new KeyValuePair<string, object>("Changing subscription", true.ToString()));
                    }

                    AddInfo(rowContainer, items);
                }

                if (m_row is DataRow row)
                {
                    if (row.DataRowRecordIsInEdit())
                    {
                        items.Add(new KeyValuePair<string, object>("Editing", true));
                    }
                    
                    var keyValuePairs = new CoreDataRowDebugView(row).Items;
                    
                    items.AddRange(keyValuePairs);
                    
                    if (string.IsNullOrEmpty(row.GetRowFault()) == false)
                    {
                        items.Add(new KeyValuePair<string, object>("Row fault", row.GetRowFault()));
                    }
                    
                    AddInfo(row, items);
                
                    var td = row.table;

                    if (td == null)
                    {
                        items.Add(new KeyValuePair<string, object>("Table is null", true.ToString()));
                    }
                    else
                    {
                        if (td.HasDataColumnChangedHandler)
                        {
                            items.Add(new KeyValuePair<string, object>("Changed subscription", true.ToString()));
                        }

                        if (td.HasDataColumnChangingHandler)
                        {
                            items.Add(new KeyValuePair<string, object>("Changing subscription", true.ToString()));
                        }

                        if (td.HasDataRowChangedHandler)
                        {
                            items.Add(new KeyValuePair<string, object>("RowChanged subscription", true.ToString()));
                        }

                        var changedHandlersColumns = new List<string>();
                        var changingHandlersColumns = new List<string>();

                        for (int i = 0; i < row.GetColumnCount(); i++)
                        {
                            if (td.HasCustomDataColumnChangedHandler(i))
                            {
                                changedHandlersColumns.Add(row.table.DataColumnInfo.Columns[i].ColumnName);
                            }

                            if (td.HasCustomDataColumnColumnChangingHandler(i))
                            {
                                changingHandlersColumns.Add(row.table.DataColumnInfo.Columns[i].ColumnName);
                            }
                        }

                        if (changedHandlersColumns.Count > 0)
                        {
                            items.Add(new KeyValuePair<string, object>("Custom column changed handlers",
                                string.Join(";", changedHandlersColumns)));
                        }

                        if (changingHandlersColumns.Count > 0)
                        {
                            items.Add(new KeyValuePair<string, object>("Custom column changing handlers",
                                string.Join(";", changingHandlersColumns)));
                        }
                    }
                }

                return items.ToArray();
            }
        }

        private static void AddInfo(IDataRowReadOnlyAccessor row, List<KeyValuePair<string, object>> items)
        {
            foreach (var gr in row.GetRowAnnotations().GroupBy(a => a.type))
            {
                items.Add(new KeyValuePair<string, object>($"Row {gr.Key}",  string.Join("|", gr)));
            }

            var rowAnnotationGroups = row.GetCellAnnotations().GroupBy(t => t.type);

            foreach (var rowAnnotationGr in rowAnnotationGroups)
            {
                var select = rowAnnotationGr.Select(a => (a.column, a.type, a.value));

                items.Add(new KeyValuePair<string, object>($"Column {rowAnnotationGr.Key}",
                    string.Join("|",
                        select.Select(errCol => $"{errCol.column}:{errCol.type} '{errCol.value}'"))));
            }
            
            if (row.XPropertyAnnotations.Any())
            {
                items.Add(new KeyValuePair<string, object>("XInfo",
                    string.Join("|",
                        row.XPropertyAnnotations.Select(x =>
                        {
                            var info = row.GetXPropertyAnnotationValues(x);
                            
                            if (info == null)
                            {
                                return string.Empty;
                            }
                            
                            return string.Join("|",
                                info.Select(errCol => $"{errCol.Key}:{CoreDataTable.ConvertObjectToString(errCol.Value)}'"));
                        }))));
            }
        }
    }
}