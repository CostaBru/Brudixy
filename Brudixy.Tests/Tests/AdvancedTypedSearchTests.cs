using System;
using System.Linq;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class AdvancedTypedSearchTests
    {
        private readonly bool m_uniqueKey;
        private const int rowCount = 1000;
        private const int initialCapacity = 101;

        private SuperTable dataTable;
        
        private class SuperTable : DataTable
        {
            protected override CoreDataRow CreateRowInstance()
            {
                return new SuperTableRow();
            }
        }

        private class SuperTableRow : DataRow
        {
        }
        

        public AdvancedTypedSearchTests(bool uniqueKey)
        {
            m_uniqueKey = uniqueKey;
        }
        
        [SetUp]
        public void Setup()
        {
            var table = new SuperTable();

            table.Name = "t_nmt";

            FillTable(table);

            dataTable = table;
        }

        private void FillTable(DataTable table)
        {
            table.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            table.AddColumn(Fields.Name, TableStorageType.String);
            table.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmId, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String);
            table.AddColumn("Expr1", TableStorageType.String, dataExpression: "name + ' ' + id");
            table.AddColumn("Default", TableStorageType.String, defaultValue: "test4312");
            table.AddColumn("MI1", TableStorageType.Int32);
            table.AddColumn("MI2", TableStorageType.Int32);
            table.AddColumn("MI3", TableStorageType.Int32);

            table.AddIndex(Fields.id, unique: m_uniqueKey);

            table.AddMultiColumnIndex(new []{"MI1", "MI2", "MI3"}, unique: m_uniqueKey);
            
            table.Capacity = initialCapacity;

            var dataEdit = table.StartTransaction();

            var dateTime = DateTime.Now.Date;

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var values = new Map<string, object>()
                {
                    { Fields.id, id },
                    { Fields.Sn, i },
                    { Fields.Name, "Name " + id },
                    { Fields.Directories.CreateDt, DateTime.Now },
                    { Fields.Directories.LmDt, DateTime.Now },
                    { Fields.Directories.LmId, -i },
                    { Fields.groupid, -i },
                    { Fields.Directories.Guid, Guid.NewGuid() },
                    { "MI1", dateTime.Year },
                    { "MI2", dateTime.Month },
                    { "MI3", dateTime.Day },
                };

                var newRow = table.NewRow(values);
                
                var importRow = table.ImportRow(newRow);

                Assert.True(importRow.IsAddedRow);

                dateTime += TimeSpan.FromDays(1);
            }

            dataEdit.Commit();
        }

        [TearDown]
        public void TearDown()
        {
            dataTable?.Dispose();
        }

        [Test]
        public void TestEmpty()
        {
            var table = new DataTable();

            Assert.IsEmpty(table.RowsOfType<DataRow>()
                .Where("").Equals("")
                .Where("").AsString().Contains("")
                .Where("").AsString().StartsWith("")
                .Where("").AsString().EndsWith("")
                .Where("").AsString().Not().Null()
                .Where("").AsString().Equals("")
                .Where("").Greater("")
                .Where("").GreaterOrEquals("")
                .Where("").Lesser("")
                .Where("").LesserOrEquals("")
                .Where("").In("")
                .Where("").In("", "")
                .Where("").In("", "", "")
                .Where("").In("", "", "", "")
                .Where("").In("", "", "", "", ""));
        }

        [Test]
        public void TestMultiKeyIndex()
        {
            var dateTime = DateTime.Now.Date + TimeSpan.FromDays(63);
            
            Data<SuperTableRow> rows;
            Data<SuperTableRow> expectedRows;

            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where("MI1")
                .Equals(dateTime.Year)
                .Where("MI2")
                .Equals(dateTime.Month)
                .Where("MI3")
                .Equals(dateTime.Day)
                .OrderBy(Fields.id)
                .ToData();
            
            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<int>("MI1") == dateTime.Year && r.Field<int>("MI2") == dateTime.Month && r.Field<int>("MI3") == dateTime.Day)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
        }

        [Test]
        public void TestCompare()
        {
            Data<SuperTableRow> rows;
            Data<SuperTableRow> expectedRows;

            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Sn).Greater(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<int>(Fields.Sn) > 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Sn).GreaterOrEquals(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<int>(Fields.Sn) >= 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Sn).Lesser(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<int>(Fields.Sn) < 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Sn).LesserOrEquals(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<int>(Fields.Sn) <= 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Sn).Not().LesserOrEquals(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => (r.Field<int>(Fields.Sn) <= 5) == false)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
        }
        
        [Test]
        public void TestInClauseUsingIndex()
        {
            Data<SuperTableRow> rows;
            DataRow row;
            Data<SuperTableRow> expectedRows;

            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).In(1)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .WhereFieldValue(Fields.id, 1)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).In(1, 5)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(2, rows.Count);

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .WhereFieldValueIn(Fields.id, 1, 5)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).In(1, 5, 7)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(3, rows.Count);

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .WhereFieldValueIn(Fields.id, 1, 5, 7)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).In(1, 5, 7, 11)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(4, rows.Count);

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .WhereFieldValueIn(Fields.id, 1, 5, 7, 11)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).In(1, 5, 7, 11, 13)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(5, rows.Count);

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .WhereFieldValueIn(Fields.id, 1, 5, 7, 11, 13)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));

            var keys = new Set<int>() { 1, 5, 7, 11, 13 };
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).In(keys)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(5, rows.Count);

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => keys.Contains(r.FieldNotNull<int>(Fields.id)))
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).Not().In(keys)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => keys.Contains(r.FieldNotNull<int>(Fields.id)) == false)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
        }
        
        [Test]
        public void TestStringClauses()
        {
            Data<SuperTableRow> rows;
            DataRow row;
            Data<SuperTableRow> expectedRows;

            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().StartsWith("Name")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Contains("Name")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);

            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<string>(Fields.Name).StartsWith("Name"))
                .Where(r => r.Field<int>(Fields.Sn) == 1)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));

            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().EndsWith(" 1")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            expectedRows = dataTable
                .RowsOfType<SuperTableRow>()
                .Where(r => r.Field<string>(Fields.Name).EndsWith(" 1"))
                .Where(r => r.Field<int>(Fields.Sn) == 1)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));

            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().EndsWith(" 1")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Equals(null)
                .ToData();

            Assert.AreEqual(0, rows.Count);
            
            rows = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Not().Equals(null)
                .ToData();

            Assert.AreEqual(dataTable.RowCount, rows.Count);

            var copy = dataTable.Copy();

            copy.Rows.First().SetNull(Fields.Name);
            
            rows = copy.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Equals(string.Empty)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = copy.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Not().Equals(string.Empty)
                .ToData();
            
            Assert.AreEqual(copy.RowCount - 1, rows.Count);
            
            rows = copy.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Null()
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = copy.RowsOfType<SuperTableRow>()
                .Where(Fields.Name).AsString().Not().Null()
                .ToData();
            
            Assert.AreEqual(copy.RowCount - 1, rows.Count);
        }

        [Test]
        public void TestEqualsIndexSearch()
        {
            SuperTableRow row;

            row = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).Equals(5)
                .Where(Fields.Sn).Equals(new int?(1))
                .FirstOrDefault();

            Assert.Null(row);

            row = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).Equals(5)
                .FirstOrDefault();

            Assert.AreEqual(row,  dataTable.Rows.FirstOrDefault(r => r.Field<int>(Fields.id) == 5));

            row = dataTable.RowsOfType<SuperTableRow>()
                .Where(Fields.id).Equals(5)
                .Where(Fields.Sn).Equals(5)
                .FirstOrDefault();

            Assert.AreEqual(row,  dataTable.Rows.FirstOrDefault(r => r.Field<int>(Fields.id) == 5 && r.Field<int>(Fields.Sn) == 5));
        }
    }
}