using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public static class CoreRowExtensions
    {
        public static T CopyToDataTable<T>(this IEnumerable<IDataRowAccessor> rows) 
            where T:  CoreDataTable, new()
        {
            var table = new T();

            var list = rows;

            var firstRow = list.FirstOrDefault();

            if (firstRow != null)
            {
                table.Name = firstRow.GetTableName();

                foreach (var column in firstRow.GetColumns())
                {
                    table.AddColumn(column);
                }

                var loadState = table.BeginLoad();

                try
                {
                    var dataRows = table.LoadRows(list, overrideExisting: true);
                    
                    dataRows.Dispose();
                }
                finally
                {
                    loadState.EndLoad();
                }
            }

            return table;
        }

        public static void CopyToDataTable<T, D>(this IEnumerable<T> rows, D table) where T : ICoreDataRowReadOnlyAccessor where D : IDataTable
        {
            var list = rows.OfType<ICoreDataRowReadOnlyAccessor>().ToData();

            var firstRow = list.FirstOrDefault();

            if (firstRow != null)
            {
                table.TableName = firstRow.GetTableName();

                foreach (var column in firstRow.GetColumns())
                {
                    table.AddColumn(column);
                }

                var transaction = table.StartTransaction();

                try
                {
                    table.LoadRows(list, overrideExisting: true);
                    
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();

                    throw;
                }
            }
            list.Dispose();
        }

        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> rows, string fieldName) where T : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            return rows.OrderByDescending(
                r =>
                {
                    if (column == null)
                    {
                        column = r.GetColumn(fieldName);
                    }

                    return r[column];
                });
        }
        
        private class DataColumnComparer : IComparer<ICoreDataRowReadOnlyAccessor>
        {
            private readonly Data<ICoreTableReadOnlyColumn> m_columns;

            public DataColumnComparer(Data<ICoreTableReadOnlyColumn> columns)
            {
                m_columns = columns;
            }
            
            public int Compare(ICoreDataRowReadOnlyAccessor x, ICoreDataRowReadOnlyAccessor y)
            {
                foreach (var column in m_columns)
                {
                    var compareTo = x.Field<IComparable>(column).CompareTo(y.Field<IComparable>(column));

                    if (compareTo != 0)
                    {
                        return compareTo;
                    }
                }

                return 0;
            }
        }
        
        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> rows, [NotNull] Data<ICoreTableReadOnlyColumn> columns) where T : ICoreDataRowReadOnlyAccessor
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }
            
            return rows.OrderBy(r => r, new DataColumnComparer(columns));
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> rows, string fieldName) where T : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            return rows.OrderBy(
                r =>
                {
                    if (column == null)
                    {
                        column = r.GetColumn(fieldName);
                    }

                    return r[column];
                });
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source, string fieldName) where T : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            return source.CreateOrderedEnumerable(
                r =>
                {
                    if (column == null)
                    {
                        column = r.GetColumn(fieldName);
                    }

                    return r[column];
                },
                null,
                false);
        }

        public static IEnumerable<T> WhereFieldValue<T, V>(this IReadOnlyCollection<T> rows, string fieldName, V value) where T : IDataRowReadOnlyAccessor
        {
            return rows.WhereFieldValue(fieldName, value, EqualityComparer<V>.Default);
        }

        public static IEnumerable<T> WhereFieldValueStartsWith<T>(this IEnumerable<T> rows, string fieldName, string value, bool caseSensitive = true) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;
            
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (row.ToString(column).StartsWith(value, comparison))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueStartsWith<T>(this IDataTableRowEnumerable<T> rows, string fieldName, string value, bool caseSensitive = true) where T : IDataTableReadOnlyRow
        {
            return rows.Where(fieldName).AsString().StartsWith(value, caseSensitive);
        }

        public static IEnumerable<T> WhereFieldValueEndsWith<T>(this IEnumerable<T> rows, string fieldName, string value, bool caseSensitive = true) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }
                
                if (row.ToString(column).EndsWith(value, comparison))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueEndsWith<T>(this IDataTableRowEnumerable<T> rows, string fieldName, string value, bool caseSensitive = true) where T : IDataTableReadOnlyRow
        {
            return rows.Where(fieldName).AsString().EndsWith(value, caseSensitive);
        }

        public static IEnumerable<T> WhereFieldValueContains<T>(this IEnumerable<T> rows, string fieldName, string value, bool caseSensitive = true) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;
            
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (row.ToString(column).Contains(value, comparison))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueContains<T>(this IDataTableRowEnumerable<T> rows, string fieldName, string value, bool caseSensitive = true) where T : IDataTableReadOnlyRow 
        {
            return rows.Where(fieldName).AsString().Contains(value, caseSensitive);
        }

        public static IEnumerable<T> WhereFieldValueGreaterThan<T, V>(this IEnumerable<T> rows, string fieldName, V value) where T : IDataRowReadOnlyAccessor where V: IComparable
        {
            ICoreTableReadOnlyColumn column = null;

            var comparer = Comparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (comparer.Compare(row.Field<V>(column), value) > 1)
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueGreaterThan<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).Greater(value);
        }

        public static IEnumerable<T> WhereFieldValueLessThan<T, V>(this IEnumerable<T> rows, string fieldName, V value) where T : IDataRowReadOnlyAccessor where V : IComparable
        {
            ICoreTableReadOnlyColumn column = null;

            var comparer = Comparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (comparer.Compare(row.Field<V>(column), value) < 0)
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueLessThan<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).Lesser(value);
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValue<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value) where T : IDataTableReadOnlyRow where V: IComparable
        {
            return rows.Where(fieldName).Equals(value);
        }

        public static IEnumerable<T> WhereFieldValue<T, V>(this IEnumerable<T> rows, string fieldName, V value) where T : IDataRowReadOnlyAccessor
        {
            return WhereFieldValue(rows, fieldName, value, EqualityComparer<V>.Default);
        }

        public static IEnumerable<T> WhereFieldValue<T, V>(this IEnumerable<T> rows, string fieldName, V value, IEqualityComparer<V> comparer) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (comparer.Equals(row.Field<V>(column), value))
                {
                    yield return row;
                }
            }
        }

        public static IEnumerable<T> WhereFieldValueIn<T, V>(this IEnumerable<T> rows, string fieldName, V value1, V value2) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            IEqualityComparer<V> comparer = EqualityComparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var field = row.Field<V>(column);

                if (comparer.Equals(field, value1) || comparer.Equals(field, value2))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueIn<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value1, V value2) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).In(value1, value2);
        }

        public static IEnumerable<T> WhereFieldValueIn<T, V>(this IEnumerable<T> rows, string fieldName, V value1, V value2, V value3) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            IEqualityComparer<V> comparer = EqualityComparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var field = row.Field<V>(column);

                if (comparer.Equals(field, value1) || comparer.Equals(field, value2) || comparer.Equals(field, value3))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueIn<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value1, V value2, V value3) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).In(value1, value2, value3);
        }

        public static IEnumerable<T> WhereFieldValueIn<T, V>(this IEnumerable<T> rows, string fieldName, V value1, V value2, V value3, V value4) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            IEqualityComparer<V> comparer = EqualityComparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var field = row.Field<V>(column);

                if (comparer.Equals(field, value1) || comparer.Equals(field, value2) || comparer.Equals(field, value3) || comparer.Equals(field, value4))
                {
                    yield return row;
                }
            }
        }
        
        public static IEnumerable<T> WhereFieldValueIn<T, V>(this IEnumerable<T> rows, string fieldName, V value1, V value2, V value3, V value4, V value5) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            IEqualityComparer<V> comparer = EqualityComparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var field = row.Field<V>(column);

                if (comparer.Equals(field, value1) || comparer.Equals(field, value2) || comparer.Equals(field, value3) || comparer.Equals(field, value4) || comparer.Equals(field, value5))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueIn<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value1, V value2, V value3, V value4) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).In(value1, value2, value3, value4);
        }

        public static IEnumerable<T> WhereFieldNull<T>(this IEnumerable<T> rows, string fieldName) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (row.IsNull(column))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldNull<T>(this IDataTableRowEnumerable<T> rows, string fieldName) where T : IDataTableReadOnlyRow
        {
            return rows.Where(fieldName).Null();
        }

        public static IEnumerable<T> WhereHasSetDoesNotContainFieldValue<T, V>(this IEnumerable<T> rows, string fieldName, IReadOnlyCollection<V> set) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (!(set.Contains(row.Field<V>(column))))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereHasSetDoesNotContainFieldValue<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, IReadOnlyCollection<V> set) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).Not().In(set);
        }
        
        public static IEnumerable<T> WhereHashSetContainsFieldValue<T, V>(this IEnumerable<T> rows, string fieldName, IReadOnlyCollection<V> set) where T : IDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (row.IsNotNull(column) && set.Contains(row.Field<V>(column)))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereHashSetContainsFieldValue<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, IReadOnlyCollection<V> set) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).In(set);
        }

        public static IEnumerable<IGrouping<TKey, IDataTableRow>> GroupRowsByField<TKey>(this IEnumerable<IDataTableRow> source, string fieldName)
        {
            ICoreTableReadOnlyColumn column = null;

            return source.GroupBy(
                r =>
                {
                    if (column == null)
                    {
                        column = r.GetColumn(fieldName);
                    }

                    return r.Field<TKey>(column);
                });
        }

        public static IEnumerable<IGrouping<TKey, ICoreDataRowAccessor>> GroupRowsByField<TKey>(this IEnumerable<ICoreDataRowAccessor> source, string fieldName)
        {
            ICoreTableReadOnlyColumn column = null;

            return source.GroupBy(
                r =>
                {
                    if (column == null)
                    {
                        column = r.GetColumn(fieldName);
                    }

                    return r.Field<TKey>(column);
                });
        }

        public static IEnumerable<IGrouping<TKey, TRow>> GroupRowsByField<TKey, TRow>(this IEnumerable<TRow> source, string fieldName) where TRow : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            return source.GroupBy(
                r =>
                {
                    if (column == null)
                    {
                        column = r.GetColumn(fieldName);
                    }

                    return r.Field<TKey>(column);
                });
        }

        public static IEnumerable<T> WhereFieldNotNull<T>(this IEnumerable<T> rows, string fieldName) where T : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (row.IsNotNull(column))
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldNotNull<T>(this IDataTableRowEnumerable<T> rows, string fieldName) where T : IDataTableReadOnlyRow 
        {
            return rows.Where(fieldName).Not().Null();
        }

        public static V MinFieldValue<V>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName) where V: IComparable
        {
            //todo use N iteration instead of nlogn

            var dataRowAccessor = OrderBy(rows, fieldName).FirstOrDefault();

            if (dataRowAccessor != null)
            {
               return dataRowAccessor.Field<V>(fieldName);
            }

            return default(V);
        }

        public static V MaxFieldValue<V>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName) where V : IComparable
        {
            //todo use N iteration instead of nlogn

            var dataRowAccessor = OrderByDescending(rows, fieldName).FirstOrDefault();

            if (dataRowAccessor != null)
            {
                return dataRowAccessor.Field<V>(fieldName);
            }

            return default(V);
        }

        public static IEnumerable<T> WhereFieldValueNotEqual<T, V>(this IEnumerable<T> rows, string fieldName, V value) where T : IDataRowReadOnlyAccessor
        {
            IDataTableReadOnlyColumn column = null;

            var equalityComparer = EqualityComparer<V>.Default;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (equalityComparer.Equals(row.Field<V>(column), value) == false)
                {
                    yield return row;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<T> WhereFieldValueNotEqual<T, V>(this IDataTableRowEnumerable<T> rows, string fieldName, V value) where T : IDataTableReadOnlyRow where V : IComparable
        {
            return rows.Where(fieldName).Not().Equals(value);
        }

        public static IEnumerable<ICoreDataRowAccessor> SetFieldValue<T>(this IEnumerable<ICoreDataRowAccessor> rows, string fieldName, T value)
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                row.Set(column, value);

                yield return row;
            }
        }

        public static IEnumerable<T> SelectFieldValue<T>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName)
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                yield return row.Field<T>(column);
            }
        }

        public static IEnumerable<T> SelectNotNullFieldValue<T>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName)
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                if (row.IsNotNull(column))
                {
                    yield return row.Field<T>(column);
                }
            }
        }

        public static IEnumerable<Tuple<V1, V2>> SelectFieldsValue<V1, V2>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName1, string fieldName2)
        {
            ICoreTableReadOnlyColumn column1 = null;
            ICoreTableReadOnlyColumn column2 = null;

            foreach (var row in rows)
            {
                if (column1 == null)
                {
                    column1 = row.GetColumn(fieldName1);
                    column2 = row.GetColumn(fieldName2);
                }

                yield return new Tuple<V1, V2>(row.Field<V1>(column1), row.Field<V2>(column2));
            }
        }

        public static IEnumerable<Tuple<V1, V2, V3>> SelectFieldsValue<V1, V2, V3>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName1, string fieldName2, string fieldName3)
        {
            ICoreTableReadOnlyColumn column1 = null;
            ICoreTableReadOnlyColumn column2 = null;
            ICoreTableReadOnlyColumn column3 = null;

            foreach (var row in rows)
            {
                if (column1 == null)
                {
                    column1 = row.GetColumn(fieldName1);
                    column2 = row.GetColumn(fieldName2);
                    column3 = row.GetColumn(fieldName3);
                }

                yield return new Tuple<V1, V2, V3>(row.Field<V1>(column1), row.Field<V2>(column2), row.Field<V3>(column3));
            }
        }


        public static IEnumerable<Tuple<V1, V2, V3, V4>> SelectFieldsValue<V1, V2, V3, V4>(this IEnumerable<ICoreDataRowReadOnlyAccessor> rows, string fieldName1, string fieldName2, string fieldName3, string fieldName4)
        {
            ICoreTableReadOnlyColumn column1 = null;
            ICoreTableReadOnlyColumn column2 = null;
            ICoreTableReadOnlyColumn column3 = null;
            ICoreTableReadOnlyColumn column4 = null;

            foreach (var row in rows)
            {
                if (column1 == null)
                {
                    column1 = row.GetColumn(fieldName1);
                    column2 = row.GetColumn(fieldName2);
                    column3 = row.GetColumn(fieldName3);
                    column4 = row.GetColumn(fieldName4);
                }

                yield return new Tuple<V1, V2, V3, V4>(row.Field<V1>(column1), row.Field<V2>(column2), row.Field<V3>(column3), row.Field<V4>(column4));
            }
        }

        public static IEnumerable<T> ExceptByField<T>(this IEnumerable<T> rows1, IEnumerable<T> rows2, string fieldName) where T : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            return rows1.ExceptBy(
                rows2,
                row =>
                {
                    if (column == null)
                    {
                        column = row.GetColumn(fieldName);
                    }

                    return row[column];
                });

        }

        public static T ValueOrDefault<T>(this ICoreDataRowReadOnlyAccessor row, string field, Func<T> defaultValue = null)
        {
            var column = row.TryGetColumn(field);

            if (column == null)
            {
                if (defaultValue == null)
                {
                    return default(T);
                }

                return defaultValue();
            }

            if (row.IsNull(column))
            {
                if (defaultValue == null)
                {
                    return default(T);
                }

                return defaultValue();
            }

            return row.Field<T>(column);
        }

      
		

     
        public static IEnumerable<R> FilterByFieldValue2<R,T>(this IEnumerable<R> rowForCheck, string fieldName1, IReadOnlyCollection<T> list1, string fieldName2, IReadOnlyCollection<T> list2) where R : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column1 = null;
            ICoreTableReadOnlyColumn column2 = null;

            foreach (var dataTableRow in rowForCheck)
            {
                if (column1 == null)
                {
                    column1 = dataTableRow.GetColumn(fieldName1);
                }

                if (column2 == null)
                {
                    column2 = dataTableRow.GetColumn(fieldName2);
                }

                if (list1.Contains(dataTableRow.Field<T>(column1)) || list2.Contains(dataTableRow.Field<T>(column2)))
                {
                    yield return dataTableRow;
                }
            }
        }


        public static IDataTableRowEnumerableReadOnly<IDataTableRow> FilterByFieldValue<T>(this IDataTableRowEnumerable<IDataTableRow> rowForCheck, string fieldName, IReadOnlyCollection<T> list) where T : IComparable
        {
             return rowForCheck.Where(fieldName).In(list);
        }

        public static IEnumerable<ICoreDataRowAccessor> FilterByFieldValue<T>(this IEnumerable<ICoreDataRowAccessor> rowForCheck, string fieldName, IEnumerable<T> list)
        {
            return (from c1 in rowForCheck.Where(r => r.IsNull(fieldName) == false)
                    join c2 in list on c1.Field<T>(fieldName) equals c2
                    select c1);
        }

        public static IEnumerable<ICoreDataRowAccessor> FilterByFieldValueNotEquals<T>(this IEnumerable<ICoreDataRowAccessor> rowForCheck, string fieldName, IReadOnlyCollection<T> list)
        {
            ICoreTableReadOnlyColumn column = null;

            foreach (var dataTableRow in rowForCheck)
            {
                if (column == null)
                {
                    column = dataTableRow.GetColumn(fieldName);
                }

                if (list.Contains(dataTableRow.Field<T>(column)) == false)
                {
                    yield return dataTableRow;
                }
            }
        }

        public static IDataTableRowEnumerableReadOnly<IDataTableRow> FilterByFieldValueNotEquals<T>(this IDataTableRowEnumerable<IDataTableRow> rowForCheck, string fieldName, IReadOnlyCollection<T> list) where T : IComparable
        {
            return rowForCheck.Where(fieldName).Not().In(list);
        }

        public static IEnumerable<V> JoinRows<T, V, X, S>(this IEnumerable<T> collection, IEnumerable<S> rows, string fieldName, Func<T, X> keySelection, Func<T, S, V> resultSelector) where S : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            var dict = new Map<X, S>();

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var value = row.Field<X>(column);

                dict[value] = row;
            }

            foreach (var item in collection)
            {
                var key = keySelection(item);

                if (dict.TryGetValue(key, out var row))
                {
                    yield return resultSelector(item, row);
                }
            }

            dict.Dispose();
        }

        public static IEnumerable<V> LeftJoinRows<T, V, X, S>(this IEnumerable<T> collection, IEnumerable<S> rows, string fieldName, Func<T, X> keySelection, Func<T, S, V> resultSelector) where S : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            var dict = new Map<X, S>();

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var value = row.Field<X>(column);

                dict[value] = row;
            }

            foreach (var item in collection)
            {
                var key = keySelection(item);

                dict.TryGetValue(key, out var row);

                yield return resultSelector(item, row);
            }

            dict.Dispose();
        }


        public static IEnumerable<V> JoinCollection<T, V, X, S>(this IEnumerable<S> rows, string fieldName, IEnumerable<T> collection, Func<T, X> keySelection, Func<T, S, V> resultSelector) where S : ICoreDataRowReadOnlyAccessor
        {
            var dict = new Map<X, T>();

            foreach (var item in collection)
            {
                var key = keySelection(item);

                dict[key] = item;
            }

            ICoreTableReadOnlyColumn column = null;

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var value = row.Field<X>(column);

                if (dict.TryGetValue(value, out var item))
                {
                    yield return resultSelector(item, row);
                }
            }

            dict.Dispose();
        }

        public static IEnumerable<V> LeftJoinCollection<T, V, X, S>(this IEnumerable<S> rows, string fieldName, IEnumerable<T> collection, Func<T, X> keySelection, Func<T, S, V> resultSelector) where S : ICoreDataRowReadOnlyAccessor
        {
            ICoreTableReadOnlyColumn column = null;

            var dict = new Map<X, T>();

            foreach (var item in collection)
            {
                var key = keySelection(item);

                dict[key] = item;
            }

            foreach (var row in rows)
            {
                if (column == null)
                {
                    column = row.GetColumn(fieldName);
                }

                var value = row.Field<X>(column);

                dict.TryGetValue(value, out var item);

                yield return resultSelector(item, row);
            }

            dict.Dispose();
        }

  
		public static T GetValueOrDefault<T>(this ICoreDataRowReadOnlyAccessor row, string field, T defaultValue)
        {
            if (row.IsExistsField(field))
            {
                return row.Field(field, defaultValue);
            }

            return defaultValue;
        }

        public static T GetValueOrDefault<T>(this ICoreDataRowReadOnlyAccessor row, string field)
        {
            return GetValueOrDefault(row, field, default(T));
        }


        /// <summary>
        /// Returns the set of elements in the first sequence which aren't
        ///             in the second sequence, according to a given key selector.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// This is a set operation; if multiple elements in <paramref name="first"/> have
        ///             equal keys, only the first such element is returned.
        ///             This operator uses deferred execution and streams the results, although
        ///             a set of keys from <paramref name="second"/> is immediately selected and retained.
        /// 
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam><typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam><param name="first">The sequence of potentially included elements.</param><param name="second">The sequence of elements whose keys may prevent elements in
        ///             <paramref name="first"/> from being returned.</param><param name="keySelector">The mapping from source element to key.</param>
        /// <returns>
        /// A sequence of elements from <paramref name="first"/> whose key was not also a key for
        ///             any element in <paramref name="second"/>.
        /// </returns>
        internal static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
        {
            return ExceptBy(first, second, keySelector, null);
        }

        /// <summary>
        /// Returns the set of elements in the first sequence which aren't
        ///             in the second sequence, according to a given key selector.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// This is a set operation; if multiple elements in <paramref name="first"/> have
        ///             equal keys, only the first such element is returned.
        ///             This operator uses deferred execution and streams the results, although
        ///             a set of keys from <paramref name="second"/> is immediately selected and retained.
        /// 
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam><typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam><param name="first">The sequence of potentially included elements.</param><param name="second">The sequence of elements whose keys may prevent elements in
        ///             <paramref name="first"/> from being returned.</param><param name="keySelector">The mapping from source element to key.</param><param name="keyComparer">The equality comparer to use to determine whether or not keys are equal.
        ///             If null, the default equality comparer for <c>TSource</c> is used.</param>
        /// <returns>
        /// A sequence of elements from <paramref name="first"/> whose key was not also a key for
        ///             any element in <paramref name="second"/>.
        /// </returns>
        internal static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            return ExceptByImpl(first, second, keySelector, keyComparer);
        }

        private static IEnumerable<TSource> ExceptByImpl<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            var keys = new Set<TKey>(second.Select(keySelector), keyComparer);
            foreach (TSource source in first)
            {
                TKey key = keySelector(source);
                if (!keys.Contains(key))
                {
                    yield return source;
                    keys.Add(key);
                    key = default(TKey);
                }
            }
            
            keys.Dispose();
        }
    }
}
