using System;
using System.Linq;
using Brudixy.Interfaces;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class ColumnExpressionTests
    {
        private const int rowCount = 2;

        private const int initialCapacity = 101;

        private DataTable dataTable;

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            table.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            table.AddColumn(Fields.Name, TableStorageType.String);
            table.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmId, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String);
            table.AddColumn("NameCode", TableStorageType.String, dataExpression: "Name + Code");
            table.AddColumn("IdNeg", TableStorageType.Int32, dataExpression: "-id");
            table.AddColumn("Id200000", TableStorageType.Int32, dataExpression: "id + 200000");

            table.AddIndex(Fields.id);

            table.Capacity = initialCapacity;

            var dataEdit = table.StartTransaction();

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var value = new object[]
                {
                    id, i, "Name " + id, DateTime.Now, DateTime.Now, -i, -i, Guid.NewGuid(), "Code " + id % 50
                };

                table.ImportRow(RowState.Added, value);
            }

            dataEdit.Commit();

            dataTable = table;
        }

        [TearDown]
        public void TearDown()
        {
            dataTable.Dispose();
        }

        [Test]
        public void TestNameCodeExpression()
        {
            var table = dataTable.Copy();

            var row = table.Rows.First();

            var expression1 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression1);

            var expression2 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression2);

            Assert.AreEqual(expression1, expression2);

            row.Set("Name", "555");

            var expression3 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression3);

            Assert.AreNotEqual(expression1, expression3);

            var expression4 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression4);

            Assert.AreEqual(expression4, expression3);

            var dataEdit = table.StartTransaction();

            row.Set("Name", "1111");

            var expression5 = row.Field<string>("NameCode");
            var expression6 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression5);
            Assert.IsNotEmpty(expression6);

            Assert.AreEqual(expression5, expression6);

            dataEdit.Commit();

            var expression7 = row.Field<string>("NameCode");
            var expression8 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression7);
            Assert.IsNotEmpty(expression8);

            Assert.AreEqual(expression7, expression8);

            Assert.AreNotEqual(expression4, expression7);
        }

        [Test]
        public void TestNameCodeExpressionAfterCopy()
        {
            var table = dataTable;

            var row = table.Rows.First();

            var expression1 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression1);

            var expression2 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression2);

            Assert.AreEqual(expression1, expression2);

            row.Set("Name", "555");

            var expression3 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression3);

            Assert.AreNotEqual(expression1, expression3);

            var expression4 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression4);

            Assert.AreEqual(expression4, expression3);

            var dataEdit = table.StartTransaction();

            row.Set("Name", "1111");

            var expression5 = row.Field<string>("NameCode");
            var expression6 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression5);
            Assert.IsNotEmpty(expression6);

            Assert.AreEqual(expression5, expression6);

            dataEdit.Commit();

            var expression7 = row.Field<string>("NameCode");
            var expression8 = row.Field<string>("NameCode");

            Assert.IsNotEmpty(expression7);
            Assert.IsNotEmpty(expression8);

            Assert.AreEqual(expression7, expression8);

            Assert.AreNotEqual(expression4, expression7);

            var copy = table.Copy();

            var copyRow = copy.Rows.First();

            var expression9 = copyRow.Field<string>("NameCode");

            Assert.IsNotEmpty(expression9);

            Assert.AreEqual(expression8, expression9);
        }
    }
}