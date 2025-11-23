using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class DataColumnInfo : CoreDataColumnInfo
    {
        [CanBeNull] internal Map<string, Set<int>> ColumnContainsInExpressionMap;

        [CanBeNull] internal Map<int, Data<string>> ExpressionDependentColumn;
        
        public DataColumnObj GetColumnContainer(int columnHandle) => (DataColumnObj)this.Columns[columnHandle].ColumnObj;
        public DataColumn GetColumn(int columnHandle) => (DataColumn)this.Columns[columnHandle];
        
        public void SetReadOnlyColumn(int columnHandle, bool value)
        {
            this.Columns[columnHandle].ColumnObj = GetColumnContainer(columnHandle).WithIsReadOnly(value);
        }

        public void SetCaption(int columnHandle, string value)
        {
            this.Columns[columnHandle].ColumnObj = GetColumnContainer(columnHandle).WithCaption(value);
        }

        public bool ColumnHasExpression(int columnHandle)
        {
            return  GetColumn(columnHandle).ExpressionLink != null;
        }
        
        public override void Dispose()
        {
            base.Dispose();

            foreach (DataColumn column in this.Columns)
            {
                column.ExpressionLink.Dispose();
            }
              
            if (ColumnContainsInExpressionMap != null)
            {
                foreach (var kv in ColumnContainsInExpressionMap)
                {
                    kv.Value?.Dispose();
                }

                ColumnContainsInExpressionMap.Dispose();
                ColumnContainsInExpressionMap = null;
            }
            
            if (ExpressionDependentColumn != null)
            {
                foreach (var kv in ExpressionDependentColumn)
                {
                    kv.Value?.Dispose();
                }

                ExpressionDependentColumn.Dispose();
                ExpressionDependentColumn = null;
            }
        }

        public override void Clear()
        {
            base.Clear();

            if (ColumnContainsInExpressionMap != null)
            {
                foreach (var kv in ColumnContainsInExpressionMap)
                {
                    kv.Value?.Clear();
                }

                ColumnContainsInExpressionMap.Clear();
            }

            if (ExpressionDependentColumn != null)
            {
                foreach (var kv in ExpressionDependentColumn)
                {
                    kv.Value?.Clear();
                }

                ExpressionDependentColumn.Clear();
            }
        }

        public override void CopyFrom(CoreDataTable owner, CoreDataColumnInfo sourceColumnInfo, bool withData)
        {
            base.CopyFrom(owner, sourceColumnInfo, withData);

            if (sourceColumnInfo is DataColumnInfo sdc)
            {
                if (sdc.ColumnContainsInExpressionMap != null)
                {
                    ColumnContainsInExpressionMap = new(StringComparer.OrdinalIgnoreCase);

                    foreach (var kv in sdc.ColumnContainsInExpressionMap)
                    {
                        if (kv.Value != null)
                        {
                            ColumnContainsInExpressionMap[kv.Key] = new Set<int>(kv.Value);
                        }
                    }
                }
                
                if (sdc.ExpressionDependentColumn != null)
                {
                    ExpressionDependentColumn = new();
                    
                    foreach (var kv in sdc.ExpressionDependentColumn)
                    {
                        if (kv.Value != null)
                        {
                            ExpressionDependentColumn[kv.Key] = new Data<string>(kv.Value);
                        }
                    }
                }
            }
        }

        public override bool CanRemove(CoreDataColumn column, bool isThrowEx, string tableName)
        {
            if (base.CanRemove(column, isThrowEx, tableName))
            {
                if(ColumnContainsInExpressionMap != null)
                {
                    var dataColumn = (DataColumn)column;

                    var columnToRemove = dataColumn.ColumnName;

                    if (dataColumn.ExpressionLink == null)
                    {
                        if (ColumnContainsInExpressionMap.TryGetValue(columnToRemove, out var dependentColumns))
                        {
                            foreach (var dependantColHandle in dependentColumns)
                            {
                                var dependantDataColumn = GetColumn(dependantColHandle);

                                DataExpression dataExpression = dependantDataColumn.ExpressionLink;

                                if (dataExpression != null && dataExpression.DependsOn(columnToRemove))
                                {
                                    if (!isThrowEx)
                                    {
                                        return false;
                                    }

                                    var expression = dataExpression.Expression ?? "Empty";

                                    throw new DataException($"Cannot remove '{column.ColumnName}' column from '{tableName}' data table, because there is a dependent '{expression}' expression for '{Columns[dependantColHandle].ColumnName}' column.");
                                }
                            }
                        }
                    }
                }
                
                return true;
            }

            return false;
        }
        
        public void AddDependentColumn(int expressionColumnHandle, string dependentColumnName)
        {
            if (ColumnContainsInExpressionMap == null)
            {
                ColumnContainsInExpressionMap = new ();
            }
            
            if (ExpressionDependentColumn == null)
            {
                ExpressionDependentColumn = new ();
            }

            if (ColumnContainsInExpressionMap.TryGetValue(dependentColumnName, out var expressionColumns) == false)
            {
                expressionColumns = new ();

                ColumnContainsInExpressionMap[dependentColumnName] = expressionColumns;
            }

            expressionColumns.Add(expressionColumnHandle);
            
            if(ExpressionDependentColumn.TryGetValue(expressionColumnHandle, out var depColumns) == false)
            {
                depColumns = new();

                ExpressionDependentColumn[expressionColumnHandle] = depColumns;
            }

            depColumns.Add(dependentColumnName);
        }

        public override void Remove(CoreDataColumn column)
        {
            var columnHandle = column.ColumnHandle;

            var dataColumn = (DataColumn)column;

            dataColumn.ExpressionLink?.Dispose();

            base.Remove(column);
            
            if (ColumnContainsInExpressionMap != null)
            {
                if (ColumnContainsInExpressionMap.TryGetValue(column.ColumnName, out var columns))
                {
                    columns.Dispose();
                }

                ColumnContainsInExpressionMap.Remove(column.ColumnName);

                foreach (var kv in ColumnContainsInExpressionMap)
                {
                    kv.Value.Remove(columnHandle);

                    var decArray = kv.Value.Where(v => v > columnHandle).ToArray();

                    foreach (var decValue in decArray)
                    {
                        kv.Value.Remove(decValue);
                        kv.Value.Add(decValue - 1);
                    }
                }
            }

            if (ExpressionDependentColumn != null)
            {
                if (ExpressionDependentColumn.TryGetValue(columnHandle, out var columns))
                {
                    columns.Dispose();
                }

                ExpressionDependentColumn.Remove(columnHandle);

                foreach (var kv in ExpressionDependentColumn)
                {
                    kv.Value.Remove(column.ColumnName);
                }
            }
        }

        protected override T SerializeColumn<T, V>(SerializerAdapter<T, V> serializer, T ele, CoreDataColumn column, Set<int> pkCols)
        {
            var col = base.SerializeColumn(serializer, ele, column, pkCols);

            if (column is DataColumn dc)
            {
                if (dc.Caption != column.ColumnName && string.IsNullOrEmpty(dc.Caption) == false)
                {
                    serializer.AppendAttribute(col, serializer.CreateAttribute("Caption", dc.Caption));
                }

                if (dc.FixType != DataColumnType.Common)
                {
                    serializer.AppendAttribute(col, serializer.CreateAttribute("FixType", dc.FixType.ToString()));
                }

                if (string.IsNullOrEmpty(dc.Expression) == false)
                {
                    serializer.AppendAttribute(col, serializer.CreateAttribute("Expression", dc.Expression));
                }

                if (dc.IsReadOnly && string.IsNullOrEmpty(dc.Expression))
                {
                    serializer.AppendAttribute(col, serializer.CreateAttribute("ReadOnly", "true"));
                }

                if (column.AllowNull == false)
                {
                    serializer.AppendAttribute(col, serializer.CreateAttribute("AllowNull", "false"));
                }
            }

            return col;
        }

        protected override CoreDataColumn DeserializeColumn<T, V>(CoreDataTable table,
            SerializerAdapter<T, V> serializer, bool buildinDefault, T colElement)
        {
            var column = (DataColumn)base.DeserializeColumn(table, serializer, buildinDefault, colElement);

            var dt = (DataTable)table;

            var caption = serializer.GetAttributeValue(colElement, "Caption");
            var readOnly = serializer.GetAttributeValue(colElement, "ReadOnly") == "true" ? true : new bool?();
            var dataExpression = serializer.GetAttributeValue(colElement, "Expression");

            var hasExpression = string.IsNullOrEmpty(dataExpression) == false;

            bool readOnlyValue = false;
            
            if (readOnly.HasValue || hasExpression)
            {
                readOnlyValue = readOnly ?? true;
            }

            column.ColumnObj = column.ColObj.WithCaptionExpressionReadOnly(caption, dataExpression, readOnlyValue);

            if (hasExpression)
            {
                var expression = new DataExpression(dt, dataExpression);

                var dependantColumns = expression.GetDependency();

                column.ExpressionLink = expression;

                foreach (var dependantColumn in dependantColumns)
                {
                    AddDependentColumn(column.ColumnHandle, dependantColumn);
                }
            }

            return column;
        }

        public override void RemapColumnHandles(Map<int, int> oldToNewMap)
        {
            base.RemapColumnHandles(oldToNewMap);
            
            if (ColumnContainsInExpressionMap != null)
            {
                foreach (var kv in ColumnContainsInExpressionMap)
                {
                    Set<int> kvValue = kv.Value;

                    foreach (var oldHandle in kvValue.ToArray())
                    {
                        if (oldToNewMap.TryGetValue(oldHandle, out var newHandle))
                        {
                            if (newHandle != oldHandle)
                            {
                                kvValue.Remove(oldHandle);
                                kvValue.Add(newHandle);
                            }
                        }
                    }
                }
            }

            if (ExpressionDependentColumn != null)
            {
                var oldKv = ExpressionDependentColumn.ToArray();

                foreach (var oldHandle in oldKv)
                {
                    if (oldToNewMap.TryGetValue(oldHandle.Key, out var newHandle))
                    {
                        if (newHandle != oldHandle.Key)
                        {
                            ExpressionDependentColumn.Remove(oldHandle.Key);
                            ExpressionDependentColumn[newHandle] = oldHandle.Value;
                        }
                    }
                }
            }
        }
    }
}