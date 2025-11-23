using System;
using Brudixy.Interfaces;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class JoinTests
    {
        [TestFixture]
        public class CommonTest
        {
            private const int rowCount = 1000;

            private const int initialCapacity = 101;

            private DataTable m_table1;

            private DataTable m_table2;

            [SetUp]
            public void Setup()
            {
                var table1 = new DataTable();

                table1.Name = "t_nmt";

                table1.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
                table1.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
                table1.AddColumn(Fields.Name, TableStorageType.String);
                table1.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
                table1.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
                table1.AddColumn(Fields.parentid, TableStorageType.Int32);
                table1.AddColumn(Fields.groupid, TableStorageType.Int32);
                table1.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
                table1.AddColumn(Fields.Directories.Code, TableStorageType.String);

                table1.AddIndex(Fields.id);

                table1.Capacity = initialCapacity;

                var dataEdit = table1.StartTransaction();

                for (int i = rowCount; i > 0; i--)
                {
                    var id = i;

                    var value = new object[] { id, i, "Name " + id, DateTime.Now, DateTime.Now, rowCount - i + 1, i == rowCount ? 500 : -i, Guid.NewGuid(), "Code " + id % 50 };

                    table1.ImportRow(RowState.Added, value);
                }

                dataEdit.Commit();

                var table2 = new DataTable();

                table2.Name = "t_nmtitem";

                table2.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
                table2.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
                table2.AddColumn(Fields.Name, TableStorageType.String);
                table2.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
                table2.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
                table2.AddColumn(Fields.parentid, TableStorageType.Int32);
                table2.AddColumn(Fields.groupid, TableStorageType.Int32);
                table2.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
                table2.AddColumn("nmtid", TableStorageType.Int32);
                table2.AddColumn("qnt", TableStorageType.Int32);

                table2.AddIndex(Fields.id);

                table2.Capacity = initialCapacity;

                var dataEdit2 = table2.StartTransaction();

                for (int i = rowCount; i > 0; i--)
                {
                    var id = i;

                    var value = new object[] { id, i, "Name " + id, DateTime.Now, DateTime.Now, -i, i == rowCount ? 500 : 0, Guid.NewGuid(), rowCount - id + 1, id % 20 };

                    table2.ImportRow(RowState.Added, value);
                }

                dataEdit2.Commit();

                m_table1 = table1;
                m_table2 = table2;
            }

            [TearDown]
            public void TearDown()
            {
                m_table1.Dispose();
            }

            [Test]
            public void TestLeftJoin()
            {
                var dataTable = CoreDataTable.LeftJoin<DataTable>(
                    "nmtid",
                    "id",
                    m_table2,
                    m_table1,
                    new[] {"nmtid", "name", "qnt" },
                    new[] { "sn", "id", "name" });

                foreach (var row in dataTable.Rows)
                {
                    Assert.AreEqual(row.Field<int?>("t1.nmtid"), row.Field<int?>("t2.id"));

                    Assert.NotNull(row.Field<int?>("t1.qnt"));
                    Assert.Greater(row.Field<int>("t2.sn"), 0);
                    Assert.NotNull(row.Field<string>("t2.name"));
                }

                Assert.Greater(dataTable.RowCount, 0);
            }

            [Test]
            public void TestRightJoin()
            {
                var dataTable = DataTable.LeftJoin<DataTable>(
                    "id",
                    "nmtid",
                    m_table1,
                    m_table2,
                    new[] { "sn", "id", "name" },
                    new[] { "nmtid", "name", "qnt" }
                    );

                Assert.Greater(dataTable.RowCount, 0);

                foreach (var row in dataTable.Rows)
                {
                    Assert.AreEqual(row.Field<int?>("t2.nmtid"), row.Field<int?>("t1.id"));

                    Assert.NotNull(row.Field<int?>("t2.qnt"));
                    Assert.Greater(row.Field<int>("t1.sn"), 0);
                    Assert.NotNull(row.Field<string>("t1.name"));
                }
            }

            [Test]
            public void TestLeftJoinSameTable()
            {
                var dataTable = DataTable.LeftJoin<DataTable>(
                    "parentid",
                    "id",
                    m_table1,
                    m_table1,
                    new[] { "sn", "id" },
                    new[] { "parentid", "name"});

                Assert.Greater(dataTable.RowCount, 0);

                foreach (var row in dataTable.Rows)
                {
                    Assert.AreEqual(row.Field<int?>("t2.parentid"), row.Field<int?>("t1.id"));

                    Assert.Greater(row.Field<int>("t1.sn"), 0);
                    Assert.NotNull(row.Field<string>("t2.name"));
                }

                Assert.Greater(dataTable.RowCount, 0);
            }

            [Test]
            public void TestInnerJoinAllRows()
            {
                var dataTable = DataTable.InnerJoin<DataTable>(
                    "id",
                    "id",
                    m_table1,
                    m_table2,
                    new[] { "sn", "id","name" },
                    new[] { "nmtid", "name", "qnt", "id" });

                Assert.Greater(dataTable.RowCount, 0);

                foreach (var row in dataTable.Rows)
                {
                    Assert.AreEqual(row.Field<int?>("t1.id"), row.Field<int?>("t2.id"));

                    Assert.Greater(row.Field<int>("t1.sn"), 0);
                    Assert.NotNull(row.Field<string>("t2.name"));
                }

            }


            [Test]
            public void TestInnerJoinEmpty()
            {
                var dataTable = DataTable.InnerJoin<DataTable>(
                    "guid",
                    "guid",
                    m_table1,
                    m_table2,
                    new[] { "sn", "id", "name" },
                    new[] { "nmtid", "name", "qnt","id" }
                    );

                Assert.AreEqual(dataTable.RowCount, 0);
            }

            [Test]
            public void TestInnerJoinSingle()
            {
                var dataTable = DataTable.InnerJoin<DataTable>(
                    "groupid",
                    "groupid",
                    m_table1,
                    m_table2,
                    new[] { "sn", "id", "name" },
                    new[] { "nmtid", "name","qnt", "id" }
                    );

                Assert.AreEqual(dataTable.RowCount, 1);
            }
        }
    }
}
