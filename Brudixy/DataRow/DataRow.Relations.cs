using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class DataRow
    {
        [NotNull]
        public IEnumerable<DataRow> GetChildRows(string relationName)
        {
            var rowState = RowRecordState;
            
            if (table != null && rowState != RowState.Detached)
            {
                var childRelation = table.GetChildRelation(relationName);
                
                return base.GetChildRows<DataRow>(childRelation, table, rowState != RowState.Deleted);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        [NotNull]
        protected IEnumerable<T> GetChildRowsCore<T>(string relationName) where T: ICoreDataRowReadOnlyAccessor
        {
            var rowState = RowRecordState;
            
            if (table != null && rowState != RowState.Detached)
            {
                var childRelation = table.GetChildRelation(relationName);
                
                return base.GetChildRows<T>(childRelation, table, rowState != RowState.Deleted);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public new IEnumerable<DataRow> GetChildRows(IDataRelation relation)
        {
            return GetChildRowsCore(relation);
        }

        internal IEnumerable<DataRow> GetChildRowsCore(IDataRelation relation)
        {
            var rowState = RowRecordState;

            if (table != null && rowState != RowState.Detached)
            {
                return GetChildRows<DataRow>(relation, table, rowState != RowState.Deleted);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
    
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetParentRows(string keyFieldName, string parentKeyFieldName)
        {
            return GetParentRows(keyFieldName, parentKeyFieldName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.GetParentRow(IDataRelation relation)
        {
            return GetParentRow((DataRelation)relation);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.GetParentRow(string relationName)
        {
            return GetParentRow(relationName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableReadOnlyRow IDataTableReadOnlyRow.GetParentRow(IDataRelation relation)
        {
            return GetParentRow((DataRelation)relation);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableReadOnlyRow IDataTableReadOnlyRow.GetParentRow(string relationName)
        {
            return GetParentRow(relationName);
        }

        [CanBeNull]
        public new DataRow GetParentRow(string relationName)
        {
            return (DataRow)base.GetParentRow(relationName);
        }

        [CanBeNull]
        public new IDataTableRow GetParentRow(IDataRelation relation)
        {
            return (DataRow)base.GetParentRow(relation);
        }

        [CanBeNull]
        public new DataRow GetParentRow(DataRelation relation)
        {
            return (DataRow)base.GetParentRow(relation);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetChildRows(IDataTableReadOnlyColumn keyFieldName, IDataTableReadOnlyColumn parentKeyFieldName)
        {
            return GetChildRows(keyFieldName, parentKeyFieldName);
        }

        public IEnumerable<IDataTableRow> GetChildRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField)
        {
            if (keyField is DataColumn keyCol && parentField is DataColumn parentCol)
            {
                return GetChildRows(keyCol, parentCol);
            }

            return base.GetChildRows<DataRow>(keyField.ColumnName, parentField.ColumnName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetChildRows(IDataRelation relation)
        {
            return GetChildRows((DataRelation)relation);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetParentRows(IDataRelation relation)
        {
            return GetParentRows((DataRelation)relation);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetParentRows(IDataRelation relation)
        {
            return GetParentRows((DataRelation)relation);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetParentRows(string relationName)
        {
            return GetParentRows(relationName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetParentRows(string keyFieldName, string parentKeyFieldName)
        {
            return GetParentRows(keyFieldName, parentKeyFieldName);
        }

        [NotNull]
        public new IEnumerable<DataRow> GetParentRows(string relationName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var parentRelation = table.GetParentRelation(relationName);
                
                var parentRowHandles = GetParentRowHandles(parentRelation, table);

                foreach (var rowHandle in parentRowHandles)
                {
                    yield return (DataRow)parentRelation.ParentTable.GetRow(rowHandle);
                }
                
                parentRowHandles.Dispose();
                
                yield break;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public new IEnumerable<DataRow> GetParentRows(DataRelation relation)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return base.GetParentRows(relation, table).OfType<DataRow>();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        [NotNull]
        public IEnumerable<IDataTableRow> GetParentRows(IDataRelation relation)
        {
            if (table != null && relation is DataRelation dr && ReferenceEquals(this.table, dr.ChildTable))
            {
                if (RowRecordState != RowState.Detached)
                {
                    var parentRowHandles = GetParentRowHandles(dr, table);

                    var dataRows = new Data<IDataTableRow>();

                    foreach (var ph in parentRowHandles)
                    {
                        dataRows.Add((IDataTableRow)relation.ParentTable.GetRow(ph));
                    }
            
                    parentRowHandles.Dispose();

                    return dataRows;
                }
            }

            return GetParentRows(relation.Name);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetChildRows(string parentKey, string childKey)
        {
            return GetChildRows(parentKey, childKey);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetChildRows(IDataRelation relation)
        {
            return GetChildRows((DataRelation)relation);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetChildRows(string parentKey, string childKey)
        {
            return GetChildRows(parentKey, childKey);
        }

        public IEnumerable<DataRow> GetChildRows(string parentKey, string childKey)
        {
            if (table is not null && RowRecordState != RowState.Detached)
            {
                return table.GetChildren<DataRow>(childKey, (IComparable)this[parentKey]);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<DataRow> GetChildRows(DataColumn idColumn, DataColumn parentIdColumn)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.GetChildren<DataRow>(parentIdColumn, (IComparable)this[idColumn]);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<DataRow> GetParentRows(string parentKey, string childKey)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.GetParentRows<DataRow>(parentKey, (IComparable)this[childKey]);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<DataRow> GetParentRows(DataColumn parentKey, DataColumn childKey)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.GetParentRows<DataRow>(parentKey, (IComparable)this[childKey]);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<DataRow> GetAllParentRows(string keyField, string parentField, bool addCurrent = false) 
        {
            return base.GetAllParentRows<DataRow>(keyField, parentField, addCurrent);
        }

        public IEnumerable<DataRow> GetAllParentRows(DataColumn keyField, DataColumn parentField, bool addCurrent = false)
        {
            return base.GetAllParentRows<DataRow>(keyField, parentField, addCurrent);
        }

        public IEnumerable<DataRow> GetAllChildRows(string keyField, string parentField, bool addCurrent = false)
        {
            var keyColumn = TryGetColumnCore(keyField) as DataColumn;
            var parentColumn = TryGetColumnCore(parentField) as DataColumn;

            if (keyColumn == null || parentColumn == null)
            {
                return Array.Empty<DataRow>();
            }

            return base.GetAllChildRows<DataRow>(keyColumn, parentColumn, addCurrent);
        }

        public IEnumerable<DataRow> GetAllChildRows(DataColumn keyColumn, DataColumn parentColumn, bool addCurrent = false)
        {
            return base.GetAllChildRows<DataRow>(keyColumn, parentColumn, addCurrent);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetAllParentRows(string keyField, string parentField, bool addCurrent = true)
        {
            return GetAllParentRows(keyField, parentField, addCurrent);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetAllChildRows(string keyField, string parentField, bool addCurrent = true)
        {
            return GetAllChildRows(keyField, parentField, addCurrent);
        }

        IEnumerable<IDataTableRow> IDataTableRow.GetAllParentRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent = true)
        {
            if (keyField is DataColumn keyCol 
                && parentField is DataColumn parentCol 
                && ReferenceEquals(keyCol.DataTable, this.table) 
                && ReferenceEquals(parentCol.DataTable, this.table))
            {
                return GetAllParentRows(keyCol, parentCol, addCurrent);
            }

            return GetAllParentRows(keyField.ColumnName, parentField.ColumnName, addCurrent);
        }

        IEnumerable<IDataTableRow> IDataTableRow.GetAllChildRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent = true)
        {
            if (keyField is DataColumn keyCol 
                && parentField is DataColumn parentColumn 
                && ReferenceEquals(keyCol.DataTable, this.table) 
                && ReferenceEquals(parentColumn.DataTable, this.table))
            {
                return GetAllChildRows(keyCol, parentColumn, addCurrent);
            }

            return GetAllChildRows(keyField.ColumnName,  parentField.ColumnName, addCurrent);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetAllParentRows(string keyField, string parentField, bool addCurrent)
        {
            return GetAllParentRows(keyField, parentField, addCurrent);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetAllChildRows(string keyField, string parentField, bool addCurrent)
        {
            return GetAllChildRows(keyField, parentField, addCurrent);
        }

        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetAllParentRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent)
        {
            if (keyField is DataColumn keyCol 
                && parentField is DataColumn parentCol     
                && ReferenceEquals(keyCol.DataTable, this.table) 
                && ReferenceEquals(parentCol.DataTable, this.table))
            {
                return GetAllParentRows(keyCol, parentCol, addCurrent);
            }

            return GetAllParentRows(keyField.ColumnName, parentField.ColumnName, addCurrent);
        }

        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetAllChildRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent)
        {
            if (keyField is DataColumn keyCol 
                && parentField is DataColumn parentColumn     
                && ReferenceEquals(keyCol.DataTable, this.table) 
                && ReferenceEquals(parentColumn.DataTable, this.table))
            {
                return GetAllChildRows(keyCol, parentColumn, addCurrent);
            }

            return GetAllChildRows(keyField.ColumnName, parentField.ColumnName, addCurrent);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetChildRows(string relationName)
        {
            return GetChildRows(relationName);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTableRow.GetParentRows(string relationName)
        {
            return GetParentRows(relationName);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyRow> IDataTableReadOnlyRow.GetChildRows(string relationName)
        {
            return GetChildRows(relationName);
        }
    }
}