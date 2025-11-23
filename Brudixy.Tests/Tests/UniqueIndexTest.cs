using System;
using System.Linq;
using Brudixy.Converter;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture(TestIndex.DefineOnInit)]
    [TestFixture(TestIndex.DefineAfterFill)]
    public class UniqueIndexTest
    {
        private readonly TestIndex m_testIndex;

        public UniqueIndexTest(TestIndex testIndex)
        {
            m_testIndex = testIndex;
        }
        
        private void FillTable(DataTable table, Map<Type, Set<object>> indexValues)
        {
            var types = new Data<TableStorageType>();
            var typeModifiers = new Data<TableStorageTypeModifier>() { TableStorageTypeModifier.Simple};

            var list1 = new Data<string>();

            foreach (var tm in typeModifiers)
            {
                foreach (TableStorageType value in Enum.GetValues(typeof(TableStorageType)))
                {
                    if (IndexesTests.SkipIndex(value, tm) || value is TableStorageType.Boolean)
                    {
                        continue;
                    }

                    table.AddColumn(value + "." + tm, value, tm);

                    if (m_testIndex == TestIndex.DefineOnInit)
                    {
                        table.AddIndex(value.ToString(), true);
                    }

                    types.Add(value);
                }
            }

            var random = new Random(100);
            
            var factory = IndexesTests.GetFactory(random);

            var loadState = table.BeginLoad();

            for (int i = 0; i < 10; i++)
            {
                var newRow = table.NewRow();

                FillRow(indexValues, types, typeModifiers, factory, newRow, list1, i);

                table.AddRow(newRow);
            }
            
            loadState.EndLoad();
            
            var list2 = new Data<string>();

            var dataEditTransaction = table.StartTransaction();

            for (int i = 10; i < 30; i++)
            {
                var newRow = table.NewRow();

                FillRow(indexValues, types, typeModifiers, factory, newRow, list2, i);

                table.AddRow(newRow);
            }
            
            dataEditTransaction.Commit();

            if (m_testIndex == TestIndex.DefineAfterFill)
            {
                foreach (var tm in typeModifiers)
                {
                    foreach (TableStorageType value in Enum.GetValues(typeof(TableStorageType)))
                    {
                        if (IndexesTests.SkipIndex(value, tm))
                        {
                            continue;
                        }

                        table.AddIndex(value + "." + tm, true);
                    }
                }
            }
        }
        
        public enum TestType
        {
            StartTran,
            AsIs
        }
        
        [Test]
        public void TestAllTypes([Values(TestType.AsIs, TestType.StartTran)] TestType testType)
        {
            var dataTable = new DataTable();

            var indexValues = new Map<Type, Set<object>>();
            
            var typeModifiers = new Data<TableStorageTypeModifier>() { TableStorageTypeModifier.Simple};
            
            FillTable(dataTable, indexValues);
            
            TestIndexLeadToData(indexValues, dataTable);

            var copy = dataTable.Copy();

            var equals = dataTable.EqualsExt(copy);
            
            Assert.True(equals.value, equals.name + "." + equals.type);

            TestIndexLeadToData(indexValues, copy);

            var clone = dataTable.Clone();
            
            clone.MergeDataOnly(copy);
            
            TestIndexLeadToData(indexValues, clone);
            
            var random = new Random(100);
            
            var factory = IndexesTests.GetFactory(random);

            var types = dataTable.GetColumns().Select(c => c.Type).ToData();

            var list1 = new Data<string>();

            int i = dataTable.RowCount;
            
            var changedIndexValues = new Map<Type, Set<object>>();

            IDataEditTransaction transaction = null;

            if (testType == TestType.StartTran)
            {
                transaction =dataTable.StartTransaction();
            }
            
            foreach (var row in dataTable.Rows.ToData())
            {
                var newRow = dataTable.NewRow();

                FillRow(changedIndexValues, types, typeModifiers, factory, newRow, list1, i);

                row.CopyFrom(newRow);
                
                i++;
            }
            
            transaction?.Commit();
            
            TestIndexLeadToData(changedIndexValues, dataTable);
            
            var changedIndexValuesTyped = new Map<Type, Set<object>>();

            ChangeTableTypedCode(dataTable, changedIndexValuesTyped, types, typeModifiers, factory, list1, i, changedIndexValues);
            
            TestIndexLeadToData(changedIndexValuesTyped, dataTable);
            
            dataTable.Dispose();
        }

        [Test]
        public void TestSingleKeyRelation()
        {
            var t1 = new DataTable();

            t1.Name = "T1";

            var indexValues = new Map<Type, Set<object>>();
            
            FillTable(t1, indexValues);

            var t2 = t1.Copy();

            t2.Name = "T2";

            var dataSet = new DataTable();
            
            dataSet.EnforceConstraints = true;

            dataSet.AddTable(t1);
            dataSet.AddTable(t2);

            foreach (var dataColumn in t1.GetColumns())
            {
                var t2Column = t2.GetColumn(dataColumn.ColumnName);
                
                dataSet.AddRelation(
                    "SK_" + dataColumn.ColumnName,
                    dataColumn,
                    t2Column,
                    RelationType.OneToOne,
                    Rule.Cascade,
                    Rule.Cascade,
                    AcceptRejectRule.Cascade);
            }

            var checkConstraints = dataSet.CheckConstraints();
            
            Assert.True(checkConstraints);
        }

        private static void ChangeTableTypedCode(DataTable dataTable,
            Map<Type, Set<object>> changedIndexValuesTyped, Data<TableStorageType> types,
            Data<TableStorageTypeModifier> typeModifiers,
            Map<Type, Func<object>> factory,
            Data<string> list1,
            int i,
            Map<Type, Set<object>> changedIndexValues)
        {
            foreach (var row in dataTable.Rows.ToData())
            {
                var newRow = dataTable.NewRow();

                FillRow(changedIndexValuesTyped, types, typeModifiers, factory, newRow, list1, i, changedIndexValues);

                foreach (var typeModifier in typeModifiers)
                {

                    foreach (var columnType in types)
                    {
                        var columnName = columnType + "." + typeModifier;

                        switch (columnType)
                        {
                            case TableStorageType.Byte:
                                SetRow<byte>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Char:
                                SetRow<char>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Decimal:
                                SetRow<decimal>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Double:
                                SetRow<double>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Guid:
                                SetRow<Guid>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Int16:
                                SetRow<Int16>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Int32:
                                SetRow<Int32>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Int64:
                                SetRow<Int64>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.Single:
                                SetRow<Single>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.DateTime:
                                SetRow<DateTime>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.SByte:
                                SetRow<SByte>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.TimeSpan:
                                SetRow<TimeSpan>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.UInt16:
                                SetRow<UInt16>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.UInt32:
                                SetRow<UInt32>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.UInt64:
                                SetRow<UInt64>(row, columnName, newRow[columnName]);
                                break;
                            case TableStorageType.DateTimeOffset:
                                SetRow<DateTimeOffset>(row, columnName, newRow[columnName]);
                                break;

                            case TableStorageType.String:
                                SetRow2<String>(row, columnName, (string)newRow[columnName]);
                                break;
                        }
                    }
                }

                i++;
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
        
        internal static void FillRow(Map<Type, Set<object>> indexValues, Data<TableStorageType> types, Data<TableStorageTypeModifier> typeModifiers, Map<Type, Func<object>> factory, DataRowContainer newRow, Data<string> list1, int i, Map<Type, Set<object>> existingValues =null)
        {
            for (int j = 0; j < 1; j++)
            {
                var allowNull = false;

                foreach (var modifier in typeModifiers)
                {
                    foreach (var type in types)
                    {
                        var columnType = DataTable.GetColumnType(type, modifier, allowNull, null);

                        if (columnType == null)
                        {
                            continue;
                        }
                        
                        var columnOrXProp = type + "." + modifier;

                        if (factory.ContainsKey(columnType))
                        {
                            var objects = indexValues.GetOrAdd(columnType, () => new Set<object>());
                            var existing = existingValues?.GetOrAdd(columnType, () => new Set<object>()) ?? new Set<object>();

                            object val = factory[columnType]();

                            while (objects.Contains(val) || existing.Contains(val))
                            {
                                val = factory[columnType]();
                            }

                            newRow[columnOrXProp] = val;

                            objects.Add(val);

                            if (type == TableStorageType.String)
                            {
                                list1.Add((string)val);
                            }
                        }
                        else
                        {
                            var o = Tool.ConvertBoxed(i, columnType);

                            newRow[columnOrXProp] = o;

                            indexValues.GetOrAdd(columnType, () => new Set<object>()).Add(o);
                        }
                    }
                }
            }
        }
        
        internal static void TestIndexLeadToData(Map<Type, Set<object>> indexValues, DataTable dataTable, bool lead = true)
        {
            foreach (var kv in indexValues)
            {
                var columnType = DataTable.GetColumnType(kv.Key);

                foreach (var val in kv.Value)
                {
                    var t = columnType.type;

                    var column = t + "." + columnType.typeModifier;
                    
                    var dataRow = dataTable.GetRow(column, (IComparable)val);

                    if (lead)
                    {
                        Assert.NotNull(dataRow, val.ToString());
                    }
                    else
                    {
                        Assert.Null(dataRow, $"{columnType}: Found, but not expected : " + val.ToString());
                    }

                    switch (t)
                    {
                        case TableStorageType.Byte: CheckGetRow<byte>(dataTable, column, val); break;
                        case TableStorageType.Char: CheckGetRow<char>(dataTable, column, val); break;
                        case TableStorageType.Decimal: CheckGetRow<decimal>(dataTable, column, val); break;
                        case TableStorageType.Double: CheckGetRow<double>(dataTable, column, val); break;
                        case TableStorageType.Guid: CheckGetRow<Guid>(dataTable, column, val); break;
                        case TableStorageType.Int16: CheckGetRow<Int16>(dataTable, column, val); break;
                        case TableStorageType.Int32: CheckGetRow<Int32>(dataTable, column, val); break;
                        case TableStorageType.Int64: CheckGetRow<Int64>(dataTable, column, val); break;
                        case TableStorageType.Single: CheckGetRow<Single>(dataTable, column, val); break;
                        case TableStorageType.DateTime: CheckGetRow<DateTime>(dataTable, column, val); break;
                        case TableStorageType.SByte: CheckGetRow<SByte>(dataTable, column, val); break;
                        case TableStorageType.TimeSpan: CheckGetRow<TimeSpan>(dataTable, column, val); break;
                        case TableStorageType.UInt16: CheckGetRow<UInt16>(dataTable, column, val); break;
                        case TableStorageType.UInt32: CheckGetRow<UInt32>(dataTable, column, val); break;
                        case TableStorageType.UInt64: CheckGetRow<UInt64>(dataTable, column, val); break;
                        case TableStorageType.DateTimeOffset: CheckGetRow<DateTimeOffset>(dataTable, column, val); break;
                        
                        case TableStorageType.String: CheckGetRow2<String>(dataTable, column, val); break;

                    }
                }
            }
        }

        private static void CheckGetRow<T>(DataTable dataTable, string columnName, object val) where T: struct, IComparable
        {
            var comparable = (T)val;
            
            var dataRow = dataTable.GetRow(columnName, ref comparable);

            Assert.NotNull(dataRow, val.ToString());
        }
        
        private static void CheckGetRow2<T>(DataTable dataTable, string columnName, object val) where T: IComparable
        {
            var dataRow = dataTable.GetRow(columnName, (T)val);

            Assert.NotNull(dataRow, val.ToString());
        }
    }
}