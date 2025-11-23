using System;
using System.Linq;
using System.Linq.Expressions;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable HeapView.ObjectAllocation

namespace Brudixy.Tests
{
    [TestFixture]
    public class RelationTests
    {
        private const int rowCount = 1000;

        private const int initialCapacity = 101;

        private const string c_fkNmtNmtitem = "FK_nmt_nmtitem";

        private DataTable m_nmt;
        private DataTable m_nmtitem;

        private DataTable m_dsNmt;

        [SetUp]
        public void Setup()
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(Rule.Cascade);

            var dataEdit = nmt.StartTransaction();

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var value = new object[] { id, i, "Name " + id, DateTime.Now, DateTime.Now, rowCount - i + 1, i == rowCount ? 500 : -i, Guid.NewGuid(), "Code " + id % 50 };

                nmt.ImportRow(RowState.Added, value);
            }

            dataEdit.Commit();

            var transaction = nmtItem.StartTransaction();

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var value = new object[] { id, i, "Name " + id, DateTime.Now, DateTime.Now, -i, i == rowCount ? 500 : 0, Guid.NewGuid(), rowCount - id + 1, id % 20 };

                nmtItem.ImportRow(RowState.Added, value);
            }

            transaction.Commit();

            m_nmt = nmt;
            m_nmtitem = nmtItem;
            m_dsNmt = dataSet;
        }

        private static (DataTable dataSet, DataTable nmt, DataTable nmtItem) CreateTestDs(Rule constraintUpdateRule)
        {
            var dataSet = new DataTable();

            var nmt = new DataTable(dataSet);

            nmt.Name = "t_nmt";

            nmt.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            nmt.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            nmt.AddColumn(Fields.Name, TableStorageType.String);
            nmt.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            nmt.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            nmt.AddColumn(Fields.parentid, TableStorageType.Int32);
            nmt.AddColumn(Fields.groupid, TableStorageType.Int32);
            nmt.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            nmt.AddColumn(Fields.Directories.Code, TableStorageType.String);
            nmt.AddColumn("Expr1", TableStorageType.String, dataExpression: "Name + '-' + Code");

            nmt.Capacity = initialCapacity;

            var nmtItem = new DataTable(dataSet);

            nmtItem.Name = "t_nmtitem";

            nmtItem.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            nmtItem.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            nmtItem.AddColumn(Fields.Name, TableStorageType.String);
            nmtItem.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            nmtItem.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            nmtItem.AddColumn(Fields.parentid, TableStorageType.Int32);
            nmtItem.AddColumn(Fields.groupid, TableStorageType.Int32);
            nmtItem.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            nmtItem.AddColumn("nmtid", TableStorageType.Int32);
            nmtItem.AddColumn("qnt", TableStorageType.Int32);

            nmtItem.Capacity = initialCapacity;

            dataSet.AddTable(nmt);
            dataSet.AddTable(nmtItem);

            dataSet.AddRelation(c_fkNmtNmtitem,
                nmt.GetColumn("id"),
                nmtItem.GetColumn("nmtid"),
                RelationType.OneToMany,
                constraintUpdateRule,
                constraintUpdateRule,
                AcceptRejectRule.Cascade);
            
            return (dataSet, nmt, nmtItem);
        }

        [TearDown]
        public void TearDown()
        {
            m_dsNmt.Dispose();
        }

        [Test]
        public void TestGetChildRows()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(Fields.id, TableStorageType.Int32);
            dataTable.AddColumn(Fields.parentid, TableStorageType.Int32);
            dataTable.AddColumn(Fields.Name, TableStorageType.String);

            var relation = dataTable.AddNestedRelation("test", Fields.id, Fields.parentid);

            var rootRow =   dataTable.AddRow(_.MapObj((Fields.id, 1), (Fields.Name, "Root")));
            var childRow1 = dataTable.AddRow(_.MapObj((Fields.id, 2), (Fields.Name, "Root.Child1"), (Fields.parentid, 1)));
            var childRow2 = dataTable.AddRow(_.MapObj((Fields.id, 3), (Fields.Name, "Root.Child2"), (Fields.parentid, 1)));
            
            var childRows = rootRow.GetChildRows(relation).ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1));
            Assert.IsTrue(childRows.Contains(childRow2));
            
            childRows = rootRow.GetChildRows((DataRelation)relation).ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1));
            Assert.IsTrue(childRows.Contains(childRow2));
            
            childRows = rootRow.GetChildRows(relation.Name).ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1));
            Assert.IsTrue(childRows.Contains(childRow2));
            
            childRows = rootRow.GetChildRows(Fields.id, Fields.parentid).ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1));
            Assert.IsTrue(childRows.Contains(childRow2));
            
            childRows = rootRow
                .GetChildRows(rootRow.GetColumn(Fields.id), rootRow.GetColumn(Fields.parentid))
                .OfType<DataRow>()
                .ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1));
            Assert.IsTrue(childRows.Contains(childRow2));
            
            childRows = rootRow
                .GetChildRows(dataTable.GetColumn(Fields.id), dataTable.GetColumn(Fields.parentid))
                .ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1));
            Assert.IsTrue(childRows.Contains(childRow2));

            foreach (var childRow in childRows)
            {
                var row = childRow.GetParentRow(relation);
                
                Assert.AreEqual(rootRow, row);
                Assert.IsTrue(childRow.GetParentRows(relation).Contains(rootRow));
                
                Assert.AreEqual(rootRow.RowHandle, childRow.GetParentRowHandle(relation.Name));
                Assert.AreEqual(rootRow.RowHandle, childRow.GetParentRowHandle(relation));
                Assert.AreEqual(rootRow.RowHandle, childRow.GetParentRowHandle((DataRelation)relation));
                
                row = childRow.GetParentRow(relation.Name);
                
                Assert.AreEqual(rootRow, row);
                Assert.IsTrue(childRow.GetParentRows(relation.Name).Contains(rootRow));
                
                row = childRow.GetParentRow((DataRelation)relation);
                
                Assert.AreEqual(rootRow, row);
                Assert.IsTrue(childRow.GetParentRows(relation.Name).Contains(rootRow));
                Assert.IsTrue(childRow.GetParentRows((DataRelation)relation).Contains(rootRow));
                Assert.IsTrue(childRow.GetParentRows(Fields.id, Fields.parentid).Contains(rootRow));
                Assert.IsTrue(childRow.GetParentRows(dataTable.GetColumn(Fields.id), dataTable.GetColumn(Fields.parentid)).Contains(rootRow));
            }
            
            var childRow1_child1 = dataTable.AddRow(_.MapObj((Fields.id, 4), (Fields.Name, "Root.Child1.Child1"), (Fields.parentid, 2)));
            var childRow1_child2 = dataTable.AddRow(_.MapObj((Fields.id, 5), (Fields.Name, "Root.Child1.Child2"), (Fields.parentid, 2)));
            
            childRows = childRow1.GetChildRows(relation).ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow1_child1));
            Assert.IsTrue(childRows.Contains(childRow1_child2));
            
            var childRow2_child1 = dataTable.AddRow(_.MapObj((Fields.id, 6), (Fields.Name, "Root.Child2.Child1"), (Fields.parentid, 3)));
            var childRow2_child2 = dataTable.AddRow(_.MapObj((Fields.id, 7), (Fields.Name, "Root.Child2.Child2"), (Fields.parentid, 3)));

            childRows = childRow2.GetChildRows(relation).ToData();
            
            Assert.AreEqual(2, childRows.Count);
            Assert.IsTrue(childRows.Contains(childRow2_child1));
            Assert.IsTrue(childRows.Contains(childRow2_child2));
            
            childRows = rootRow
                .GetAllChildRows(Fields.id, Fields.parentid, true)
                .OfType<DataRow>()
                .ToData();
            
            Assert.AreEqual(dataTable.RowCount, childRows.Count);
            
            childRows = rootRow
                .GetAllChildRows(Fields.id, Fields.parentid, false)
                .OfType<DataRow>()
                .ToData();
            
            Assert.AreEqual(dataTable.RowCount - 1, childRows.Count);
            
            childRows = rootRow
                .GetAllChildRows(dataTable.GetColumn(Fields.id), dataTable.GetColumn(Fields.parentid), true)
                .OfType<DataRow>()
                .ToData();
            
            Assert.AreEqual(dataTable.RowCount, childRows.Count);
            
            childRows = ((IDataTableRow)rootRow)
                .GetAllChildRows(rootRow.GetColumn(Fields.id), rootRow.GetColumn(Fields.parentid), true)
                .OfType<DataRow>()
                .ToData();
            
            Assert.AreEqual(dataTable.RowCount, childRows.Count);
            
            childRows = rootRow
                .GetAllChildRows(dataTable.GetColumn(Fields.id), dataTable.GetColumn(Fields.parentid), false)
                .OfType<DataRow>()
                .ToData();
            
            Assert.AreEqual(dataTable.RowCount - 1, childRows.Count);

            var bottom = childRows.Last();

            var dataRows = bottom.GetAllParentRows(Fields.id, Fields.parentid, true).ToData();
            
            Assert.AreEqual(3, dataRows.Count);

            Assert.Null(dataRows.Last().GetParentRow(relation));
            
            dataRows = bottom.GetAllParentRows(dataTable.GetColumn(Fields.id), dataTable.GetColumn(Fields.parentid), true).ToData();
            
            Assert.AreEqual(3, dataRows.Count);

            Assert.Null(dataRows.Last().GetParentRow(relation));
            
            dataRows = ((IDataTableRow)bottom).GetAllParentRows(bottom.GetColumn(Fields.id), bottom.GetColumn(Fields.parentid), true)
                .OfType<DataRow>().ToData();
            
            Assert.AreEqual(3, dataRows.Count);

            Assert.Null(dataRows.Last().GetParentRow(relation));
        }

        [Test]
        public void TestChildRows()
        {
            foreach (var row in m_nmt.Rows)
            {
                var id = row.Field<int>("id");

                var childRows = row.GetChildRows(c_fkNmtNmtitem);

                Assert.IsNotEmpty(childRows);

                Assert.True(childRows.All(r => r.GetTableName() == m_nmtitem.Name));

                Assert.True(childRows.All(r => r.Field<int>("nmtid") == id));
            }
        }

        [Test]
        public void TestParentRows()
        {
            foreach (var itemRow in m_nmtitem.Rows)
            {
                var nmtId = itemRow.Field<int>("nmtid");

                var parentRows = itemRow.GetParentRows(c_fkNmtNmtitem).Union(itemRow.GetParentRow(c_fkNmtNmtitem).SingleToArray()).ToArray();

                Assert.IsNotEmpty(parentRows);

                Assert.True(parentRows.All(r => r.GetTableName() == m_nmt.Name));

                Assert.True(parentRows.All(r => r.Field<int>("id") == nmtId));

                var parentNmtRow = m_nmt.GetRow("id", nmtId);

                var relation = m_dsNmt.TryGetRelation(c_fkNmtNmtitem);
                
                itemRow.SetParentRow(relation, null);
                
                Assert.True(itemRow.IsNull("nmtid"));
                
                itemRow.SetParentRow(relation, parentNmtRow);
                
                Assert.AreEqual(nmtId, itemRow.Field<int>("nmtid"));
                
                itemRow.SetParentRow((IDataRelation)relation, null);
                
                Assert.True(itemRow.IsNull("nmtid"));
                
                itemRow.SetParentRow((IDataRelation)relation, parentNmtRow);
                
                Assert.AreEqual(nmtId, itemRow.Field<int>("nmtid"));
            }
        }

        [Test]
        public void TestNested1()
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
            table1.AddColumn("Expr1", TableStorageType.String, dataExpression: "Name + '-' + Code");

            table1.Capacity = initialCapacity;

            var dataEdit = table1.StartTransaction();

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var value = new object[] { id, i, "Name " + id, DateTime.Now, DateTime.Now, rowCount - i + 1, i == rowCount ? 500 : -i, Guid.NewGuid(), "Code " + id % 50 };

                table1.ImportRow(RowState.Added, value);
            }

            dataEdit.Commit();

            table1.AddIndex( "id");
            table1.AddIndex("parentid");

            foreach (var row in table1.Rows)
            {
                var children = table1.GetChildren<DataRow>("id", (IComparable)row["id"]);

                Assert.IsNotEmpty(children);

                var allChildren = table1.GetAllChildrenHandles("id", "parentid", (IComparable)row["id"]);

                Assert.IsNotEmpty(allChildren);

                var parentRows = table1.GetParentRows<DataRow>("id", (IComparable)row["id"]);

                Assert.IsNotEmpty(parentRows);
            }
        }

        [Test]
        public void TestUpdate()
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(Rule.Cascade);

            nmt.EnforceConstraints = true;
            nmtItem.EnforceConstraints = true;
            
            nmt.AddRow(_.MapObj(("id", 1), ("name", "test nmt 1")));
            nmt.AddRow(_.MapObj(("id", 2), ("name", "test nmt 2")));
            
            nmtItem.AddRow(_.MapObj(("id", 1), ("name", "test nmtitem 1"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 2), ("name", "test nmtitem 2"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 3), ("name", "test nmtitem 3"), ("nmtid", 2)));
            nmtItem.AddRow(_.MapObj(("id", 4), ("name", "test nmtitem 4"), ("nmtid", 2)));

            var nmt1 = testUpdate(nmt, 1);
            var nmt2 = testUpdate(nmt, 2);

            nmt1.Set("id", 3);
            
            testUpdate(nmt, 3);
            testUpdate(nmt, 2);
        }

        [Test]
        public void TestDataSetXProps()
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(Rule.Cascade);
            
            nmt.AddRow(_.MapObj(("id", 1), ("name", "test nmt 1")));
            nmt.AddRow(_.MapObj(("id", 2), ("name", "test nmt 2")));
            
            nmtItem.AddRow(_.MapObj(("id", 1), ("name", "test nmtitem 1"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 2), ("name", "test nmtitem 2"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 3), ("name", "test nmtitem 3"), ("nmtid", 2)));
            nmtItem.AddRow(_.MapObj(("id", 4), ("name", "test nmtitem 4"), ("nmtid", 2)));

            var newGuid = Guid.NewGuid();
            
            dataSet.SetXProperty("Test", true);
            dataSet.SetXProperty("Guid", newGuid);
            
            Assert.True(dataSet.HasXProperty("Test"));
            Assert.AreEqual(2, dataSet.XProperties.Count());
            
            Assert.AreEqual(true, dataSet.GetXProperty<bool>("Test"));
            Assert.AreEqual(newGuid, dataSet.GetXProperty<Guid>("Guid"));

            dataSet = dataSet.Copy();
            
            Assert.AreEqual(true, dataSet.GetXProperty<bool>("Test"));
            Assert.AreEqual(newGuid, dataSet.GetXProperty<Guid>("Guid"));

            var xElement = dataSet.ToXml(SerializationMode.Full);
            var jElement = dataSet.ToJson(SerializationMode.Full);

            dataSet = new DataTable();
            
            dataSet.LoadFromXml(xElement);
            
            Assert.AreEqual(true, dataSet.GetXProperty<bool>("Test"));
            Assert.AreEqual(newGuid, dataSet.GetXProperty<Guid>("Guid"));
            
            dataSet = new DataTable();
            
            dataSet.LoadFromJson(jElement);
            
            Assert.AreEqual(true, dataSet.GetXProperty<bool>("Test"));
            Assert.AreEqual(newGuid, dataSet.GetXProperty<Guid>("Guid"));
            
            dataSet = dataSet.Clone();
            
            Assert.AreEqual(true, dataSet.GetXProperty<bool>("Test"));
            Assert.AreEqual(newGuid, dataSet.GetXProperty<Guid>("Guid"));
        }

        [Test]
        public void TestFollowTransaction()
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(Rule.Cascade);

            nmt.EnforceConstraints = true;
            nmtItem.EnforceConstraints = true;

            nmt.AddRow(_.MapObj(("id", 1), ("name", "test nmt 1")));
            nmt.AddRow(_.MapObj(("id", 2), ("name", "test nmt 2")));

            nmtItem.AddRow(_.MapObj(("id", 1), ("name", "test nmtitem 1"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 2), ("name", "test nmtitem 2"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 3), ("name", "test nmtitem 3"), ("nmtid", 2)));
            nmtItem.AddRow(_.MapObj(("id", 4), ("name", "test nmtitem 4"), ("nmtid", 2)));

            var nmt1 = nmt.GetRow("id", 1);

            var transaction = nmt1.StartTransaction();

            nmt1.Set("id", 3);

            transaction.Commit();

            testUpdate(nmt, 3);
        }

        [Test]
        public void TestFollowTransactionRollbackTransaction()
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(Rule.Cascade);

            nmt.EnforceConstraints = true;
            nmtItem.EnforceConstraints = true;

            nmt.AddRow(_.MapObj(("id", 1), ("name", "test nmt 1")));
            nmt.AddRow(_.MapObj(("id", 2), ("name", "test nmt 2")));

            nmtItem.AddRow(_.MapObj(("id", 1), ("name", "test nmtitem 1"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 2), ("name", "test nmtitem 2"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 3), ("name", "test nmtitem 3"), ("nmtid", 2)));
            nmtItem.AddRow(_.MapObj(("id", 4), ("name", "test nmtitem 4"), ("nmtid", 2)));

            var nmt1 = nmt.GetRow("id", 1);

            var transaction = nmt1.StartTransaction();

            nmt1.Set("id", 3);

            transaction.Rollback();

            testUpdate(nmt, 1);
        }

        [Test]
        public void TestDeleteFollowTransaction([Values(true, false)] bool rollBack)
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(Rule.Cascade);

            nmt.EnforceConstraints = true;
            nmtItem.EnforceConstraints = true;
            
            nmt.AddRow(_.MapObj(("id", 1), ("name", "test nmt 1")));
            nmt.AddRow(_.MapObj(("id", 2), ("name", "test nmt 2")));
            
            nmtItem.AddRow(_.MapObj(("id", 1), ("name", "test nmtitem 1"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 2), ("name", "test nmtitem 2"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 3), ("name", "test nmtitem 3"), ("nmtid", 2)));
            nmtItem.AddRow(_.MapObj(("id", 4), ("name", "test nmtitem 4"), ("nmtid", 2)));

            dataSet.AcceptChanges();
            
            var nmt1 = nmt.GetRow("id", 1);

            var transaction = nmt1.StartTransaction();

            try
            {
                nmt1.Delete();

                if (rollBack)
                {
                    throw null;
                }
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
            }

            if (rollBack)
            {
                testUpdate(nmt, 1);
            }
            else
            {
                var deletedRow = nmt.GetRow("id", 1, ignoreDeleted: true);
                
                Assert.Null(deletedRow);

                var nmtItems1 = nmtItem.GetRows("nmtid", 1).ToData();

                Assert.AreEqual(0, nmtItems1.Count);
                
                deletedRow = nmt.GetRow("id", 1, ignoreDeleted: false);
                
                Assert.Null(deletedRow);
            }
          
            testUpdate(nmt, 2);
        }

        private static DataRow testUpdate(DataTable nmt, int id)
        {
            var nmt1 = nmt.GetRow("id", id);

            var nmtItems1 = nmt1.GetChildRows(c_fkNmtNmtitem).ToData();

            Assert.AreEqual(2, nmtItems1.Count);

            Assert.True(nmtItems1.All(r => r.Field<int>("nmtid") == nmt1.Field<int>("id")));

            return (DataRow)nmt1;
        }
        
        [Test]
        public void TestCascadeDeleteAdded()
        {
            var (nmt, nmtItem) = CreateTestData();

            var (nmt1, nmt1Items) = testState(nmt.GetRowBy(1), RowState.Added);
            var (nmt2, nmt2Items) = testState(nmt.GetRowBy(2), RowState.Added);

            nmt1.Delete();
            
            Assert.True(nmt1Items.All(r => r.RowRecordState == RowState.Detached));
            
            testState(nmt.GetRowBy(2), RowState.Added);
        }
        
        [Test]
        public void TestCascadeDelete()
        {
            var (nmt, nmtItem) = CreateTestData();
            
            nmt.AcceptChanges();
            nmtItem.AcceptChanges();

            var (nmt1, nmt1Items) = testState(nmt.GetRowBy(1), RowState.Unchanged);
            var (nmt2, nmt2Items) = testState(nmt.GetRowBy(2), RowState.Unchanged);

            nmt1.Delete();
            
            Assert.True(nmt1Items.All(r => r.RowRecordState == RowState.Deleted));
            
            testState(nmt.GetRowBy(2), RowState.Unchanged);
        }
        
        [Test]
        public void TestCascadeDeleteSetNull()
        {
            var (nmt, nmtItem) = CreateTestData(Rule.SetNull);
            
            nmt.AcceptChanges();
            nmtItem.AcceptChanges();

            var (nmt1, nmt1Items) = testState(nmt.GetRowBy(1), RowState.Unchanged);
            var (nmt2, nmt2Items) = testState(nmt.GetRowBy(2), RowState.Unchanged);

            nmt1.Delete();
            
            Assert.True(nmt1Items.All(r => r.RowRecordState == RowState.Modified));
            Assert.True(nmt1Items.All(r => r.IsNull("nmtid")));
            
            testState(nmt.GetRowBy(2), RowState.Unchanged);
        }
        
        [Test]
        public void TestCascadeDeleteSetDefault()
        {
            var (nmt, nmtItem) = CreateTestData(Rule.SetDefault);
            
            nmt.AcceptChanges();
            nmtItem.AcceptChanges();

            var nmtid = nmtItem.GetColumn("nmtid");

            nmtItem.SetupColumnDefaultValue(nmtid.Ordinal, 2);

            var (nmt1, nmt1Items) = testState(nmt.GetRowBy(1), RowState.Unchanged);
            var (nmt2, nmt2Items) = testState(nmt.GetRowBy(2), RowState.Unchanged);

            nmt1.Delete();
            
            Assert.True(nmt1Items.All(r => r.RowRecordState == RowState.Modified));
            Assert.True(nmt1Items.All(r => r.Field<int>(nmtid) == 2));
            
            var nmtItems2 = nmt2.GetChildRows(c_fkNmtNmtitem).ToData();

            Assert.AreEqual(4, nmtItems2.Count);

            Assert.True(nmtItems2.All(r => r.Field<int>("nmtid") == nmt2.Field<int>("id")));
        }
        
        [Test]
        public void TestCascadeRejectChanges()
        {
            var (nmt, nmtItem) = CreateTestData();
            
            nmt.AcceptChanges();
            nmtItem.AcceptChanges();

            var (nmt1, nmt1Items) = testState(nmt.GetRowBy(1), RowState.Unchanged);

            nmt1.Set(Fields.Directories.Guid, Guid.NewGuid());
            
            Assert.AreEqual(nmt1.RowRecordState, RowState.Modified);

            foreach (var nmt1Item in nmt1Items)
            {
                nmt1Item.Set(Fields.Directories.Guid, Guid.NewGuid());
                
                Assert.AreEqual(nmt1Item.RowRecordState, RowState.Modified);
            }

            var (nmt2, nmt2Items) = testState(nmt.GetRowBy(2), RowState.Unchanged);

            nmt.RejectChanges();
            
            Assert.AreEqual(nmt1.RowRecordState, RowState.Unchanged);
            Assert.True(nmt1.IsNull(Fields.Directories.Guid));
            
            Assert.True(nmt1Items.All(r => r.RowRecordState == RowState.Unchanged));
            Assert.True(nmt1Items.All(r => r.IsNull(Fields.Directories.Guid)));
            
            testState(nmt.GetRowBy(2), RowState.Unchanged);
        }

        private static (DataTable nmt, DataTable nmtItem) CreateTestData(Rule constraintUpdateRule = Rule.Cascade)
        {
            var (dataSet, nmt, nmtItem) = CreateTestDs(constraintUpdateRule);

            nmt.EnforceConstraints = true;
            nmtItem.EnforceConstraints = true;

            nmt.AddRow(_.MapObj(("id", 1), ("name", "test nmt 1")));
            nmt.AddRow(_.MapObj(("id", 2), ("name", "test nmt 2")));

            nmtItem.AddRow(_.MapObj(("id", 1), ("name", "test nmtitem 1"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 2), ("name", "test nmtitem 2"), ("nmtid", 1)));
            nmtItem.AddRow(_.MapObj(("id", 3), ("name", "test nmtitem 3"), ("nmtid", 2)));
            nmtItem.AddRow(_.MapObj(("id", 4), ("name", "test nmtitem 4"), ("nmtid", 2)));
            return (nmt, nmtItem);
        }

        private static (IDataTableRow row, IDataTableRow[] childs) testState(IDataTableRow nmt1, RowState state)
        {
            var nmtItems1 = nmt1.GetChildRows(c_fkNmtNmtitem).ToData();

            Assert.AreEqual(2, nmtItems1.Count);

            Assert.True(nmtItems1.All(r => r.Field<int>("nmtid") == nmt1.Field<int>("id")));
            Assert.True(nmtItems1.All(r => r.RowRecordState == state));

            return (nmt1, nmtItems1.ToArray());
        }

        [Test]
        public void TestNested2()
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
            table1.AddColumn("Expr1", TableStorageType.String, dataExpression: "Name + '-' + Code");

            table1.Capacity = initialCapacity;

            var dataEdit = table1.StartTransaction();

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var value = new object[] { id, i, "Name " + id, DateTime.Now, DateTime.Now, rowCount - i + 1, i == rowCount ? 500 : -i, Guid.NewGuid(), "Code " + id % 50 };

                table1.ImportRow(RowState.Added, value);
            }

            dataEdit.Commit();

            table1.AddNestedRelation("nested", "id", "parentid");

            foreach (var itemRow in table1.Rows)
            {
                var nmtId = itemRow.Field<int>("parentid");

                var parentRows = itemRow.GetParentRows("nested").Union(itemRow.GetParentRow("nested").SingleToArray()).ToArray();

                Assert.IsNotEmpty(parentRows);

                Assert.True(parentRows.All(r => r.Field<int>("id") == nmtId));

                var parentRow = table1.GetRow("id", nmtId);

                itemRow.SetParentRow(table1.GetParentRelation("nested"), null);
                
                Assert.True(itemRow.IsNull("parentid"));
                
                itemRow.SetParentRow(table1.GetParentRelation("nested"), parentRow);
                
                Assert.AreEqual(nmtId, itemRow.Field<int>("parentid"));
                
                itemRow.SetParentRow((IDataRelation)table1.GetParentRelation("nested"), null);
                
                Assert.True(itemRow.IsNull("parentid"));
                
                itemRow.SetParentRow((IDataRelation)table1.GetParentRelation("nested"), parentRow);
                
                Assert.AreEqual(nmtId, itemRow.Field<int>("parentid"));
            }

            foreach (var row in table1.Rows)
            {
                var id = row.Field<int>("id");

                var childRows = row.GetChildRows("nested");

                Assert.IsNotEmpty(childRows);

                Assert.True(childRows.All(r => r.Field<int>("parentid") == id));
            }
        }

        public enum ReadOnlyTestDs
        {
            Xml,
            Json,
            Copy,
            Merge
        }
        
        [Test]
        public void TestDsReadonly([Values(ReadOnlyTestDs.Copy, ReadOnlyTestDs.Json, ReadOnlyTestDs.Xml)] ReadOnlyTestDs mode)
        {
            var dataSet = m_dsNmt.Copy();
            
            Assert.False(dataSet.IsReadOnly);

            dataSet.IsReadOnly = true;
            
            var testDs = new DataTable();

            if (mode == ReadOnlyTestDs.Xml)
            {
                var dsX = dataSet.ToXml(SerializationMode.Full);
                testDs.LoadFromXml(dsX);
            }
            else if (mode == ReadOnlyTestDs.Json)
            {
                var dsJ = dataSet.ToJson(SerializationMode.Full);
                testDs.LoadFromJson(dsJ);
            }
            else if (mode == ReadOnlyTestDs.Copy)
            {
                testDs = dataSet.Copy();
            }
            else if (mode == ReadOnlyTestDs.Merge)
            {
                testDs.FullMerge(dataSet); 
            }

            Assert.AreEqual(true, testDs.IsReadOnly);

            Assert.DoesNotThrow(() =>
            {
                testDs.IsReadOnly = false;
                testDs.IsReadOnly = true;
            }); 
            
            var metaAge = testDs.MetaAge;
            var dataAge = testDs.DataAge;
            
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.AddRelation(new DataRelation(Guid.NewGuid().ToString(), testDs.Tables.First().GetColumn(Fields.id), testDs.Tables.First().GetColumn(Fields.parentid) )));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.AddRelation(Guid.NewGuid().ToString(), testDs.Tables.First().GetColumn(Fields.id), testDs.Tables.First().GetColumn(Fields.parentid) ));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.AddTable(Guid.NewGuid().ToString()));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.AddTable(new DataTable(Guid.NewGuid().ToString())));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.DropTable(testDs.Tables.First().Name));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.RemoveRelation(testDs.Relations.First().Name));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.AcceptChanges());
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.RejectChanges());
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.ClearData());
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.FullMerge(dataSet));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.MergeMeta(dataSet));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.MergeData(dataSet));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.MergeSchema(dataSet));
            Assert.Throws<ReadOnlyAccessViolationException>(() => testDs.StartTransaction());

            foreach (var table in testDs.Tables)
            {
                Assert.AreEqual(true, table.IsReadOnly);
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.IsReadOnly = false);
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.TableName = Guid.NewGuid().ToString());
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.IsBuildin = true);
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.CaseSensitive = !table.CaseSensitive);
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.StorageDateTimeKind = DateTimeKind.Local);
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.EnforceConstraints = !table.EnforceConstraints);

                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddColumn(new CoreDataColumnContainer()));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveRow(table.Rows.First()));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.SilentlySetValue(Fields.Code, "-1"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.SilentlySetValue(Guid.NewGuid().ToString(), "-1"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.SetUpDataSet(new DataTable()));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddColumn((ICoreTableReadOnlyColumn)new CoreDataColumnContainer()));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddColumn(Guid.NewGuid().ToString()));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddIndex(Fields.parentid));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddMultiColumnIndex(_.List(Fields.parentid, Fields.id)));
                
                var dataRelation = new DataRelation(Guid.NewGuid().ToString(), table.GetColumn(Fields.id), table.GetColumn(Fields.parentid) );
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddChildRelation(dataRelation));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddParentRelation(dataRelation));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddNestedRelation(Guid.NewGuid().ToString(), Fields.id, Fields.parentid ));
               
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveMultiColumnIndex(_.List(Fields.parentid, Fields.id)));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveIndex(Fields.id));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.BeginLoad());
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.StartTransaction());
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.Owner = null);
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.FullMerge(m_dsNmt.GetTable(table.Name)));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.MergeDataOnly(m_dsNmt.GetTable(table.Name)));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.MergeMetaOnly(m_dsNmt.GetTable(table.Name)));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.StorageDateTimeKind = DateTimeKind.Utc);

                var dataRow = table.Rows.First();

                var dataRowContainer = dataRow.ToContainer();
                
                var contAge = dataRowContainer.GetRowAge();
                
                Assert.True(dataRowContainer.IsReadOnly);

                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddRow(dataRowContainer));
                
                Assert.DoesNotThrow(() => table.NewRow());
                Assert.DoesNotThrow(() => table.NewColumn());

                Assert.Throws<ReadOnlyAccessViolationException>(() => table.LoadRows(_.List(dataRow)));
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveRow(dataRow));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveRelation(dataRelation));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveMultiColumnIndex(_.List(Fields.parentid, Fields.id)));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.RejectChanges());
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddColumn("str"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AddColumn(new DataColumnContainer()));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.AcceptChanges());
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.StartTransaction());
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.SetPrimaryKeyColumn(Fields.parentid));
                Assert.Throws<ReadOnlyAccessViolationException>(() => table.SetXProperty("124", true));
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.StartTransaction());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.LockEvents());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.CopyChanges(dataRow));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.CopyFrom(dataRow));
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetXProperty("1234", DateTime.Now));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetRowError("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetRowInfo("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetRowFault("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetRowWarning("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetAdded());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetModified());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.Delete());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetParentRow(dataRelation, null));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetParentRow((IDataRelation)dataRelation, null));
                
                Assert.DoesNotThrow(() => dataRow.DetachRow());
                Assert.DoesNotThrow(() => dataRow.AttachRow(table, dataRow.RowHandle));
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.LockEvents());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.AcceptChanges());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.RejectChanges());
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.CopyChanges(dataRow));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.CopyFrom(dataRow));
                
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetXProperty("1234", DateTime.Now));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetRowError("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetRowInfo("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetRowWarning("1234"));
                Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.RowRecordState = RowState.Deleted);
                
                foreach (var column in table.GetColumns().ToData())
                {
                    Assert.False(table.CanRemoveColumn(column.ColumnName));
                    Assert.False(dataRow.CanChangeTo(column.ColumnName, null, out var reason));
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveColumn(column.ColumnHandle));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => table.RemoveColumn(column.ColumnName));

                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetNull(column));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.Set(column, string.Empty));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow[column] = null);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.Set(column.ColumnName, string.Empty));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow[column.ColumnName] = null);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.Set(new ColumnHandle(column.ColumnHandle), string.Empty));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow[new ColumnHandle(column.ColumnHandle)] = null);
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnError(column, "1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnWarning(column,"1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnInfo(column, "1234"));
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnError(column.ColumnName, "1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnWarning(column.ColumnName,"1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnInfo(column.ColumnName, "1234"));
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnError(new ColumnHandle(column.ColumnHandle), "1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnWarning(new ColumnHandle(column.ColumnHandle),"1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetColumnInfo(new ColumnHandle(column.ColumnHandle), "1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetDefault(column.ColumnName));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetDefault(new ColumnHandle(column.ColumnHandle)));

               
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SetField(column.ColumnName, null));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SilentlySetValue(column, null));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SilentlySetValue(column.ColumnName, null));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRow.SilentlySetValue(new ColumnHandle(column.ColumnHandle), null));
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.Caption = string.Empty);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.Expression = string.Empty);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.Type = TableStorageType.Empty);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.IsAutomaticValue = !column.IsAutomaticValue);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.ColumnName = column.ColumnName + column.ColumnName);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.DefaultValue = new object());
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.IsServiceColumn = !column.IsServiceColumn);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.MaxLength = 100);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.IsReadOnly = !column.IsReadOnly);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => column.SetXProperty("3123",column.ColumnName));
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetNull(column));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.Set(column, string.Empty));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer[column] = null);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.Set(column.ColumnName, string.Empty));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer[column.ColumnName] = null);
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer[column.ColumnHandle] = null);
                    
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetColumnError(column.ColumnName, "1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetColumnWarning(column.ColumnName,"1234"));
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SetColumnInfo(column.ColumnName, "1234"));
               
                    Assert.Throws<ReadOnlyAccessViolationException>(() => dataRowContainer.SilentlySetValue(column.ColumnName, null));
                }
                
                Assert.AreEqual(contAge, dataRowContainer.GetRowAge());
            }

            Assert.AreEqual(metaAge, testDs.MetaAge);
            Assert.AreEqual(dataAge, testDs.DataAge);
        }
    }
}
