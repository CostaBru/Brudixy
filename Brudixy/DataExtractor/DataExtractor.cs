using System.Collections;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class DataExtractor<T> : 
        IDataTableRowEnumerable<T>, 
        IDataTableRowEnumerableToConcrete<T>,
        IDataTableRowEnumerableStringToConcrete<T>
        where T : IDataTableReadOnlyRow
    {
        private readonly DataTable m_dataTable;
        private readonly byte m_allRows;

        private readonly Map<string, Data<ConditionValue>> m_conditionsValues = new(StringComparer.OrdinalIgnoreCase);
        private readonly Data<string> m_conditions = new();

        public DataExtractor(DataTable dataTable, bool allRows = false)
        {
            m_dataTable = dataTable;
            m_allRows = (byte)(allRows ? 1 : 0);
        }

        private enum SearchType
        {
            Equals,
            StartsWith,
            EndsWith,
            Contains,
            Greater,
            Lesser,
            GreaterOrEquals,
            LesserOrEquals,
            In,
        }

        private class ConditionValue
        {
            public MultiValueWrapper Values;

            public IEnumerable<IComparable> Comparables
            {
                get
                {
                    foreach (var comparable in Values.Items.OfType<IComparable>())
                    {
                        yield return comparable;
                    }
                }
            }

            public bool Null;

            public bool Negate;

            public bool Empty;

            public SearchType SearchType;

            public bool CaseSensitive;
        }

        public IDataTableRowEnumerableToConcrete<T> Where(string column)
        {
            return WhereCore(column);
        }

        private DataExtractor<T> WhereCore(string column)
        {
            m_conditionsValues[column] = new Data<ConditionValue> { new ConditionValue {Empty = true} };

            m_conditions.Add(column);

            return this;
        }

        public IDataTableRowEnumerableStringToConcrete<T> AsString()
        {
             return this;
        }

        public IDataTableRowEnumerable<T> Add(IDataRowReadOnlyAccessor newRow)
        {
            m_dataTable.ImportRow(newRow);

            return this;
        }

        public IDataTableRowEnumerable<T> AddRange(IEnumerable<IDataRowReadOnlyAccessor> newRows)
        {
            m_dataTable.ImportRows(newRows);
            
            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Equals<V>(V? value) where V : struct, IComparable
        {
            if (value == null)
            {
                return Null();
            }

            return Equals(value.Value);
        }

        public IDataTableRowEnumerableReadOnly<T> Equals<V>(V value) where V : IComparable
        {
            PrepareCondition(value, SearchType.Equals);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Equals(string value, bool caseSensitive = true)
        {
            PrepareCondition(value, SearchType.Equals, caseSensitive);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1) where V : IComparable
        {
            PrepareCondition(value1, SearchType.Equals);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2) where V : IComparable
        {
            var list = new Data<IComparable> { value1, value2 };

            PrepareCondition(new MultiValueWrapper(list.Count, value1, v => list.Contains((V)v), list), SearchType.In, false);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3) where V : IComparable
        {
            var list = new Data<IComparable> { value1, value2, value3 };

            PrepareCondition(new MultiValueWrapper(list.Count, value1, v => list.Contains((V)v), list), SearchType.In, false);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4) where V : IComparable
        {
            var list = new Data<IComparable> { value1, value2, value3, value4 };

            PrepareCondition(new MultiValueWrapper(list.Count, value1, v => list.Contains((V)v), list), SearchType.In,  false);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4, V value5) where V : IComparable
        {
            var list = new Data<IComparable> { value1, value2, value3, value4, value5 };

            PrepareCondition(new MultiValueWrapper(list.Count, value1, v => list.Contains((V)v), list), SearchType.In,  false);

            return this; 
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(IReadOnlyCollection<V> values) where V : IComparable
        {
            PrepareCondition(new MultiValueWrapper(values.Count, values.First(), v => values.Contains((V)v), values), SearchType.In, false);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> StartsWith(string value, bool caseSensitive = true)
        {
            PrepareCondition(value, SearchType.StartsWith, caseSensitive);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> EndsWith(string value, bool caseSensitive = true)
        {
            PrepareCondition(value, SearchType.EndsWith, caseSensitive);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Contains(string value, bool caseSensitive = true)
        {
            PrepareCondition(value, SearchType.Contains, caseSensitive);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Greater<V>(V value) where V : IComparable
        {
            PrepareCondition(value, SearchType.Greater);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Lesser<V>(V value) where V : IComparable
        {
            PrepareCondition(value, SearchType.Lesser);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> GreaterOrEquals<V>(V value) where V : IComparable
        {
            PrepareCondition(value, SearchType.GreaterOrEquals);

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> LesserOrEquals<V>(V value) where V : IComparable
        {
            PrepareCondition(value, SearchType.LesserOrEquals);

            return this;
        }

        private void PrepareCondition(IComparable value, SearchType valueSearchType, bool caseSensitive = true)
        {
            PrepareCondition(new MultiValueWrapper(1, value, i => Equals(value, i)), valueSearchType, caseSensitive);
        }

        private class MultiValueWrapper
        {
            private IEnumerable m_enumerable;

            public MultiValueWrapper(int count, IComparable first, Func<IComparable, bool> check, IEnumerable enumerable = null)
            {
                Contains = check;

                Count = count;
                First = first;

                m_enumerable = enumerable;
            }

            [NotNull]
            public readonly Func<IComparable, bool> Contains;

            [CanBeNull]
            public readonly IComparable First;

            [NotNull]
            public IEnumerable Items
            {
                get
                {
                    if (m_enumerable != null)
                    {
                        return m_enumerable;
                    }

                    var list = new Data<IComparable>();

                    m_enumerable = list;

                    if (Count > 0)
                    {
                        list.Add(First);
                    }

                    return m_enumerable;
                }
            }

            public readonly int Count;
        }

        private void PrepareCondition(MultiValueWrapper values, SearchType valueSearchType, bool caseSensitive = true)
        {
            for (int i = m_conditions.Count - 1; i >= 0; i--)
            {
                var condition = m_conditions[i];

                var conditionsValue = m_conditionsValues[condition];

                bool anyEmpty = false;

                for (int j = conditionsValue.Count - 1; j >= 0; j--)
                {
                    var conditionValue = conditionsValue[j];

                    if (conditionValue.Empty)
                    {
                        anyEmpty = true;

                        conditionValue.Empty = false;
                        conditionValue.Values = values;
                        conditionValue.SearchType = valueSearchType;
                        conditionValue.CaseSensitive = caseSensitive;
                    }
                    else
                    {
                        break;
                    }
                }

                if (anyEmpty == false && valueSearchType != SearchType.Equals)
                {
                    conditionsValue.Add(new ConditionValue { Values = values, SearchType = valueSearchType, CaseSensitive = caseSensitive, Empty = false });

                    break;
                }
            }
        }

        public IDataTableRowEnumerableReadOnly<T> Null()
        {
            var last = m_conditions.Last();

            var conditionsValue = m_conditionsValues[last];

            var conditionValue = conditionsValue.Last();

            if (conditionValue.Empty)
            {
                conditionValue.Null = true;
            }
            else
            {
                conditionsValue.Add(new ConditionValue { Null = true });
            }

            return this;
        }

        public IDataTableRowEnumerableToConcrete<T> Not()
        {
            return NotCore();
        }

        IDataTableRowEnumerableStringToConcrete<T> IDataTableRowEnumerableStringToConcrete<T>.Not()
        {
            return NotCore();
        }

        private DataExtractor<T> NotCore()
        {
            var last = m_conditions.Last();

            var conditionsValue = m_conditionsValues[last];

            var conditionValue = conditionsValue.Last();

            if (conditionValue.Empty)
            {
                conditionValue.Negate = true;
            }
            else
            {
                conditionsValue.Add(new ConditionValue {Negate = true});
            }

            return this;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (m_conditions.Count == 0)
            {
                if (m_allRows == 1)
                {
                    foreach (var row in m_dataTable.GetAllRows().OfType<T>())
                    {
                        yield return row;
                    }
                }
                else
                {
                    foreach (var row in m_dataTable.GetRows().OfType<T>())
                    {
                        yield return row;
                    }
                }

                yield break;
            }

            OptimizeQuery(out var indexUseCondition, out var otherConditions, out var mulitIndexConditions);

            IEnumerable<DataRow> rows = TryFetchByIndexes(indexUseCondition, mulitIndexConditions);

            Func<IEnumerable<DataRow>> getRows;

            if (m_allRows == 1)
            {
                getRows = m_dataTable.GetAllRows;
            }
            else
            {
                getRows = m_dataTable.GetRows;
            }

            foreach (var condition in otherConditions)
            {
                var column = m_dataTable.GetColumn(condition);

                foreach (var conditionValue in m_conditionsValues[condition])
                {
                    rows = Filter(rows ?? getRows(), column, conditionValue);
                }
            }

            if (rows == null)
            {
                foreach (var row in getRows().OfType<T>())
                {
                    yield return row;
                }

                yield break;
            }

            foreach (var row in rows.OfType<T>())
            {
                yield return row;
            }
        }

        [CanBeNull]
        private IEnumerable<DataRow> TryFetchByIndexes(
            Data<(string field, int indexMapping)> indexUseCondition,
            Map<int, Data<string>> multiIndexConditions)
        {
            IEnumerable<DataRow> rows = null;

            foreach (var indexedCondition in indexUseCondition)
            {
                foreach (var conditionValue in m_conditionsValues[indexedCondition.field])
                {
                    if (rows == null)
                    {
                        if (conditionValue.SearchType == SearchType.In && conditionValue.Values.Count > 1)
                        {
                            var indexSearchResults = new Data<IEnumerable<DataRow>>(conditionValue.Values.Count);

                            foreach (var value in conditionValue.Comparables)
                            {
                                var dataRows = GetRowsBySingleColumnIndex(indexedCondition.indexMapping, value, SearchType.Equals);
                                
                                indexSearchResults.Add(dataRows);
                            }

                            rows = indexSearchResults.SelectMany(r => r).Distinct().ToArray();
                        }
                        else
                        {
                            rows = GetRowsBySingleColumnIndex(indexedCondition.indexMapping, conditionValue.Comparables.FirstOrDefault(), conditionValue.SearchType);
                        }
                    }
                    else
                    {
                        var column = m_dataTable.GetColumn(indexedCondition.field);

                        rows = Filter(rows, column, conditionValue);
                    }
                }
            }

            foreach (var mulitIndexCondition in multiIndexConditions)
            {
                if (rows == null)
                {
                    var index = m_dataTable.MultiColumnIndexInfo.Indexes[mulitIndexCondition.Key];

                    var values = new Data<IComparable>();

                    foreach (var columnHandle in index.Columns)
                    {
                        var column = m_dataTable.DataColumnInfo.Columns[columnHandle].ColumnName;

                        if(m_conditionsValues.TryGetValue(column, out var conditionValues))
                        {
                            values.Add(conditionValues[0].Values.First);
                        }
                        else
                        {
                            break;
                        }
                    }

                    rows = m_dataTable.GetRowsByMultiIndex<DataRow, IComparable>(mulitIndexCondition.Key, values).OfType<DataRow>();
                }
            }

            return rows;
        }

        private void OptimizeQuery(out Data<(string field, int indexMapping)> indexUseCondition, out Data<string> otherConditions, out Map<int, Data<string>> multiIndexUseCondition)
        {
            indexUseCondition = new Data<(string field, int indexMapping)>();
            multiIndexUseCondition = new Map<int, Data<string>>();
            otherConditions = new Data<string>(m_conditions.Count);

            foreach (var condition in m_conditions)
            {
                var conditionValues = m_conditionsValues[condition];

                bool canUse = false;

                var validCount = 0;
                var multiIndexValidCount = 0;

                foreach (var conditionValue in conditionValues)
                {
                    if (conditionValue.Negate)
                    {
                        break;
                    }

                    if (conditionValue.Empty)
                    {
                        break;
                    }


                    if (conditionValue.SearchType != SearchType.In && conditionValue.SearchType != SearchType.Equals)
                    {
                        if(conditionValue.SearchType == SearchType.StartsWith || conditionValue.SearchType == SearchType.EndsWith || conditionValue.SearchType == SearchType.Contains)
                        {
                            var column = m_dataTable.GetColumn(condition);

                            var caseSensitive = column.GetXProperty<bool?>(CoreDataTable.StringIndexCaseSensitiveXProp) ?? true;
                            
                            if (column.HasIndex == false || caseSensitive != conditionValue.CaseSensitive)
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    validCount++;

                    if (conditionValue.SearchType != SearchType.Equals)
                    {
                        break;
                    }

                    if (conditionValue.Values.Count != 1)
                    {
                        break;
                    }

                    if (conditionValue.CaseSensitive == false)
                    {
                        break;
                    }

                    multiIndexValidCount++;
                }

                if (conditionValues.Count > 0)
                {
                    bool simpleIndexed = false;
                    int indexMapping = -1;

                    if (validCount == conditionValues.Count)
                    {
                        var column = m_dataTable.DataColumnInfo.ColumnMappings[condition];

                        if (m_dataTable.IndexInfo.IndexMappings.TryGetValue(column.ColumnHandle, out indexMapping))
                        {
                            simpleIndexed = true;
                        }
                    }

                    if (simpleIndexed)
                    {
                        indexUseCondition.Add((condition, indexMapping));
                    }
                    else
                    {
                        if (multiIndexValidCount == 1 && m_dataTable.MultiColumnIndexInfo.HasAny)
                        {
                            var column = m_dataTable.DataColumnInfo.ColumnMappings[condition];

                            var columnFirstIndex = m_dataTable.MultiColumnIndexInfo.GetColumnFirstIndex(column.ColumnHandle);

                            if (columnFirstIndex >= 0)
                            {
                                if (multiIndexUseCondition.TryGetValue(columnFirstIndex, out var conditions) == false)
                                {
                                    multiIndexUseCondition[columnFirstIndex] = conditions = new Data<string>();
                                }

                                conditions.Add(condition);

                                continue;
                            }
                        }

                        otherConditions.Add(condition);
                    }
                }
            }
        }

        [NotNull]
        private IEnumerable<DataRow> GetRowsBySingleColumnIndex(
            int indexMapping,
            IComparable value,
            SearchType conditionValueSearchType)
        {
            if (conditionValueSearchType == SearchType.Equals)
            {
                foreach (var row in m_dataTable.GetRowsBySimpleIndex<DataRow, IComparable>(indexMapping, value))
                {
                    yield return row;
                }
            }
            else
            {
                var str = (string)value;
                
                CoreDataTable.StringIndexLookupType lookupType;

                switch (conditionValueSearchType)
                {
                    case SearchType.StartsWith:
                        lookupType = CoreDataTable.StringIndexLookupType.StartsWith;
                        break;
                    case SearchType.Contains:
                        lookupType = CoreDataTable.StringIndexLookupType.Contains;
                        break;
                    case SearchType.EndsWith:
                        lookupType = CoreDataTable.StringIndexLookupType.EndsWith;
                        break;
                    default: 
                        lookupType = CoreDataTable.StringIndexLookupType.Equals;
                        break;
                }

                foreach (var row in m_dataTable.GetRowsByStringIndex<DataRow>(indexMapping, str, lookupType))
                {
                    yield return row;
                }
            }
        }

        private static IEnumerable<DataRow> Filter(IEnumerable<DataRow> rows, DataColumn column, ConditionValue value)
        {
            var trueOrFalse = !(value.Negate);

            if (value.Null || value.Empty || value.Values.Count == 0)
            {
                foreach (var row in rows)
                {
                    if (row.IsNull(column) == trueOrFalse)
                    {
                        yield return row;
                    }
                }

                yield break;
            }

            var valuesFirst = value.Values.First;
            
            var isNullCompare = TypeConvertor.CanBeTreatedAsNullBoxed(column.Type, column.TypeModifier, valuesFirst);

            if (isNullCompare)
            {
                valuesFirst = default;
            }
            
            switch (value.SearchType)
            {
                case SearchType.Greater:
                {
                    foreach (var row in rows)
                    {
                        if (row.IsNotNull(column) && row.Field<IComparable>(column)?.CompareTo(valuesFirst) > 0 == trueOrFalse)
                        {
                            yield return row;
                        }
                    }

                    break;
                }

                case SearchType.GreaterOrEquals:
                {
                    foreach (var row in rows)
                    {
                        if (row.IsNotNull(column) && row.Field<IComparable>(column)?.CompareTo(valuesFirst) >= 0 == trueOrFalse)
                        {
                            yield return row;
                        }
                    }

                    break;
                }

                case SearchType.Lesser:
                {
                    foreach (var row in rows)
                    {
                        if (row.IsNotNull(column) && row.Field<IComparable>(column)?.CompareTo(valuesFirst) < 0 == trueOrFalse)
                        {
                            yield return row;
                        }
                    }

                    break;
                }

                case SearchType.LesserOrEquals:
                {
                    foreach (var row in rows)
                    {
                        if (row.IsNotNull(column) && row.Field<IComparable>(column)?.CompareTo(valuesFirst) <= 0 == trueOrFalse)
                        {
                            yield return row;
                        }
                    }

                    break;
                }

                case SearchType.In:
                {
                    foreach (var row in rows)
                    {
                        var rowValue = row.Field<IComparable>(column);

                        var any = rowValue != null && value.Values.Contains(rowValue);

                        if (any == trueOrFalse)
                        {
                            yield return row;
                        }
                    }


                    break;
                }

                case SearchType.Equals:
                {
                    if (!(value.CaseSensitive))
                    {
                        var valueValue = GetStringValue(value);

                        foreach (var row in rows)
                        {
                            if (string.Equals(row.ToString(column), valueValue, StringComparison.CurrentCultureIgnoreCase) == trueOrFalse)
                            {
                                yield return row;
                            }
                        }
                    }
                    else
                    {
                        if (isNullCompare)
                        {
                            foreach (var row in rows)
                            {
                                if (row.IsNull(column) == trueOrFalse)
                                {
                                    yield return row;
                                }
                            }
                        }
                        else
                        {
                            foreach (var row in rows)
                            {
                                if (Equals(row[column], valuesFirst) == trueOrFalse)
                                {
                                    yield return row;
                                }
                            }
                        }
                    }

                    break;
                }

                case SearchType.Contains:
                {
                    if (!(value.CaseSensitive))
                    {
                        var lower = GetStringValue(value).ToLower();

                        if (string.IsNullOrEmpty(lower) == false)
                        {
                            foreach (var row in rows)
                            {
                                if (row.ToString(column).ToLower().Contains(lower) == trueOrFalse)
                                {
                                    yield return row;
                                }
                            }
                        }
                    }
                    else
                    {
                        var stringValue = GetStringValue(value);

                        if (string.IsNullOrEmpty(stringValue) == false)
                        {
                            foreach (var row in rows)
                            {
                                if (row.ToString(column).Contains(stringValue) == trueOrFalse)
                                {
                                    yield return row;
                                }
                            }
                        }
                    }

                    break;
                }

                case SearchType.StartsWith:
                {
                    var stringValue = GetStringValue(value);

                    var stringComparison = StringComparison.CurrentCultureIgnoreCase;

                    if (value.CaseSensitive)
                    {
                        stringComparison = StringComparison.CurrentCulture;
                    }

                    if (string.IsNullOrEmpty(stringValue) == false)
                    {
                        foreach (var row in rows)
                        {
                            if (row.ToString(column).StartsWith(stringValue, stringComparison) == trueOrFalse)
                            {
                                yield return row;
                            }
                        }
                    }

                    break;
                }

                case SearchType.EndsWith:
                {
                    var stringValue = GetStringValue(value);

                    var stringComparison = StringComparison.CurrentCultureIgnoreCase;

                    if (value.CaseSensitive)
                    {
                        stringComparison = StringComparison.CurrentCulture;
                    }

                    if (string.IsNullOrEmpty(stringValue) == false)
                    {
                        foreach (var row in rows)
                        {
                            if (row.ToString(column).EndsWith(stringValue, stringComparison) == trueOrFalse)
                            {
                                yield return row;
                            }
                        }
                    }

                    break;
                }
            }
        }

        private static string GetStringValue(ConditionValue value)
        {
            var valuesFirst = value.Values.First;

            string stringValue;

            if (valuesFirst is string s)
            {
                stringValue = s;
            }
            else
            {
                stringValue = valuesFirst?.ToString();
            }

            return stringValue;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
