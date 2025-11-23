using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using Brudixy.Converter;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    partial class CoreDataTable
    {
        public const string JoinResultTableName = "JoinResult";

        public static T LeftJoin<T>(
            [NotNull] string key1,
            [NotNull] string key2,
            [NotNull] CoreDataTable table1,
            [NotNull] CoreDataTable table2,
            [NotNull] IEnumerable<string> columns1,
            [NotNull] IEnumerable<string> columns2,
            string joinTableName = JoinResultTableName)
            where T : CoreDataTable, new()
        {
            return JoinOneSide<T>(key1, key2, table1, table2, columns1, columns2, false, joinTableName);
        }

        public static T RightJoin<T>(
         [NotNull] string key1,
         [NotNull] string key2,
         [NotNull] CoreDataTable table1,
         [NotNull] CoreDataTable table2, 
         [NotNull] IEnumerable<string> columns1,
         [NotNull] IEnumerable<string> columns2, 
         string joinTableName = JoinResultTableName)
            where T : CoreDataTable, new()
        {
            Tool.Swap(ref key1, ref key2);
            Tool.Swap(ref table1, ref table2);
            Tool.Swap(ref columns1, ref columns2);

            return JoinOneSide<T>(key1, key2, table1, table2, columns1, columns2, true, joinTableName);
        }

        public static T InnerJoin<T>(
            [NotNull] string key1,
            [NotNull] string key2,
            [NotNull] CoreDataTable table1,
            [NotNull] CoreDataTable table2,
            [NotNull] IEnumerable<string> table1Fields,
            [NotNull] IEnumerable<string> table2Fields,
            string joinTableName = JoinResultTableName)
        where T : CoreDataTable, new()
        {
            var columns1 = new Data<ICoreTableReadOnlyColumn>();
            var columns2 = new Data<ICoreTableReadOnlyColumn>();

            foreach (var table1Field in table1Fields)
            {
                columns1.Add(table1.GetColumn(table1Field));
            }

            foreach (var table2Field in table2Fields)
            {
                columns2.Add(table2.GetColumn(table2Field));
            }

            CheckKeyTypes(key1, key2, table1, table2);

            var dataColumns = GetDataColumnsToMap(table1, table2, columns1, columns2);

            var valueArray = ArrayPool<object>.Shared.Rent(dataColumns.Count);
            
            valueArray.Clear();

            var resultSet = new T();

            resultSet.Name = joinTableName;

            var dataEdit = resultSet.BeginLoad();

            PrepareColumnCollections(resultSet, dataColumns, table1, table2, out var mapping, out var table1Columns, out var table2Columns);

            HashSet<IComparable> set1 = GetKeyValueSet(key1, table1);
            HashSet<IComparable> set2 = GetKeyValueSet(key2, table2);

            set1.IntersectWith(set2);

            foreach (var sameKeyValue in set1)
            {
                var rows1 = table1.GetRows<CoreDataRow, IComparable>(key1, sameKeyValue);
                var rows2 = table2.GetRows<CoreDataRow, IComparable>(key2, sameKeyValue);

                var row1 = rows1.First();
                var row2 = rows2.First();

                FillValueArray(table1, table1Columns, mapping, valueArray, row1, firstTableRows: true, rightJoin: false);
                FillValueArray(table2, table2Columns, mapping, valueArray, row2, firstTableRows: false, rightJoin: false);

                resultSet.ImportRow(RowState.Unchanged, valueArray);

                valueArray.Clear();
            }
            
            ArrayPool<object>.Shared.Return(valueArray);

            dataEdit.EndLoad();
            
            columns1.Dispose();
            columns2.Dispose();

            return resultSet;
        }

        private static HashSet<IComparable> GetKeyValueSet(string key1, CoreDataTable table1)
        {
            var column = table1.GetColumn(key1);

            var dataItem = column.DataStorageLink;

            var set1 = new HashSet<IComparable>();

            foreach (var rowsHandle in table1.RowsHandles)
            {
                var comparable = dataItem.GetData(rowsHandle, column) as IComparable;

                if (comparable != null)
                {
                    set1.Add(comparable);
                }
            }
            return set1;
        }

        private static T JoinOneSide<T>(
            string key1, 
            string key2, 
            CoreDataTable table1,
            CoreDataTable table2,
            IEnumerable<string> table1Fields, 
            IEnumerable<string> table2Fields,
            bool rightJoin,
            string joinTableName)
            where T : CoreDataTable, new()
        {
            var table1Columns = new Data<ICoreTableReadOnlyColumn>();
            var table2Columns = new Data<ICoreTableReadOnlyColumn>();

            foreach (var table1Field in table1Fields)
            {
                table1Columns.Add(table1.GetColumn(table1Field));
            }

            foreach (var table2Field in table2Fields)
            {
                table2Columns.Add(table2.GetColumn(table2Field));
            }

            CheckKeyTypes(key1, key2, table1, table2);

            var resultSet = new T();

            resultSet.Name = joinTableName;

            var dataEdit = resultSet.BeginLoad();

            var dataColumns = GetDataColumnsToMap(table1, table2, table1Columns, table2Columns);

            var valueArray = ArrayPool<object>.Shared.Rent(dataColumns.Count);
            
            valueArray.Clear();

            PrepareColumnCollections(resultSet, dataColumns, table1, table2, out var mapping, out var columns1, out var columns2, rightJoin);

            foreach (var row1 in table1.Rows)
            {
                FillValueArray(table1, columns1, mapping, valueArray, row1, firstTableRows: true, rightJoin: rightJoin);

                var row2 = table2.GetRows<CoreDataRow, IComparable>(key2, (IComparable)row1[key1]).FirstOrDefault();

                if (row2 != null)
                {
                    FillValueArray(table2, columns2, mapping, valueArray, row2, firstTableRows: false, rightJoin: rightJoin);
                }

                resultSet.ImportRow(RowState.Unchanged, valueArray);

                Array.Clear(valueArray, 0, valueArray.Length - 1);
            }
            
            ArrayPool<object>.Shared.Return(valueArray);

            dataEdit.EndLoad();

            return resultSet;
        }

        private static Data<CoreDataColumnContainer> GetDataColumnsToMap(CoreDataTable table1, CoreDataTable table2, IReadOnlyCollection<ICoreTableReadOnlyColumn> columns1, IReadOnlyCollection<ICoreTableReadOnlyColumn> columns2)
        {
            IEnumerable<ICoreTableReadOnlyColumn> column1 = columns1;
            IEnumerable<ICoreTableReadOnlyColumn> column2 = columns2;

            var table1Name = table1.Name + "_1";
            var table2Name = table2.Name + "_2";

            if (columns1.Count == 0 && columns2.Count == 0)
            {
                column1 = table1.GetColumns();
                column2 = table2.GetColumns();
            }

            return column1
                     .Select(col => new CoreDataColumnContainer(col) { TableName = table1Name })
                    .Union(column2.Select(col => new CoreDataColumnContainer(col) { TableName = table2Name }))
                    .ToData();
        }

        private static void CheckKeyTypes(string key1, string key2, CoreDataTable table1, CoreDataTable table2)
        {
            var column1 = table1.GetColumn(key1);
            var column2 = table2.GetColumn(key2);

            if (column1.Type != column2.Type)
            {
                throw new InvalidOperationException(
                    $"Types of columns are incompatible. Key1 column {table1.Name}.{key1} type is {column1.Type};  Key2 column {table2.Name}.{key2} type is {column2.Type}.");
            }
        }

        private static void PrepareColumnCollections(
            CoreDataTable resultSet,
            Data<CoreDataColumnContainer> columns, 
            CoreDataTable table1, 
            CoreDataTable table2, 
            out Map<string, int> mapping, 
            out Data<CoreDataColumnContainer> table1Columns, 
            out Data<CoreDataColumnContainer> table2Columns,
            bool rightJoin = false)
        {
            mapping = new ();

            table1Columns = new ();
            table2Columns = new ();

            var t1 = table1.Name + "_1";
            var t2 = table2.Name + "_2";

            for (int index = 0; index < columns.Count; index++)
            {
                var column = columns[index];

                bool first = false;

                if (column.TableName == t1)
                {
                    first = true;

                    table1Columns.Add(column);
                }
                else if (column.TableName == t2)
                {
                    table2Columns.Add(column);
                }
                else
                {
                    continue;
                }

                var resultSetColumnName = GetResultSetColumnName(first, column, rightJoin);

                resultSet.AddColumn(resultSetColumnName, column.Type);

                mapping[resultSetColumnName] = index;
            }
        }

        private static string GetResultSetColumnName(bool first, CoreDataColumnContainer column, bool rightJoin)
        {
            if (rightJoin)
            {
                first = !first;
            }

            var resultSetColumnName = (first ? "t1" : "t2") + "." + column.ColumnName;
            return resultSetColumnName;
        }

        private static void FillValueArray(CoreDataTable table, IEnumerable<CoreDataColumnContainer> columns, Map<string, int> mapping, object[] valueArray, CoreDataRow row, bool firstTableRows, bool rightJoin)
        {
            foreach (var column in columns)
            {
                var resultSetColumnName = GetResultSetColumnName(firstTableRows, column, rightJoin);

                var i = mapping[resultSetColumnName];

                valueArray[i] = table.GetRowFieldValue(row.RowHandleCore, table.GetColumn(column.ColumnName), DefaultValueType.ColumnBased, null);
            }
        }
    }
}
