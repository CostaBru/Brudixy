using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    partial class DataTable : IExpressionDataSource
    {
        IEnumerable<string> IExpressionDataSource.GetColumns()
        {
            return GetColumns().Select(c => c.ColumnName);
        }
        
        internal class DataExpressionCache : IDisposable
        {
            private struct ExprRowColTKey
            {
                public ExprRowColTKey(int row, int col)
                {
                    Row = row;
                    Column = col;
                }

                public int Row;
                public int Column;
            }

            private struct ColAge
            {
                public int ColumnHandle;

                public ulong Age;
            }

            private Map<ExprRowColTKey, Tuple<ulong, Data<ColAge>>> m_ages;
          
            private Map<ExprRowColTKey, object> m_expressionValue; 

            private readonly DataTable m_dataTable;

            public DataExpressionCache(DataTable dataTable)
            {
                m_dataTable = dataTable;
            }

            public void MergeFrom(DataExpressionCache source)
            {
                if (source.m_ages != null)
                {
                    m_ages = new Map<ExprRowColTKey, Tuple<ulong, Data<ColAge>>>(source.m_ages);
                }

                if (source.m_expressionValue != null)
                {
                    m_expressionValue = new Map<ExprRowColTKey, object>(source.m_expressionValue);
                }
            }

            public void Dispose()
            {
                if (m_ages != null)
                {
                    foreach (var tuple in m_ages)
                    {
                        tuple.Value.Item2?.Clear();
                    }

                    m_ages.Clear();

                    m_ages = null;
                }

                if (m_expressionValue != null)
                {
                    m_expressionValue.Clear();

                    m_expressionValue = null;
                }
            }


            public object GetExpressionValue(int columnHandle, int rowHandle)
            {
                var dataColumn = m_dataTable.GetColumn(columnHandle);
                
                var expression = dataColumn.ExpressionLink;

                if (expression != null && expression.IsTableAggregate() == false && expression.HasLocalAggregate() == false)
                {
                    return GetExpressionValue(columnHandle, rowHandle, expression);
                }

                var dataItem = dataColumn.DataStorageLink;

                if (dataItem.IsNull(rowHandle, dataColumn))
                {
                    return null;
                }

                return dataItem.GetData(rowHandle, dataColumn);
            }

            private object GetExpressionValue(int columnHandle, int rowHandle, DataExpression expression)
            {
                m_ages ??= new Map<ExprRowColTKey, Tuple<ulong, Data<ColAge>>>();
                m_expressionValue ??= new Map<ExprRowColTKey, object>();

                var exprRowColTKey = new ExprRowColTKey(rowHandle, columnHandle);

                var tableAge = m_dataTable.m_dataAge;

                bool evaluate = true;

                Data<ColAge> currentColumnAges = null;

                if (m_ages.TryGetValue(exprRowColTKey, out var savedTableARowAge))
                {
                    var savedTableAge = savedTableARowAge.Item1;
                    var savedColumnAges = currentColumnAges = savedTableARowAge.Item2;

                    if (savedTableAge == tableAge)
                    {
                        var anyColumnAgeWasChanged = CheckDependentColumnChanged(exprRowColTKey, savedColumnAges);

                        if (anyColumnAgeWasChanged == false)
                        {
                            evaluate = false;
                        }
                    }
                }
                else
                {
                    currentColumnAges = new Data<ColAge>();

                    CheckDependentColumnChanged(exprRowColTKey, currentColumnAges);
                }

                if (evaluate || m_expressionValue.ContainsKey(exprRowColTKey) == false)
                {
                    return EvaluateNew(rowHandle, expression, exprRowColTKey, tableAge, currentColumnAges);
                }

                return m_expressionValue[exprRowColTKey];
            }

            private bool CheckDependentColumnChanged(ExprRowColTKey rowColTKey,[CanBeNull] Data<ColAge> savedAges)
            {
                bool anyColumnAgeWasChanged = false;

                var needCheck = savedAges?.Count > 0;

                Data<string> columns = null;
                
                if (savedAges != null && (m_dataTable.DataColumnInfo.ExpressionDependentColumn?.TryGetValue(rowColTKey.Column, out columns) ?? false))
                {
                    foreach (var dependentColumnName in columns)
                    {
                        if (m_dataTable.DataColumnInfo.ColumnMappings.TryGetValue(dependentColumnName, out var dependentColumn) == false)
                        {
                            continue;
                        } 
                        
                        var dependentColumnAge = dependentColumn.DataStorageLink.GetAge(rowColTKey.Row, dependentColumn);

                        var savedAge = new ColAge { Age = dependentColumnAge, ColumnHandle = dependentColumn.ColumnHandle };

                        if (needCheck)
                        {
                            var index = savedAges.FindIndex(colAge => colAge.ColumnHandle == dependentColumn.ColumnHandle);

                            if (index >= 0)
                            {
                                ColAge savedColAge = savedAges[index];

                                if (dependentColumnAge > savedColAge.Age)
                                {
                                    anyColumnAgeWasChanged = true;
                                }

                                savedAges[index] = savedAge;
                            }
                            else
                            {
                                savedAges.Add(savedAge);
                            }
                        }
                        else
                        {
                            savedAges.Add(savedAge);
                        }
                    }
                }
                return anyColumnAgeWasChanged;
            }

            private object EvaluateNew(int rowHandle, DataExpression expression, ExprRowColTKey exprRowColTKey, ulong tableAge, Data<ColAge> rowsAge)
            {
                var value = expression.Evaluate(rowHandle);

                m_expressionValue[exprRowColTKey] = value;

                m_ages[exprRowColTKey] = new Tuple<ulong, Data<ColAge>>(tableAge, rowsAge);

                return value;
            }
        }

        internal void EvaluateExpressions()
        {
            var columns = GetDependentColumns();

            if (columns.Any())
            {
                foreach (var rowHandles in RowsHandles)
                {
                    EvaluateDependentExpressions(columns, rowHandles);
                }
            }
        }

        private Set<int> GetDependentColumns()
        {
            var columns = new Set<int>();

            foreach (var dataColumn in GetColumns())
            {
                var dataExpression = dataColumn.Expression;
                if (dataExpression != null)
                {
                    columns.Add(dataColumn.ColumnHandle);
                }
            }

            var dependentColumns = DataColumnInfo.ExpressionDependentColumn;

            if (dependentColumns != null)
            {
                foreach (var kv in dependentColumns)
                {
                    columns.Add(kv.Key);
                
                    foreach (var col in kv.Value)
                    {
                        if (ColumnMapping.TryGetValue(col, out var dependantColumn))
                        {
                            columns.Add(dependantColumn.ColumnHandle);
                        }
                        else
                        {
                            var expressionColumn = DataColumnInfo.Columns[kv.Key].ColumnName;

                            ThrowMissingColumnInExpression(expressionColumn, col);
                        }
                    }
                }
            }
            
            return columns;
        }

        private void ThrowMissingColumnInExpression(string expressionColumn, string col)
        {
            throw new MissingMetadataException(
                $"Cannot evaluate expression for '{expressionColumn}' column because of missing column '{col}' parsed expression in '{Name}' table.");
        }

        internal void EvaluateExpressions(int row, List<DataRow> cachedRows = null)
        {
            var columns = GetDependentColumns();

            EvaluateDependentExpressions(columns, row, cachedRows);
        }

        internal void EvaluateExpressions(ColumnHandle columnHandle)
        {
            var dataExpression = GetColumn(columnHandle.Handle).ExpressionLink;

            if (dataExpression != null)
            {
                int count = RowCount;

                if (dataExpression.IsTableAggregate() && count > 0)
                {
                    object obj = dataExpression.Evaluate();

                    SilentlySetValue(columnHandle, obj);
                }

                EvaluateDependentExpressions(columnHandle);
            }
        }

        internal void EvaluateDependentExpressions(ColumnHandle columnHandle)
        {
            Data<string> dependentOn = null;
            if (DataColumnInfo.ExpressionDependentColumn?.TryGetValue(columnHandle.Handle, out dependentOn) ?? false)
            {
                foreach (var dependentColumn in dependentOn)
                {
                    var dataColumn = ColumnMapping[dependentColumn];

                    if (columnHandle.Handle != dataColumn.ColumnHandle)
                    {
                        EvaluateExpressions(dataColumn.ColumnHandle);
                    }
                }
            }
        }

        internal void EvaluateDependentExpressions(Set<int> columns, int rowHandle, List<DataRow> cachedRows = null)
        {
            foreach (var columnHandle in columns)
            {
                var dataColumn = GetColumn(columnHandle);
                
                var dataExpression = dataColumn.ExpressionLink;

                if (dataExpression == null)
                {
                    continue;
                }

                if (dataExpression.HasLocalAggregate())
                {
                    bool isTableAggregate = dataExpression.IsTableAggregate();

                    if (isTableAggregate)
                    {
                        var newValue = dataExpression.Evaluate(rowHandle);

                        SilentlySetValue(new ColumnHandle(columnHandle), newValue);
                    }
                }

                if (dataExpression.HasLocalAggregate() == m_nestedInDataset)
                {
                    if (cachedRows != null)
                    {
                        foreach (DataRow dataRow in cachedRows)
                        {
                            object newValue = dataExpression.Evaluate(dataRow.RowHandleCore);

                            var dataItem = dataColumn.DataStorageLink;

                            dataItem.SilentlySetValue(rowHandle, newValue, dataColumn);
                        }
                    }

                    if (ParentRelationsMap != null)
                    {
                        foreach (var parentRelation in ParentRelationsMap)
                        {
                            var relation = parentRelation.Value;
                            var relationName = parentRelation.Key;

                            var parentTable = relation.ParentTable;

                            if (parentTable?.Name == Name)
                            {
                                var dataRow = GetRowInstance(rowHandle);

                                foreach (var parentRow in dataRow.GetParentRows(relationName))
                                {
                                    if (cachedRows == null || cachedRows.Contains(parentRow) == false)
                                    {
                                        object newValue = dataExpression.Evaluate(parentRow.RowHandleCore);

                                        var dataItem = dataColumn.DataStorageLink;

                                        dataItem.SilentlySetValue(rowHandle, newValue, dataColumn);
                                    }
                                }
                            }
                        }
                    }

                    if (ChildRelationsMap != null)
                    {
                        foreach (var childRelation in ChildRelationsMap)
                        {
                            var relation = childRelation.Value;
                            var relationName = childRelation.Key;

                            var childTable = relation.ChildTable;

                            if (childTable?.Name == Name)
                            {
                                var dataRow = GetRowInstance(rowHandle);

                                foreach (DataRow childRow in dataRow.GetChildRows<DataRow>(relationName))
                                {
                                    if (cachedRows == null || !cachedRows.Contains(childRow))
                                    {
                                        object newValue = dataExpression.Evaluate(childRow.RowHandleCore);

                                        var dataItem = dataColumn.DataStorageLink;

                                        dataItem.SilentlySetValue(rowHandle, newValue, dataColumn);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        int IExpressionDataSource.GetColumnHandle(string column)
        {
            return DataColumnInfo.ColumnMappings[column].ColumnHandle;
        }

        IEnumerable<int> IExpressionDataSource.GetRowHandles()
        {
            return RowsHandles;
        }

        string IExpressionDataSource.Name => TableName;

        bool IExpressionDataSource.IsLoading => GetIsInTransaction();

        TableStorageType? IExpressionDataSource.GetColumnType(string column)
        {
            return DataColumnInfo.ColumnMappings[column].Type;
        }

        IDataRowReadOnlyAccessor IExpressionDataSource.GetRowByHandle(int rowHandle)
        {
            return this.GetRowByHandle(rowHandle);
        }
        
        bool IExpressionDataSource.ExpressionDependsOn(int columnHandle, string column)
        {
            var dataExpression = GetColumn(columnHandle).ExpressionLink;

            if (dataExpression != null)
            {
                return dataExpression.DependsOn(column);
            }

            return false;
        }

        object IExpressionDataSource.GetRowColumnValueByHandle(int rowHandle, string column)
        {
            var row = GetRowByHandle(rowHandle);

            if (row != null)
            {
                var value = row[column];

                if (value == null)
                {
                    var columnVal = row.TryGetColumn(column);

                    if (columnVal != null)
                    {
                        return CoreDataTable.GetDefaultNotNull(columnVal.Type, columnVal.TypeModifier);
                    }
                }

                return value;
            }

            return null;
        }

        public object GetRowXPropertyByHandle(int rowHandle, string xProperty)
        {
            return GetRowByHandle(rowHandle)?.GetXProperty<object>(xProperty);
        }

        public object GetColumnXProperty(string column, string xProperty)
        {
            return TryGetColumn(column)?.GetXProperty<object>(xProperty);
        }

        public virtual IFunctionRegistry GetFunctionRegistry()
        {
            return FunctionRegistry.Registry;
        }
    }
}
