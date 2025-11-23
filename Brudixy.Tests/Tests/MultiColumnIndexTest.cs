using System;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class MultiColumnIndexTest
    {
        private const int rowCount = 1000;
        private const int initialCapacity = 101;

        private DataTable dataTable;

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            FillTable(table);

            dataTable = table;
        }

        private const string Year = "Year";
        private const string Month = "Month";
        private const string Day = "Day";

        private static void FillTable(DataTable table)
        {
            Setup(table);

            var dataEdit = table.BeginLoad();

            AddRow(table, new DateTime(2018, 1, 1));
            AddRow(table, new DateTime(2018, 1, 2));
            AddRow(table, new DateTime(2018, 1, 3));
            AddRow(table, new DateTime(2018, 1, 4));

            AddRow(table, new DateTime(2018, 2, 1));
            AddRow(table, new DateTime(2018, 2, 2));
            AddRow(table, new DateTime(2018, 2, 3));
            AddRow(table, new DateTime(2018, 2, 4));

            AddRow(table, new DateTime(2018, 3, 1));
            AddRow(table, new DateTime(2018, 3, 2));
            AddRow(table, new DateTime(2018, 3, 3));
            AddRow(table, new DateTime(2018, 3, 4));

            dataEdit.EndLoad();
        }

        private static void Setup(DataTable table)
        {
            table.AddColumn(Year, TableStorageType.Int32);
            table.AddColumn(Month, TableStorageType.Int32);
            table.AddColumn(Day, TableStorageType.Int32);
            table.AddColumn(Fields.Name, TableStorageType.String);
            table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table.AddColumn("Expr1", TableStorageType.String, dataExpression: "Year + ' ' + Month");
            table.AddColumn("Default", TableStorageType.String, defaultValue: "test4312");
            table.AddColumn(Year + 1, TableStorageType.Int32);
            table.AddColumn(Month + 1, TableStorageType.Int32);
            table.AddColumn(Day + 1, TableStorageType.Int32);

            table.SetPrimaryKeyColumns(new[] { Year, Month, Day });

            table.AddMultiColumnIndex(new[] { Year + 1, Month + 1, Day + 1 });

            table.Capacity = initialCapacity;
        }

        private static void AddRow(DataTable table, DateTime time)
        {
            var newRow = table.NewRow();

            newRow[Year] = time.Year;
            newRow[Month] = time.Month;
            newRow[Day] = time.Day;
            newRow[Fields.Name] = "Name " + time.ToShortDateString();
            newRow[Fields.Directories.Guid] = Guid.NewGuid();
            newRow[Year + 1] = time.Year;
            newRow[Month + 1] = time.Month;
            newRow[Day + 1] = time.Day;

            Assert.True(table.AddRow(newRow).IsAddedRow);
        }

        [TearDown]
        public void TearDown()
        {
            dataTable.Dispose();
        }

        [Test]
        public void TestLookingFor()
        {
            var copy = dataTable.Copy();
            
            Assert.True(dataTable.MultiColumnIndexInfo.HasAny);
           
            TestDate(copy, 2018, 1, 1);
            TestDate(copy, 2018, 2, 2);
            TestDate(copy, 2018, 3, 3);
            
            var expRow = copy.Select<DataRow>("Year = 2018 and Month = 1 and Day = 4").First();

            var row = new DataExtractor<IDataTableRow>(copy)
                .Where(Year).Equals(2018)
                .Where(Month).Equals(1)
                .Where(Day).Equals(4)
                .First();
            
            Assert.True(row.Equals(expRow));
            
            expRow = copy.Rows.ApplyFilterExpression("Year = 2018 and Month = 1 and Day = 4").First();
            
            Assert.True(row.Equals(expRow));

            row.Set(Year, 2015);

            TestDate(copy, row.Field<int>(Year), row.Field<int>(Month), row.Field<int>(Day));
        }

        [Test]
        public void TestUnique()
        {
            var copy = dataTable.Copy();
            
            var row = new DataExtractor<IDataTableRow>(copy)
                .Where(Year).Equals(2018)
                .Where(Month).Equals(1)
                .Where(Day).Equals(4)
                .First();

            var day = row.Field<int>(Day);

            Assert.Throws<ConstraintException>(() => row.Set(Day, 1));

            Assert.AreEqual(day, row.Field<int>(Day));
            
            Assert.True(dataTable.MultiColumnIndexInfo.HasAnyUnique);
            Assert.True(copy.MultiColumnIndexInfo.HasAnyUnique);

            Assert.Throws<ConstraintException>(() => AddRow(copy, new DateTime(2018, 1, 1)));
            Assert.False(copy.GetIsInTransaction());
            Assert.Throws<ConstraintException>(() => AddRow(copy, new DateTime(2018, 2, 2)));
            Assert.False(copy.GetIsInTransaction());
            Assert.Throws<ConstraintException>(() => AddRow(copy, new DateTime(2018, 3, 3)));
            Assert.False(copy.GetIsInTransaction());
            
            Assert.Throws<ConstraintException>(() => row.Set(Day, 1));
        }

        [Test]
        public void TestUniqueSingleRow()
        {
            var table = new DataTable(){Name = "Test"};

            Setup(table);

            var dataEdit = table.BeginLoad();

            AddRow(table, new DateTime(2018, 1, 1));
            AddRow(table, new DateTime(2018, 1, 4));
            
            dataEdit.EndLoad();
            
            var row = new DataExtractor<IDataTableRow>(table)
                .Where(Year).Equals(2018)
                .Where(Month).Equals(1)
                .Where(Day).Equals(4)
                .First();
            
            Assert.Throws<ConstraintException>(() => row.Set(Day, 1));
            Assert.Throws<ConstraintException>(() => AddRow(table, new DateTime(2018, 1, 1)));
            Assert.False(table.GetIsInTransaction());
            Assert.Throws<ConstraintException>(() => row.Set(Day, 1));
        }

        private static void TestDate(DataTable copy, int y, int m, int d)
        {
            var tableRows = new DataExtractor<IDataTableRow>(copy)
                .Where(Year).Equals(y)
                .Where(Month).Equals(m)
                .Where(Day).Equals(d)
                .ToData();

            var row = tableRows
                .FirstOrDefault();

            Assert.NotNull(row);

            Assert.AreEqual(1, tableRows.Count);

            tableRows = new DataExtractor<IDataTableRow>(copy)
                .Where(Year).Equals(y)
                .Where(Month).Equals(m)
                .ToData();

            var list = copy.Rows.Where(r => r.Field<int>(Year) == y && r.Field<int>(Month) == m).ToData();

            Assert.AreEqual(list.Count, tableRows.Count);

            tableRows = new DataExtractor<IDataTableRow>(copy)
                .Where(Year).Equals(y)
                .ToData();

            list = copy.Rows.Where(r => r.Field<int>(Year) == y).ToData();

            Assert.AreEqual(list.Count, tableRows.Count);
        }
    }
}

