using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using CommandLine;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    public enum TestIndex
    {
        DefineOnInit,
        DefineAfterFill
    }

    public static class MoreLinq
    {
        public static T MaxBy<T, R>(this IEnumerable<T> en, Func<T, R> evaluate, Func<R, R, int> comparer) where R : IComparable<R> {
            return en.Select(t => new Tuple<T, R>(t, evaluate(t)))
                .Aggregate((max, next) => comparer(next.Item2, max.Item2) > 0 ? next : max).Item1;
        }

        public static T MinBy<T, R>(this IEnumerable<T> en, Func<T, R> evaluate, Func<R, R, int> comparer) where R : IComparable<R> {
            return en.Select(t => new Tuple<T, R>(t, evaluate(t)))
                .Aggregate((max, next) => comparer(next.Item2, max.Item2) < 0 ? next : max).Item1;
        }
        
        public static double StdDev(this IEnumerable<double> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count  > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }
    }
    
    [TestFixture(TestIndex.DefineOnInit)]
    [TestFixture(TestIndex.DefineAfterFill)]
    public class IndexesTests
    {
        private readonly TestIndex m_testIndex;

        public IndexesTests(TestIndex testIndex)
        {
            m_testIndex = testIndex;
        }
        
        private void FillTable(DataTable table, Map<Type, Data<object>> indexValues)
        {
            var types = new Data<TableStorageType>();

            var tableStorageTypeModifiers = new Data<TableStorageTypeModifier>() { TableStorageTypeModifier.Simple };

            foreach (var modifier in tableStorageTypeModifiers)
            {
                foreach (TableStorageType value in Enum.GetValues<TableStorageType>())
                {
                    if (SkipIndex(value, modifier))
                    {
                        continue;
                    }

                    /*if (value != TableStorageType.Boolean)
                    {
                        continue;
                    }*/

                    var columnName = value + "." + modifier;
                    
                    table.AddColumn(columnName, value, modifier);

                    if (m_testIndex == TestIndex.DefineOnInit)
                    {
                        table.AddIndex(columnName);
                    }

                    types.Add(value);
                }
            }

            var random = new Random(100);
            
            var factory = GetFactory(random);

            var loadState = table.BeginLoad();

            for (int i = 0; i < 10; i++)
            {
                var newRow = table.NewRow();

                FillRow(indexValues, factory, newRow, i);

                table.AddRow(newRow);
            }
            
            loadState.EndLoad();
            
            var dataEditTransaction = table.StartTransaction();

            for (int i = 0; i < 20; i++)
            {
                var newRow = table.NewRow();

                FillRow(indexValues, factory, newRow, i);

                table.AddRow(newRow);
            }
            
            dataEditTransaction.Commit();

            if (m_testIndex == TestIndex.DefineAfterFill)
            {
                foreach (var modifier in tableStorageTypeModifiers)
                {
                    foreach (TableStorageType value in Enum.GetValues(typeof(TableStorageType)))
                    {
                        if (SkipIndex(value, modifier))
                        {
                            continue;
                        }
                        
                        var columnName = value + "." + modifier;

                        table.AddIndex(columnName);
                    }
                }
            }
        }

        internal static Map<Type, Func<object>> GetFactory(Random random)
        {
            var factory = new Map<Type, Func<object>>()
            {
                {
                    typeof(string), () =>
                    {
                        var bytes = new byte[16];
                        random.NextBytes(bytes);
                        return Convert.ToBase64String(bytes);
                    }
                },
                { typeof(DateTime), () => DateTime.Now.AddDays((random.NextDouble() - 0.5) * 100) },
                { typeof(DateTimeOffset), () => DateTimeOffset.Now.AddDays((random.NextDouble() - 0.5) * 100) },
                { typeof(TimeSpan), () => TimeSpan.FromMilliseconds((random.NextDouble() - 0.5) * 100) },
                { typeof(Guid), () => Guid.NewGuid() },
                { typeof(Char), () => (char)random.Next(0, char.MaxValue) },
                { typeof(SByte), () => (sbyte)random.Next(0, sbyte.MaxValue) },
                { typeof(Byte), () => (byte)random.Next(0, byte.MaxValue) },
                { typeof(bool), () => random.Next(0, 1) == 1 },
            };
            return factory;
        }

        internal static bool SkipIndex(TableStorageType value, TableStorageTypeModifier modifier)
        {
            if (modifier != TableStorageTypeModifier.Simple)
            {
                return true;
            }
            
            return value is 
                TableStorageType.Char or 
                TableStorageType.Empty or 
                TableStorageType.Uri or 
                TableStorageType.Json or 
                TableStorageType.Xml or 
                TableStorageType.Object or 
                TableStorageType.Type or 
                TableStorageType.BigInteger or
                TableStorageType.UserType;
        }

        internal static void FillRow(Map<Type, Data<object>> indexValues, 
            Map<Type, Func<object>> factory, DataRowContainer newRow,
            int i)
        {
            foreach (var column in newRow.Columns)
            {
                var columnType = column.StorageType;

                var type = column.AllowNull ? Nullable.GetUnderlyingType(columnType) ?? columnType : columnType;
                
                if (factory.ContainsKey(type))
                {
                    var o = factory[type]();

                    newRow[column] = o;

                    indexValues.GetOrAdd(columnType, () => new Data<object>()).Add(o);
                }
                else
                {
                    var o = Convert.ChangeType(i, type);

                    newRow[column] = o;

                    indexValues.GetOrAdd(columnType, () => new Data<object>()).Add(o);
                }
            }
        }

        public enum TestType
        {
            StartTran,
            AsIs
        }

        [Test]
        public void TestAllTypes1([Values(TestType.AsIs, TestType.StartTran)] TestType testType)
        {
            var dataTable = new DataTable();

            var indexValues = new Map<Type, Data<object>>();

            FillTable(dataTable, indexValues);

            TestIndexLeadToData(indexValues, dataTable);
        }

        [Test]
        public void TestAllTypes2([Values(TestType.AsIs, TestType.StartTran)] TestType testType)
        {
            var dataTable = new DataTable();

            var indexValues = new Map<Type, Data<object>>();

            FillTable(dataTable, indexValues);

            var clone = dataTable.Clone();

            clone.MergeDataOnly(dataTable);

            TestIndexLeadToData(indexValues, clone);
        }

        [Test]
        public void TestAllTypes3([Values(TestType.AsIs, TestType.StartTran)] TestType testType)
        {
            var dataTable = new DataTable();
            var indexValues = new Map<Type, Data<object>>();
            var modifiers = new Data<TableStorageTypeModifier>() { TableStorageTypeModifier.Simple };

            FillTable(dataTable, indexValues);

            var random = new Random(100);
            var factory = GetFactory(random);
            var types = dataTable.GetColumns().Select(c => c.Type).ToData();
            var changedIndexValues = new Map<Type, Data<object>>();
            int i = dataTable.RowCount;

            IDataEditTransaction transaction = null;

            if (testType == TestType.StartTran)
            {
                transaction = dataTable.StartTransaction();
            }

            foreach (var row in dataTable.Rows.ToData())
            {
                var newRow = dataTable.NewRow();

                FillRow(changedIndexValues, factory, newRow, i);

                var column = newRow.Columns.First();

                column.SetXProperty("W", 1);

                newRow.SetColumnError(column.ColumnName, "Error");
                newRow.SetColumnInfo(column.ColumnName, "Info");
                newRow.SetColumnWarning(column.ColumnName, "Warning");

                newRow.SetXProperty("Test", true);

                row.CopyFrom(newRow);

                Assert.AreEqual("Error", row.GetCellError(column.ColumnName));
                Assert.AreEqual("Info", row.GetCellInfo(column.ColumnName));
                Assert.AreEqual("Warning", row.GetCellWarning(column.ColumnName));
                Assert.AreEqual(true, row.GetXProperty<bool>("Test"));

                i++;
            }

            transaction?.Commit();

            TestIndexLeadToData(changedIndexValues, dataTable);
        }

        [Test]
        public void TestAllTypes4([Values(TestType.AsIs, TestType.StartTran)] TestType testType)
        {
            var dataTable = new DataTable();
            var indexValues = new Map<Type, Data<object>>();

            FillTable(dataTable, indexValues);

            var changedIndexValuesTyped = new Map<Type, Data<object>>();
            var random = new Random(100);
            var factory = GetFactory(random);
            int i = dataTable.RowCount;

            MutateTableTypedCode(dataTable, changedIndexValuesTyped, factory, i);

            TestIndexLeadToData(changedIndexValuesTyped, dataTable);
        }

        [Test]
        public void TestAllTypes5([Values(TestType.AsIs, TestType.StartTran)] TestType testType)
        {
            var dataTable = new DataTable();
            var indexValues = new Map<Type, Data<object>>();

            FillTable(dataTable, indexValues);

            var changedIndexValuesTyped = new Map<Type, Data<object>>();
            var random = new Random(100);
            var factory = GetFactory(random);
            int i = dataTable.RowCount;

            MutateTableTypedCode(dataTable, changedIndexValuesTyped, factory, i);

            TestIndexLeadToData(changedIndexValuesTyped, dataTable);

            var clone = dataTable.Clone();

            clone.MergeDataOnly(dataTable);

            TestIndexLeadToData(changedIndexValuesTyped, clone);

            var copy = dataTable.Copy();

            TestIndexLeadToData(changedIndexValuesTyped, copy);
        }

        [Test]
        public void TestIndexUpdatedDuringTransaction()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(Fields.Name);
            
            dataTable.AddIndex(Fields.Name);

            var dataRow = dataTable.AddRow(_.MapObj((Fields.Name, "1")));

            var transaction = dataTable.StartTransaction();

            dataRow.Set(Fields.Name, "2");

            var row = dataTable.GetRow(Fields.Name, "2");
            
            Assert.NotNull(row);

            transaction.Rollback();
        }
        
        [Test]
        public void TestAllTypes5()
        {
            var dataTable = new DataTable();
            var indexValues = new Map<Type, Data<object>>();

            FillTable(dataTable, indexValues);

            var copy = dataTable.Copy();
            
            var changedIndexValuesTypedAfterClean = new Map<Type, Data<object>>();
            var random = new Random(100);
            var factory = GetFactory(random);
            int i = dataTable.RowCount;
            
            //todo rework index set inside transaction
            //var transaction = copy.StartTransaction();

            foreach (var row in copy.Rows)
            {
                foreach (var dataColumn in copy.GetColumns())
                {
                    row.SetNull(dataColumn);
                }
            }
            
            AssertGetRowsByNull(copy);

            MutateTableTypedCode(copy, changedIndexValuesTypedAfterClean, factory, i);
            
           // transaction.Commit();
            
            TestIndexLeadToData(changedIndexValuesTypedAfterClean, copy);
        
            TestCheckAllKeys(copy);
        }

        private static void TestCheckAllKeys(DataTable table)
        {
            var indexes = table.IndexInfo.Indexes;

            foreach (var index in indexes)
            {
                var indexStorage = index.ReadyIndex.Copy();

                Assert.AreEqual(indexStorage.Count, index.ReadyIndex.Count);
                
                var checkAllKeys = indexStorage.CheckAllKeys(index.ReadyIndex, (r) => true, (r) => true).ToSet();

                Assert.AreEqual(0, checkAllKeys.Count);

                var grouping = indexStorage.GetComparableKeyValues().Where(k => k.hasValue).GroupBy(g => g.key).ToData();

                var key = grouping[0].Key;

                foreach (var kv in grouping[0])
                {
                    indexStorage.Remove(key, kv.reference);
                }

                checkAllKeys = indexStorage.CheckAllKeys(index.ReadyIndex, (r) => true, (r) => true).ToSet();

                foreach (var kv in grouping[0])
                {
                    Assert.True(checkAllKeys.Contains(kv.reference), $"Missing ref {kv.reference} key {kv.key}");
                }
            }
        }

        private static void TestMinMaxes(Data<TableStorageType> types, DataTable table)
        {
            foreach (var type in types)
            {
                IComparable max;
                IComparable min;

                switch (type)
                {
                    case TableStorageType.Byte:
                        max = table.Rows.SelectFieldValue<byte>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<byte>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Char:
                        max = table.Rows.SelectFieldValue<char>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<char>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Decimal:
                        max = table.Rows.SelectFieldValue<decimal>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<decimal>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Double:
                        max = table.Rows.SelectFieldValue<double>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<double>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Int16:
                        max = table.Rows.SelectFieldValue<Int16>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<Int16>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Int32:
                        max = table.Rows.SelectFieldValue<Int32>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<Int32>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Int64:
                        max = table.Rows.SelectFieldValue<Int64>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<Int64>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.Single:
                        max = table.Rows.SelectFieldValue<Single>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<Single>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.DateTime:
                        max = table.Rows.SelectFieldValue<DateTime>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<DateTime>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.SByte:
                        max = table.Rows.SelectFieldValue<SByte>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<SByte>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.TimeSpan:
                        max = table.Rows.SelectFieldValue<TimeSpan>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<TimeSpan>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.UInt16:
                        max = table.Rows.SelectFieldValue<UInt16>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<UInt16>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.UInt32:
                        max = table.Rows.SelectFieldValue<UInt32>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<UInt32>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.UInt64:
                        max = table.Rows.SelectFieldValue<UInt64>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<UInt64>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;
                    case TableStorageType.DateTimeOffset:
                        max = table.Rows.SelectFieldValue<DateTimeOffset>(type.ToString()).Max();
                        min = table.Rows.SelectFieldValue<DateTimeOffset>(type.ToString()).Min();

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;

                    /*case TableStorageType.String:
                        max = table.Rows.SelectFieldValue<string>(type.ToString())
                            .MaxBy(s => s, (s1, s2) => StringComparer.Ordinal.Compare(s1, s2));
                        min = table.Rows.SelectFieldValue<string>(type.ToString())
                            .MinBy(s => s, (s1, s2) => StringComparer.Ordinal.Compare(s1, s2));

                        Assert.AreEqual(max, table.Max(type.ToString()));
                        Assert.AreEqual(min, table.Min(type.ToString()));
                        break;*/
                }
            }
        }


        private static void MutateTableTypedCode(DataTable dataTable,
            Map<Type, Data<object>> changedIndexValuesTyped,
            Map<Type, Func<object>> factory,
            int i)
        {
            foreach (var row in dataTable.Rows.ToData())
            {
                var newRow = dataTable.NewRow();

                FillRow(changedIndexValuesTyped, factory, newRow, i);

                foreach (var column in dataTable.GetColumns())
                {
                    var columnOrXProp = column.ColumnName;

                    switch (column.Type)
                    {
                        case TableStorageType.Boolean:
                            var val = (bool)newRow[columnOrXProp];
                            SetRow<bool>(row, columnOrXProp, val);
                            break;
                        case TableStorageType.Byte:
                            SetRow<byte>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Char:
                            SetRow<char>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Decimal:
                            SetRow<decimal>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Double:
                            SetRow<double>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Guid:
                            SetRow<Guid>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Int16:
                            SetRow<Int16>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Int32:
                            SetRow<Int32>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Int64:
                            SetRow<Int64>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.Single:
                            SetRow<Single>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.DateTime:
                            SetRow<DateTime>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.SByte:
                            SetRow<SByte>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.TimeSpan:
                            SetRow<TimeSpan>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.UInt16:
                            SetRow<UInt16>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.UInt32:
                            SetRow<UInt32>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.UInt64:
                            SetRow<UInt64>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;
                        case TableStorageType.DateTimeOffset:
                            SetRow<DateTimeOffset>(row, columnOrXProp, newRow[columnOrXProp]);
                            break;

                        case TableStorageType.String:
                            SetRow2<String>(row, columnOrXProp, (string)newRow[columnOrXProp]);
                            break;
                    }
                }

                i++;
            }
        }

        private static void AssertGetRowsByNull(DataTable dataTable)
        {
            foreach (var column in dataTable.GetColumns())
            {
                var rows = dataTable.GetRows(column, (IComparable)null).ToData();

                Assert.AreEqual(dataTable.RowCount, rows.Count, column.ColumnName);
            }
        }

        private static void SetRow<T>(DataRow row, string columnName, object val) where T: struct, IComparable, IComparable<T>
        {
            var comparable = (T)val;

            row.Set(columnName, comparable);
        }
        
        private static void SetRow2<T>(DataRow row, string columnName, string val) 
        {
            row.SetField(columnName, val);
        }

        internal static void TestIndexLeadToData(Map<Type, Data<object>> indexValues, DataTable dataTable)
        {
            foreach (var kv in indexValues)
            {
                var columnType = DataTable.GetColumnType(kv.Key);

                foreach (var val in kv.Value)
                {
                    var dataRow = dataTable.GetRow(columnType.type + "." + columnType.typeModifier, (IComparable)val);

                    Assert.NotNull(dataRow, val.ToString());
                }
            }
        }
    }
}