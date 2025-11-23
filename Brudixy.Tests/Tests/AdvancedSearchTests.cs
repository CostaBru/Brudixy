using System;
using System.Linq;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class AdvancedSearchTests
    {
        private readonly bool m_uniqueKey;
        private const int rowCount = 1000;
        private const int initialCapacity = 101;

        private DataTable dataTable;

        public AdvancedSearchTests(bool uniqueKey)
        {
            m_uniqueKey = uniqueKey;
        }
        
        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

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
            table.AddIndex(Fields.Name);

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
        public void TestMultiKeyIndex()
        {
            var dateTime = DateTime.Now.Date + TimeSpan.FromDays(63);
            
            Data<DataRow> rows;
            Data<DataRow> expectedRows;

            rows = dataTable.Rows
                .Where("MI1")
                .Equals(dateTime.Year)
                .Where("MI2")
                .Equals(dateTime.Month)
                .Where("MI3")
                .Equals(dateTime.Day)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<int>("MI1") == dateTime.Year && r.Field<int>("MI2") == dateTime.Month && r.Field<int>("MI3") == dateTime.Day)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
        }

        [Test]
        public void TestCompare()
        {
            Data<DataRow> rows;
            Data<DataRow> expectedRows;

            rows = dataTable.Rows
                .Where(Fields.Sn).Greater(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<int>(Fields.Sn) > 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.Sn).GreaterOrEquals(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<int>(Fields.Sn) >= 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.Sn).Lesser(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<int>(Fields.Sn) < 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.Sn).LesserOrEquals(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<int>(Fields.Sn) <= 5)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.Sn).Not().LesserOrEquals(5)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => (r.Field<int>(Fields.Sn) <= 5) == false)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
        }
        
        [Test]
        public void TestInClauseUsingIndex()
        {
            Data<DataRow> rows;
            DataRow row;
            Data<DataRow> expectedRows;

            rows = dataTable.Rows
                .Where(Fields.id).In(1)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            expectedRows = dataTable
                .Rows
                .WhereFieldValue(Fields.id, 1)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.id).In(1, 5)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(2, rows.Count);

            expectedRows = dataTable
                .Rows
                .WhereFieldValueIn(Fields.id, 1, 5)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.id).In(1, 5, 7)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(3, rows.Count);

            expectedRows = dataTable
                .Rows
                .WhereFieldValueIn(Fields.id, 1, 5, 7)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.id).In(1, 5, 7, 11)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(4, rows.Count);

            expectedRows = dataTable
                .Rows
                .WhereFieldValueIn(Fields.id, 1, 5, 7, 11)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.id).In(1, 5, 7, 11, 13)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(5, rows.Count);

            expectedRows = dataTable
                .Rows
                .WhereFieldValueIn(Fields.id, 1, 5, 7, 11, 13)
                .OrderBy(Fields.id)
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));

            var keys = new Set<int>() { 1, 5, 7, 11, 13 };
            
            rows = dataTable.Rows
                .Where(Fields.id).In(keys)
                .OrderBy(Fields.id)
                .ToData();

            Assert.AreEqual(5, rows.Count);

            expectedRows = dataTable
                .Rows
                .Where(r => keys.Contains(r.FieldNotNull<int>(Fields.id)))
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
            
            rows = dataTable.Rows
                .Where(Fields.id).Not().In(keys)
                .OrderBy(Fields.id)
                .ToData();

            expectedRows = dataTable
                .Rows
                .Where(r => keys.Contains(r.FieldNotNull<int>(Fields.id)) == false)
                .OrderBy(Fields.id)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));
        }
        
        [Test]
        public void TestStringClauses()
        {
            Data<DataRow> rows;
            DataRow row;
            Data<DataRow> expectedRows;

            rows = dataTable.Rows
                .Where(Fields.Name).AsString().StartsWith("Name")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = dataTable.Rows
                .Where(Fields.Name).AsString().Contains("Name")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);

            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<string>(Fields.Name).StartsWith("Name"))
                .Where(r => r.Field<int>(Fields.Sn) == 1)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));

            rows = dataTable.Rows
                .Where(Fields.Name).AsString().EndsWith(" 1")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            expectedRows = dataTable
                .Rows
                .Where(r => r.Field<string>(Fields.Name).EndsWith(" 1"))
                .Where(r => r.Field<int>(Fields.Sn) == 1)
                .ToData();
            
            Assert.True(expectedRows.SequenceEqual(rows));

            rows = dataTable.Rows
                .Where(Fields.Name).AsString().EndsWith(" 1")
                .Where(Fields.Sn).Equals(1)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = dataTable.Rows
                .Where(Fields.Name).AsString().Equals(null)
                .ToData();

            Assert.AreEqual(0, rows.Count);
            
            rows = dataTable.Rows
                .Where(Fields.Name).AsString().Not().Equals(null)
                .ToData();

            Assert.AreEqual(dataTable.RowCount, rows.Count);

            var copy = dataTable.Copy();

            copy.Rows.First().SetNull(Fields.Name);
            
            rows = copy.Rows
                .Where(Fields.Name).AsString().Equals(string.Empty)
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = copy.Rows
                .Where(Fields.Name).AsString().Not().Equals(string.Empty)
                .ToData();
            
            Assert.AreEqual(copy.RowCount - 1, rows.Count);
            
            rows = copy.Rows
                .Where(Fields.Name).AsString().Null()
                .ToData();

            Assert.AreEqual(1, rows.Count);
            
            rows = copy.Rows
                .Where(Fields.Name).AsString().Not().Null()
                .ToData();
            
            Assert.AreEqual(copy.RowCount - 1, rows.Count);
        }

        [Test]
        public void TestEqualsIndexSearch()
        {
            DataRow row;

            row = dataTable.Rows
                .Where(Fields.id).Equals(5)
                .Where(Fields.Sn).Equals(new int?(1))
                .FirstOrDefault();

            Assert.Null(row);

            row = dataTable.Rows
                .Where(Fields.id).Equals(5)
                .FirstOrDefault();

            Assert.AreEqual(row,  dataTable.Rows.FirstOrDefault(r => r.Field<int>(Fields.id) == 5));

            row = dataTable.Rows
                .Where(Fields.id).Equals(5)
                .Where(Fields.Sn).Equals(5)
                .FirstOrDefault();

            Assert.AreEqual(row,  dataTable.Rows.FirstOrDefault(r => r.Field<int>(Fields.id) == 5 && r.Field<int>(Fields.Sn) == 5));
        }

        [Test]
        public void TestFullTextSearch()
        {
            var table = new DataTable();
            table.Name = "fulltext_table";
            // base columns
            table.AddColumn("id", TableStorageType.Int32, unique: true);
            table.AddColumn("Name", TableStorageType.String);
            // enable full text on Name BEFORE adding index
            var nameCol = table.GetColumns().First(c => c.ColumnName == "Name");
            nameCol.SetXProperty(CoreDataTable.StringIndexFullTextXProp, true);
            table.AddIndex("id", unique: true);
            table.AddIndex("Name");

            var names = new[]
            {
                "Quick brown fox",
                "Brown fox jumps",
                "Lazy dog sleeps",
                "Another quick example",
                "Fulltext indexing test",
                "Brown sugar",
                "Quickly done",
                "Text full Brown Quick",
                "No match here",
                "brown lower case",
                "Brown sugar" // duplicate to test multiple matches
            };

            var tx = table.StartTransaction();
            for (int i = 0; i < names.Length; i++)
            {
                var values = new Map<string, object>
                {
                    { "id", i + 1 },
                    { "Name", names[i] },
                };
                var newRow = table.NewRow(values);
                var importRow = table.ImportRow(newRow);
                Assert.True(importRow.IsAddedRow);
            }
            tx.Commit();

            // Contains 'brown' (case insensitive) should match all rows containing brown regardless of case
            var rows = table.Rows
                .Where("Name").AsString().Contains("brown")
                .OrderBy("id")
                .ToData();

            var expectedRows = table.Rows
                .Where(r => r.Field<string>("Name").IndexOf("brown", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy("id")
                .ToData();

            Assert.True(expectedRows.SequenceEqual(rows));
            Assert.AreEqual(expectedRows.Count, rows.Count);
            Assert.Greater(rows.Count, 0);

            // StartsWith 'Quick'
            rows = table.Rows
                .Where("Name").AsString().StartsWith("Quick")
                .OrderBy("id")
                .ToData();

            expectedRows = table.Rows
                .Where(r => r.Field<string>("Name").StartsWith("Quick", StringComparison.OrdinalIgnoreCase))
                .OrderBy("id")
                .ToData();
            Assert.True(expectedRows.SequenceEqual(rows));

            // EndsWith 'here'
            rows = table.Rows
                .Where("Name").AsString().EndsWith("here")
                .OrderBy("id")
                .ToData();

            expectedRows = table.Rows
                .Where(r => r.Field<string>("Name").EndsWith("here", StringComparison.OrdinalIgnoreCase))
                .OrderBy("id")
                .ToData();
            Assert.True(expectedRows.SequenceEqual(rows));
            Assert.AreEqual(1, rows.Count);

            // Search for token not present should yield zero rows
            rows = table.Rows
                .Where("Name").AsString().Contains("unfindabletoken")
                .ToData();
            Assert.AreEqual(0, rows.Count);

            table.Dispose();
        }
    }
}