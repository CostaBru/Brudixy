using System;
using Brudixy.Interfaces;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class BeginEditTests
    {
        [Test]
        public void TestIsolation()
        {
            var dataTable = CreateTable();

            var tr = dataTable.GetRowByHandle(0);
            
            var editRow = dataTable.BeginEditRow(tr);

            MutateRow(dataTable, editRow);

            ContentsAreNotEqual(dataTable, tr, editRow);

            var changedCopy = (DataRowContainer)editRow.Clone();

            var updatedRow = dataTable.EndEditRow(editRow);
            
            Assert.True(editRow.IsDetachedRow);

            ContentsEqual(dataTable, changedCopy, tr);
        }
        
        [Test]
        public void TestTransactionCommit()
        {
            var dataTable = CreateTable();
            
            var tr = dataTable.GetRowByHandle(0);
            
            var editRow = dataTable.BeginEditRow(tr);

            var transaction = dataTable.StartTransaction();

            MutateRow(dataTable, editRow);

            ContentsAreNotEqual(dataTable, tr, editRow);

            var changedCopy = (DataRowContainer)editRow.Clone();

            transaction.Commit();

            Assert.False(editRow.IsDetachedRow);

            ContentsEqual(dataTable, changedCopy, tr, invert: true);
        }
        
        [Test]
        public void TestTransactionEndEditCommit()
        {
            var dataTable = CreateTable();
            
            var tr = dataTable.GetRowByHandle(0);
            
            var editRow = dataTable.BeginEditRow(tr);

            var transaction = dataTable.StartTransaction();

            MutateRow(dataTable, editRow);

            ContentsAreNotEqual(dataTable, tr, editRow);

            var changedCopy = (DataRowContainer)editRow.Clone();
            
            dataTable.EndEdit();
            
            Assert.True(editRow.IsDetachedRow);

            ContentsEqual(dataTable, changedCopy, tr);

            transaction.Commit();

            ContentsEqual(dataTable, changedCopy, tr);
        }
        
        [Test]
        public void TestTransactionRollback()
        {
            var dataTable = CreateTable();
            
            var tr = dataTable.GetRowByHandle(0);
            
            var editRow = dataTable.BeginEditRow(tr);

            var transaction = dataTable.StartTransaction();

            MutateRow(dataTable, editRow);

            ContentsAreNotEqual(dataTable, tr, editRow);

            var changedCopy = (DataRowContainer)editRow.Clone();

            transaction.Rollback();

            Assert.False(editRow.IsDetachedRow);

            ContentsEqual(dataTable, changedCopy, tr, invert: true);
        }
        
        [Test]
        public void TestTransactionEndEditRollback()
        {
            var dataTable = CreateTable();
            
            var tr = dataTable.GetRowByHandle(0);
            
            var editRow = dataTable.BeginEditRow(tr);

            var transaction = dataTable.StartTransaction();

            MutateRow(dataTable, editRow);

            ContentsAreNotEqual(dataTable, tr, editRow);

            var changedCopy = (DataRowContainer)editRow.Clone();
            
            dataTable.EndEdit();
            
            Assert.True(editRow.IsDetachedRow);

            ContentsEqual(dataTable, changedCopy, tr);

            transaction.Rollback();

            ContentsEqual(dataTable, changedCopy, tr, invert: true);
        }

        private static DataTable CreateTable()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn("TestSet1");
            dataTable.AddColumn("TestSet2");
            dataTable.AddColumn("TestSet3");
            dataTable.AddColumn("TestSet4");
            dataTable.AddColumn("TestSet5");
            dataTable.AddColumn("TestSet6");

            dataTable.AddColumn("TestSetNull1");
            dataTable.AddColumn("TestSetNull2");
            dataTable.AddColumn("TestSetNull3");
            
            var nr = dataTable.NewRow();
            
            ResetRow(dataTable, nr);

            var tr = dataTable.AddRow(nr);
            
            return dataTable;
        }

        private static void ResetRow(DataTable dataTable, IDataRowAccessor nr)
        {
            foreach (var column in dataTable.Columns)
            {
                nr[column] = 1;
            }

            nr.SetXProperty("X", "x");
        }

        private static void MutateRow(DataTable dataTable, DataRow editRow)
        {
            editRow.Set("TestSet2", 2);
            editRow.Set(dataTable.GetColumn("TestSet3"), 2);
            editRow.Set(new ColumnHandle(dataTable.GetColumn("TestSet4").ColumnHandle), 2);

            editRow["TestSet1"] = 2;
            editRow[dataTable.GetColumn("TestSet6")] = 2;
            editRow[new ColumnHandle(dataTable.GetColumn("TestSet5").ColumnHandle)] = 2;

            editRow.SetNull(new ColumnHandle(dataTable.GetColumn("TestSetNull1").ColumnHandle));
            editRow.SetNull(dataTable.GetColumn("TestSetNull2"));
            editRow.SetNull("TestSetNull3");

            editRow.SetRowError("E");
            editRow.SetRowWarning("W");
            editRow.SetRowFault("F");
            editRow.SetRowInfo("I");

            editRow.SetXProperty("X", "y");

            editRow.SetXPropertyAnnotation("X", "Key", 1);

            editRow.SetColumnError("TestSet1", "E");
            editRow.SetColumnWarning("TestSet1", "W");
            editRow.SetColumnInfo("TestSet1", "I");
        }

        private static void ContentsAreNotEqual(DataTable dataTable, DataRow tr, DataRow editRow)
        {
            foreach (var column in dataTable.Columns)
            {
                Assert.AreNotEqual(tr[column], editRow[column], column.ColumnName);
                Assert.AreNotEqual(tr[new ColumnHandle(column.ColumnHandle)], editRow[new ColumnHandle(column.ColumnHandle)], column.ColumnName);
                Assert.AreNotEqual(tr[column.ColumnName], editRow[column.ColumnName], column.ColumnName);
                Assert.AreNotEqual(tr.Field<string>(column), editRow.Field<string>(column), column.ColumnName);
                Assert.AreNotEqual(tr.Field<string>(new ColumnHandle(column.ColumnHandle)), editRow.Field<string>(new ColumnHandle(column.ColumnHandle)), column.ColumnName);
                Assert.AreNotEqual(tr.Field<string>(column.ColumnName), editRow.Field<string>(column.ColumnName), column.ColumnName);
            }

            Assert.AreNotEqual(tr.GetRowError(), editRow.GetRowError());
            Assert.AreNotEqual(tr.GetRowWarning(), editRow.GetRowWarning());
            Assert.AreNotEqual(tr.GetRowInfo(), editRow.GetRowInfo());
            Assert.AreNotEqual(tr.GetRowFault(), editRow.GetRowFault());

            Assert.AreNotEqual(tr.GetCellError("TestSet1"), editRow.GetCellError("TestSet1"));
            Assert.AreNotEqual(tr.GetCellWarning("TestSet1"), editRow.GetCellWarning("TestSet1"));
            Assert.AreNotEqual(tr.GetCellInfo("TestSet1"), editRow.GetCellInfo("TestSet1"));

            Assert.AreNotEqual(tr.GetXProperty<string>("X"), editRow.GetXProperty<string>("X"));
            Assert.AreNotEqual(tr.GetXPropertyAnnotation<int>("X", "Key"), editRow.GetXPropertyAnnotation<int>("X", "Key"));
        }
        
        private static void ContentsEqual(DataTable dataTable, DataRowContainer expected, DataRow testRow, bool invert = false)
        {
            Action<object, object, string> assert = (x, y, col) =>
            {
                Assert.AreEqual(x, y, col);
            };

            if (invert)
            {
                assert = (x, y, col) => Assert.AreNotEqual(x, y, col);
            }
            
            
            foreach (var column in dataTable.Columns)
            {
                assert(expected[column], testRow[column], column.ColumnName);
                assert(expected[column.ColumnHandle], testRow[new ColumnHandle(column.ColumnHandle)], column.ColumnName);
                assert(expected[column.ColumnName], testRow[column.ColumnName], column.ColumnName);

                assert(expected.Field<string>(column), testRow.Field<string>(column), column.ColumnName);
               // Assert.AreEqual(tr.Field<string>(new ColumnHandle(column.ColumnHandle)), editRow.Field<string>(column.ColumnHandle), column.ColumnName);
               assert(expected.Field<string>(column.ColumnName), testRow.Field<string>(column.ColumnName), column.ColumnName);
            }

            assert(expected.GetRowError(), testRow.GetRowError(), "row error");
            assert(expected.GetRowWarning(), testRow.GetRowWarning(), "row warn");
            assert(expected.GetRowInfo(), testRow.GetRowInfo(), "row info");
            assert(expected.GetRowFault(), testRow.GetRowFault(), "row fault");

            assert(expected.GetCellError("TestSet1"), testRow.GetCellError("TestSet1"), "col error");
            assert(expected.GetCellWarning("TestSet1"), testRow.GetCellWarning("TestSet1"), "col warn");
            assert(expected.GetCellInfo("TestSet1"), testRow.GetCellInfo("TestSet1"), "col info");

            assert(expected.GetXProperty<string>("X"), testRow.GetXProperty<string>("X"), "xprop");
            assert(expected.GetXPropertyAnnotation<int>("X", "Key"), testRow.GetXPropertyAnnotation<int>("X", "Key"), "xprop key");
        }
    }
}