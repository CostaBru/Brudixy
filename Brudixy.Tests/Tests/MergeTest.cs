using System;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class MergeTest
    {
        private DataTable dataTable;

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            table.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table.AddColumn(Fields.Name, TableStorageType.String);
            table.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmId, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String);
            table.Columns.Add(new DataColumnContainer() { ColumnName = "Expr1", Type = TableStorageType.String, Expression = "name + ' - ' + code"});

            table.SetPrimaryKeyColumn(Fields.id);
            
            table.AddIndex(Fields.id);
            table.AddIndex(Fields.Name);
            table.AddIndex(Fields.Directories.Guid);

            table.Capacity = 100;

            var dataEdit = table.StartTransaction();

            for (int i = 500; i > 0; i--)
            {
                var id = 500 - i;

                var row = table.ImportRow(table.NewRow().Set(Fields.id, id).Set(Fields.Name, "Name " + id).Set(Fields.Directories.CreateDt, DateTime.Now).Set(Fields.Directories.LmDt, DateTime.Now).Set(Fields.Directories.LmId, -i).Set(Fields.groupid, -1).Set(Fields.Directories.Guid, Guid.NewGuid()).Set(Fields.Code, "Code " + id % 50));

                Assert.True(row.IsAddedRow);
            }
            
            for (int i = 100; i > 0; i--)
            {
                var id = 500 - i;

                var rowAccessor = table.NewRow();
                    
                rowAccessor
                    .Set(Fields.id, id)
                    .Set(Fields.Name, "Name " + id)
                    .Set(Fields.Directories.CreateDt, DateTime.Now)
                    .Set(Fields.Directories.LmDt, DateTime.Now)
                    .Set(Fields.Directories.LmId, -i)
                    .Set(Fields.groupid, -1)
                    .Set(Fields.Directories.Guid, Guid.NewGuid())
                    .Set(Fields.Code, "Code " + id % 50);
                
                table.Rows.Add(rowAccessor);
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
        public void TestMergeSchemaOnly()
        {
            var targetTable = new DataTable();

            targetTable.MergeMetaOnly(dataTable);

            Assert.AreEqual(dataTable.ColumnCount, targetTable.ColumnCount);

            var targetColumns = targetTable.GetColumns().ToDictionary(c => c.ColumnName, c => c);

            foreach (var dataColumn in dataTable.GetColumns())
            {
                var targetColumn = targetColumns[dataColumn.ColumnName];

                Assert.AreEqual(dataColumn.Caption, targetColumn.Caption);
                Assert.AreEqual(dataColumn.FixType, targetColumn.FixType);
                Assert.AreEqual(dataColumn.AllowNull, targetColumn.AllowNull);
                Assert.AreEqual(dataColumn.IsAutomaticValue, targetColumn.IsAutomaticValue);
                Assert.AreEqual(dataColumn.MaxLength, targetColumn.MaxLength);
                Assert.AreEqual(dataColumn.Type, targetColumn.Type);
                Assert.AreEqual(dataColumn.HasIndex, targetColumn.HasIndex);
            }
        }

        [Test]
        public void TestMergeDataOnly()
        {
            var targetTable = new DataTable();

            targetTable.Name = "t_tempt";

            targetTable.AddColumn(Fields.id, TableStorageType.Int32);
            targetTable.AddColumn(Fields.Name, TableStorageType.String);
            targetTable.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            targetTable.AddColumn(Fields.groupid, TableStorageType.Int32);
            targetTable.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            targetTable.AddColumn(Fields.Directories.Code, TableStorageType.String);

            targetTable.AddIndex(Fields.id);

            targetTable.MergeDataOnly(dataTable);

            Assert.AreEqual(dataTable.RowCount, targetTable.RowCount);
            Assert.AreNotEqual(dataTable.ColumnCount, targetTable.ColumnCount);

            var targetRows = targetTable.Rows.ToArray();
            var sourceRows = dataTable.Rows.ToArray();

            foreach (var column in targetTable.GetColumns())
            {
                for (int index = 0; index < targetRows.Length; index++)
                {
                    var targetRow = targetRows[index];
                    var sourceRow = sourceRows[index];

                    Assert.AreEqual(sourceRow[column.ColumnName], targetRow[column.ColumnName]);
                }
            }
        }

        [Test]
        public void TestMergeFull()
        {
            var targetTable = new DataTable();

            targetTable.Name = "t_tempt1241243";

            targetTable.AddColumn(Fields.id, TableStorageType.Int32);
            targetTable.AddColumn(Fields.Name, TableStorageType.String);
            targetTable.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            targetTable.AddColumn(Fields.groupid, TableStorageType.Int32);
            targetTable.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            targetTable.AddColumn(Fields.Directories.Code, TableStorageType.String);

            targetTable.FullMerge(dataTable);

            Assert.AreNotEqual(dataTable.Name, targetTable.Name);

            Assert.AreEqual(dataTable.RowCount, targetTable.RowCount);
            Assert.AreEqual(dataTable.ColumnCount, targetTable.ColumnCount);

            var targetRows = targetTable.Rows.ToArray();
            var sourceRows = dataTable.Rows.ToArray();

            foreach (var column in targetTable.GetColumns())
            {
                for (int index = 0; index < targetRows.Length; index++)
                {
                    var targetRow = targetRows[index];
                    var sourceRow = sourceRows[index];

                    var actual = targetRow[column.ColumnName];
                    
                    Assert.AreEqual(sourceRow[column.ColumnName], actual);
                }
            }
        }

        [Test]
        public void TestMergeDeleted()
        {
            var testTable = dataTable.Copy();
            
            testTable.AcceptChanges();

            var targetTable = new DataTable();

            targetTable.Name = "test";

            targetTable.FullMerge(testTable);

            var toDelete = targetTable.Rows.Skip(5).Take(10).ToArray();

            foreach (var row in toDelete)
            {
                var sourceRow = dataTable.GetRowBy(row.Field<int>("id"));

                Assert.False(sourceRow.IsDeletedRow);

                row.Delete();
            }

            testTable.MergeDataOnly(targetTable);

            foreach (var row in toDelete)
            {
                Assert.Null(testTable.GetRowBy(row.Field<int>("id")));

                var sourceRow = testTable.AllRows.FirstOrDefault(r => r.Field<int>("id") == row.Field<int>("id"));

                Assert.NotNull(sourceRow);

                Assert.True(sourceRow.IsDeletedRow);
            }
        }

        [Test]
        public void TestLoadRowsAppendDifferentScheme([Values(true, false)] bool copy)
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            table.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String);
            table.AddColumn(Fields.Name, TableStorageType.String);

            table.Capacity = 100;

            var dataEdit = table.StartTransaction();

            for (int i = 500; i > 0; i--)
            {
                var id = 500000 - i;

                var row = table.ImportRow(table.NewRow().Set(Fields.id, id).Set(Fields.Name, "Name " + id).Set(Fields.groupid, -i).Set(Fields.Code, "Code " + id % 50));
            }

            dataEdit.Commit();

            var testTable = copy ? dataTable.Copy() : dataTable.Clone();

            var toImport = table.Rows.ToArray();

            testTable.LoadRows(toImport);

            for (int i = 500; i > 0; i--)
            {
                var importedRow = toImport[i - 1];

                var id = importedRow.FieldNotNull<int>("id");

                var resultRow = testTable.GetRowBy(id);

                foreach (var importedRowColumn in importedRow.GetColumns())
                {
                    Assert.AreEqual(resultRow[importedRowColumn.ColumnName], importedRow[importedRowColumn.ColumnName]);
                }
            }
        }

        [Test]
        public void TestLoadRowsAppendSameScheme()
        {
            var targetTable = new DataTable();

            targetTable.Name = "test";

            targetTable.FullMerge(dataTable);

            var dataEdit = targetTable.StartTransaction();

            var rows = targetTable.Rows.ToData();

            foreach (var row in rows)
            {
                row["id"] = (row.Field<int>("id") + 1000) * 1000;
            }

            dataEdit.Commit();

            var newTable = dataTable.Copy();

            newTable.LoadRows(rows);

            Assert.AreEqual(dataTable.RowCount + targetTable.RowCount, newTable.RowCount);

            foreach (var row in rows.Union(dataTable.Rows))
            {
                var dataRow = newTable.GetRowBy(row.Field<int>("id"));

                Assert.NotNull(dataRow);
            }
        }


        [Test]
        public void TestLoadRowsUpsertDifferentScheme([Values(true, false)] bool overrideRows)
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            table.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String);
            table.AddColumn(Fields.Name, TableStorageType.String);

            table.SetPrimaryKeyColumn(Fields.id);
            
            table.Capacity = 100;

            var transaction = table.StartTransaction();

            for (int i = 500; i > 0; i--)
            {
                var id = 500000 - i;

                var row = table.ImportRow(table.NewRow().Set(Fields.id, id).Set(Fields.Code, "Code " + id % 50).Set(Fields.Name, "Name " + id).Set(Fields.groupid, -i));
            }

            var updatedRows = new Data<IDataTableRow>();

            for (int i = 10; i > 0; i--)
            {
                var id = 500 - i;

                var row = table.ImportRow(table.NewRow().Set(Fields.id, id).Set(Fields.Code, "Updated code" + id % 2).Set(Fields.Name, "Updated name " + id).Set(Fields.groupid, 42));

                updatedRows.Add(row);
            }

            transaction.Commit();

            var testTable = dataTable.Copy();

            var toImport = table.Rows.ToData();

            toImport.Reverse();
            
            if (overrideRows)
            {
                testTable.LoadRows(toImport, overrideExisting: true);

                for (int i = 500; i > 0; i--)
                {
                    var importedRow = toImport[i - 1];

                    var id = importedRow.FieldNotNull<int>("id");

                    var resultRow = testTable.GetRowBy(id);

                    foreach (var importedRowColumn in importedRow.GetColumns())
                    {
                        Assert.AreEqual(resultRow[importedRowColumn.ColumnName], importedRow[importedRowColumn.ColumnName]);
                    }
                }

                foreach (var updatedRow in updatedRows)
                {
                    var id = updatedRow.Field<int>("id");

                    var resultRow = testTable.GetRowBy(id);

                    Assert.True(resultRow.Field<string>(Fields.Name).StartsWith("Updated"));
                    Assert.True(resultRow.Field<string>(Fields.Code).StartsWith("Updated"));
                    Assert.True(resultRow.Field<int>(Fields.groupid) == 42);
                }
            }
            else
            {
                Assert.Throws<ConstraintException>(() =>
                {
                    testTable.ImportRows(toImport);
                });
            }
        }

        [Test]
        public void TestLoadRowsUpsertSameScheme()
        {
            var targetTable = new DataTable();

            targetTable.Name = "test";

            targetTable.FullMerge(dataTable);

            var beginEdit = targetTable.StartTransaction();

            var rows = targetTable.Rows.ToData();

            foreach (var row in rows.Skip(5))
            {
                row["id"] = (row.Field<int>("id") + 1000) * 1000;
            }

            foreach (var row in rows.Take(5))
            {
                row["name"] = "test" + row["id"];
            }

            beginEdit.Commit();

            var newTable = dataTable.Copy();

            newTable.LoadRows(rows, overrideExisting: true);

            Assert.AreEqual(dataTable.RowCount + targetTable.RowCount - 5, newTable.RowCount);

            foreach (var row in rows.Skip(5).Union(dataTable.Rows))
            {
                var dataRow = newTable.GetRowBy(row.Field<int>("id"));

                Assert.NotNull(dataRow);
            }

            foreach (var row in rows.Take(5))
            {
                var dataRow = newTable.GetRowBy(row.Field<int>("id"));

                Assert.AreEqual("test" + dataRow["id"], row["name"]);
            }
        }

        [Test]
        public void TestLoadRowsIdName([Values(true, false)] bool overrideRows,[Values(true, false)] bool correctOrder)
        {
            var targetTable = new DataTable();

            targetTable.Name = "test";

            if (correctOrder)
            {
                targetTable.AddColumn("id", TableStorageType.Int32);
                targetTable.AddColumn(Fields.Name, TableStorageType.String);
            }
            else
            {
                targetTable.AddColumn(Fields.Name, TableStorageType.String);
                targetTable.AddColumn("id", TableStorageType.Int32);
            }

            var beginEdit = targetTable.StartTransaction();

            for (int i = 500; i > 0; i--)
            {
                var id = 500000 - i;

                targetTable.ImportRow(targetTable.NewRow().Set("id", id).Set(Fields.Name, "Name " + id));
            }

            beginEdit.Commit();

            var newTable = overrideRows ? dataTable.Copy() : dataTable.Clone();

            newTable.MergeDataOnly(targetTable);

            var toImport = targetTable.Rows.ToData();

            for (int i = 500; i > 0; i--)
            {
                var importedRow = toImport[i - 1];

                var id = importedRow.FieldNotNull<int>("id");

                var resultRow = newTable.GetRowBy(id);

                foreach (var importedRowColumn in importedRow.GetColumns())
                {
                    Assert.AreEqual(resultRow[importedRowColumn.ColumnName], importedRow[importedRowColumn.ColumnName]);
                }
            }
        }
    }
}
