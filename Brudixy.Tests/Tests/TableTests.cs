using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Disassemblers;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class TableTests
    {
        [Test]
        public void TestResetAnnotations()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            CommonTest.FillTable(table);

            Assert.True(table.XProperties.Any());

            table.ClearTableXProperties();

            Assert.False(table.XProperties.Any());

            var dataRow = table.Rows.First();

            dataRow.SetColumnError(Fields.id, "Error");
            Assert.AreEqual("Error", dataRow.GetCellError(Fields.id));

            dataRow.SetRowError("Error");
            dataRow.SetRowFault("Fault");
            Assert.AreEqual("Error", dataRow.GetRowError());
            Assert.AreEqual("Fault", dataRow.GetRowFault());

            table.ClearRowOnlyAnnotations();

            Assert.IsEmpty(dataRow.GetRowError());
            Assert.IsEmpty(dataRow.GetRowFault());
            Assert.IsNotEmpty(dataRow.GetCellError(Fields.id));

            dataRow.SetRowError("Error");
            Assert.AreEqual("Error", dataRow.GetRowError());

            table.ClearCellAnnotations();

            Assert.AreEqual("Error", dataRow.GetRowError());
            Assert.IsEmpty(dataRow.GetCellError(Fields.id));

            dataRow.SetXProperty("Test", true);
            dataRow.SetXPropertyAnnotation("Test", "Key", true);
            Assert.AreEqual(true, dataRow.GetXProperty<bool>("Test"));
            Assert.AreEqual(true, dataRow.GetXPropertyAnnotation<bool>("Test", "Key"));

            table.ClearRowsXProperties();

            Assert.IsEmpty(dataRow.GetXProperties());
            
            Assert.AreEqual(false, dataRow.GetXProperty<bool>("Test"));
            Assert.AreEqual(false, dataRow.GetXPropertyAnnotation<bool>("Test", "Key"));

            dataRow.SetColumnError(Fields.id, "Error");
            Assert.AreEqual("Error", dataRow.GetCellError(Fields.id));

            dataRow.SetRowError("Error");
            Assert.AreEqual("Error", dataRow.GetRowError());
            
            table.ClearAllAnnotations();

            Assert.IsEmpty(dataRow.GetCellError(Fields.id));
            Assert.IsEmpty(dataRow.GetRowError());
        }

        [Test]
        public void TestEnumXProp()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            CommonTest.FillTable(table);

            table.SetXProperty("Test12312", StringComparison.InvariantCultureIgnoreCase);

            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase,
                table.GetXProperty<StringComparison>("Test12312"));
            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase,
                table.GetXProperty<StringComparison?>("Test12312").Value);
            Assert.Null(table.GetXProperty<StringComparison?>(Guid.NewGuid().ToString()));

            var dataColumn = table.GetColumn(Fields.id);

            dataColumn.SetXProperty("Test12312", StringComparison.InvariantCultureIgnoreCase);

            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase,
                dataColumn.GetXProperty<StringComparison>("Test12312"));
            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase,
                dataColumn.GetXProperty<StringComparison?>("Test12312").Value);
            Assert.Null(dataColumn.GetXProperty<StringComparison?>(Guid.NewGuid().ToString()));

            var row = table.Rows.First();

            row.SetXProperty("Test12312", StringComparison.InvariantCultureIgnoreCase);

            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase,
                row.GetXProperty<StringComparison>("Test12312"));
            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase,
                row.GetXProperty<StringComparison?>("Test12312").Value);
            Assert.Null(row.GetXProperty<StringComparison?>(Guid.NewGuid().ToString()));

            table.SilentlySetValue("Test12312", StringComparison.Ordinal);

            Assert.AreEqual(StringComparison.Ordinal, row.GetXProperty<StringComparison>("Test12312"));
            Assert.AreEqual(StringComparison.Ordinal, row.GetXProperty<StringComparison?>("Test12312").Value);
            Assert.Null(row.GetXProperty<StringComparison?>(Guid.NewGuid().ToString()));
        }

        [Test]
        public void TestSetColOrXProp()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            CommonTest.FillTable(table);

            var dataRow = table.Rows.First();

            var missingCol = Guid.NewGuid().ToString();

            Assert.False(dataRow.IsExistsField(missingCol));

            dataRow.SetXProperty(missingCol, string.Empty);
            dataRow.SilentlySetValue(missingCol, missingCol);

            Assert.AreEqual(missingCol, dataRow.SilentlyGetValue(missingCol));

            missingCol = Guid.NewGuid().ToString();

            Assert.False(dataRow.IsExistsField(missingCol));

            dataRow.SetXProperty(missingCol, string.Empty);
            dataRow.Set(missingCol, missingCol);

            Assert.AreEqual(missingCol, dataRow.Field<string>(missingCol));

            missingCol = Guid.NewGuid().ToString();

            Assert.False(dataRow.IsExistsField(missingCol));

            dataRow.SetXProperty(missingCol, string.Empty);
            dataRow[missingCol] = missingCol;

            Assert.AreEqual(missingCol, dataRow.Field<string>(missingCol));

            var newGuid = Guid.NewGuid();

            missingCol = newGuid.ToString();

            Assert.False(dataRow.IsExistsField(missingCol));

            dataRow.SetXProperty(missingCol, string.Empty);
            dataRow.Set(missingCol, newGuid);

            Assert.AreEqual(missingCol, dataRow.Field<string>(missingCol));

            Guid? newGuid1 = Guid.NewGuid();

            missingCol = newGuid1.ToString();

            Assert.False(dataRow.IsExistsField(missingCol));

            dataRow.SetXProperty(missingCol, string.Empty);
            dataRow.Set(missingCol, newGuid1);

            Assert.AreEqual(missingCol, dataRow.Field<string>(missingCol));
        }

        [Test]
        public void TestRebuildIndexServiceCol()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.String, serviceColumn: true);
            table.AddColumn("Data", TableStorageType.String, serviceColumn: true);
            table.AddColumn(Fields.Name, TableStorageType.String, serviceColumn: false);

            table.AddIndex(Fields.id);

            var newRow1 = table.NewRow();

            newRow1[Fields.id] = -1;
            newRow1["Data"] = "-Data1";

            var dataRow = table.AddRow(newRow1);

            dataRow[Fields.id] = 1;
            dataRow["Data"] = "Data1";

            var newRow2 = table.NewRow();

            newRow2.Set(Fields.id, -2);
            newRow2.Set("Data", "-Data2");

            dataRow = table.AddRow(newRow2);

            dataRow.Set(Fields.id, -2);
            dataRow.Set(Fields.id, new int?(2));
            dataRow.Set("Data", "Data2");

            Assert.Null(table.GetRowBy("1"));
            Assert.Null(table.GetRowBy("2"));

            table.RebuildIndex(Fields.id);

            Assert.AreEqual("Data1", table.GetRow(Fields.id, "1")["Data"]);
            Assert.AreEqual("Data2", table.GetRow(Fields.id, "2")["Data"]);

            var tableDataAge = table.DataAge;

            table.SilentlySetValue(Fields.Name, "Test");

            Assert.AreEqual(tableDataAge + 1, table.DataAge);

            tableDataAge = table.DataAge;

            table.SilentlySetValue(Fields.id, 0);

            Assert.AreEqual(tableDataAge, table.DataAge);
        }

        [Test]
        public void TestUniqueStringIndexIgnoreCase()
        {
            var table = new DataTable();

            table.AddColumn(Fields.Code);
            table.AddColumn("Data");

            table.GetColumn(Fields.Code)
                .SetXProperty(DataTable.StringIndexCaseSensitiveXProp, false);
          
            table.SetPrimaryKeyColumn(Fields.Code);

            for (int i = 0; i < 10; i++)
            {
                var values = _.MapObj((Fields.Code, "Code " + i), ("Data", "Test " + i));

                table.AddRow(table.NewRow(values));
            }

            for (int i = 0; i < 10; i++)
            {
                var dataRow = table.GetRow(Fields.Code, ("Code " + i).ToLower());

                Assert.AreEqual("Test " + i, dataRow.Field<string>("Data"));

                dataRow = table.GetRow(Fields.Code, ("Code " + i).ToUpper());

                Assert.AreEqual("Test " + i, dataRow.Field<string>("Data"));

                dataRow = table.GetRow(Fields.Code, ("Code " + i));

                Assert.AreEqual("Test " + i, dataRow.Field<string>("Data"));
            }
        }

        [Test]
        public void TestIndexRollback()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);

            table.SetPrimaryKeyColumn(Fields.id);

            var loadState = table.BeginLoad();
            {
                var newRow1 = table.NewRow();
                
                newRow1[Fields.id] = 1;
                newRow1[Fields.Code] = 1;

                table.AddRow(newRow1);
            }
            
            loadState.EndLoad();

            table.AcceptChanges();

            var r1 = table.GetRowBy(1);
            var rc1 = r1.ToContainer();
            
            var t1 = table.StartTransaction();
            {
                r1.Delete();

                var deleted = table.GetRow(Fields.id, 1, ignoreDeleted: true);

                Assert.Null(deleted);

                var ignoreDeleted = table.GetRow(Fields.id, 1, ignoreDeleted: false);

                Assert.NotNull(ignoreDeleted);
            }

            t1.Rollback();

            var rr1 = table.GetRowBy(1).ToContainer();
            Assert.True(rc1.Equals(rr1));
        }
        
        [Test]
        public void TestIndexCommit()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);

            table.SetPrimaryKeyColumn(Fields.id);

            var loadState = table.BeginLoad();
            {
                var newRow1 = table.NewRow();
                
                newRow1[Fields.id] = 1;
                newRow1[Fields.Code] = 1;

                table.AddRow(newRow1);
            }
            
            loadState.EndLoad();

            table.AcceptChanges();

            var r1 = table.GetRowBy(1);
            
            var t1 = table.StartTransaction();
            {
                r1.Delete();
            }

            t1.Commit();

            var deletedAfterCommit = table.GetRowBy(1);

            Assert.Null(deletedAfterCommit);
            
            var ignoreDeleted = table.GetRow(Fields.id, 1, ignoreDeleted: false);
            
            Assert.Null(ignoreDeleted);
        }

        [Test]
        public void TestIndexNewRowRollbackTranShouldRestoreAddedRec()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);
            
            table.SetPrimaryKeyColumn(Fields.id);

            var loadState = table.BeginLoad();
            var newRow1 = table.NewRow();
                
            newRow1[Fields.id] = 1;
            newRow1[Fields.Code] = 1;

            var dr = table.AddRow(newRow1);
            
            loadState.EndLoad();

            var transaction = table.StartTransaction();
            
            dr.Delete();

            transaction.Rollback();

            var dataRow = table.Rows.First();

            var ext = dataRow.EqualsExt(dr);

            Assert.True(ext.value, ext.type +',' + ext.name);
        }

        [Test]
        public void TestIndexRollbackNested()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);

            table.SetPrimaryKeyColumn(Fields.id);

            var loadState = table.BeginLoad();
            {
                var newRow1 = table.NewRow();
                
                newRow1[Fields.id] = 1;
                newRow1[Fields.Code] = 1;

                table.AddRow(newRow1);
            }
            
            loadState.EndLoad();

            table.AcceptChanges();

            var r1 = table.GetRowBy(1);
            var rc1 = r1.ToContainer();
            
            var t1 = table.StartTransaction();
            {
                var t2 = table.StartTransaction();

                r1.Delete();
                
                t2.Commit();

                var deleted = table.GetRow(Fields.id, 1, ignoreDeleted: true);

                Assert.Null(deleted);

                var ignoreDeleted = table.GetRow(Fields.id, 1, ignoreDeleted: false);

                Assert.NotNull(ignoreDeleted);
            }

            t1.Rollback();

            var rr1 = table.GetRowBy(1).ToContainer();
            Assert.True(rc1.Equals(rr1));
        }

        [Test]
        public void TestMultiIndexRollbackNested()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);

            table.SetPrimaryKeyColumns(new[] { Fields.id, Fields.groupid });

            var loadState = table.BeginLoad();
            {
                var newRow1 = table.NewRow();
                newRow1[Fields.id] = 1;
                newRow1[Fields.groupid] = 1;
                newRow1[Fields.Code] = 1;

                table.AddRow(newRow1);
            }
            loadState.EndLoad();
            table.AcceptChanges();

            var r1 = table.Rows.Where(Fields.id).Equals(1).Where(Fields.groupid).Equals(1).First();

            var rc1 = r1.ToContainer();

            var t1 = table.StartTransaction();
            {
                var t2 = table.StartTransaction();
                {
                    r1.Delete();

                    var deletetedInTran = table.Rows
                        .Where(Fields.id).Equals(1)
                        .Where(Fields.groupid).Equals(1)
                        .FirstOrDefault();

                    Assert.Null(deletetedInTran);

                    t2.Commit();
                }

                var deletetedAfterCommit = table.Rows
                    .Where(Fields.id).Equals(1)
                    .Where(Fields.groupid).Equals(1)
                    .FirstOrDefault();

                Assert.Null(deletetedAfterCommit);
            }

            t1.Rollback();

            var rr1 = table.Rows
                .Where(Fields.id).Equals(1)
                .Where(Fields.groupid).Equals(1)
                .First()
                .ToContainer();

            Assert.True(rc1.Equals(rr1));
        }
        
         [Test]
        public void TestMultiIndexRollback()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);

            table.SetPrimaryKeyColumns(new[] { Fields.id, Fields.groupid });

            var loadState = table.BeginLoad();
            {
                var newRow1 = table.NewRow();
                newRow1[Fields.id] = 1;
                newRow1[Fields.groupid] = 1;
                newRow1[Fields.Code] = 1;

                table.AddRow(newRow1);
            }
            loadState.EndLoad();
            table.AcceptChanges();

            var r1 = table.Rows
                .Where(Fields.id).Equals(1)
                .Where(Fields.groupid).Equals(1).First();

            var rc1 = r1.ToContainer();

            var t1 = table.StartTransaction();

            {
                r1.Delete();

                var deletetedR1 = table.Rows
                    .Where(Fields.id).Equals(1)
                    .Where(Fields.groupid).Equals(1)
                    .FirstOrDefault();

                Assert.Null(deletetedR1);
            }

            t1.Rollback();

            var rr1 = table.Rows
                .Where(Fields.id).Equals(1)
                .Where(Fields.groupid).Equals(1).First().ToContainer();
          
            Assert.True(rc1.Equals(rr1));
        }

        [Test]
        public void TestIndexRollbackAggregateReset()
        {
            var table = new DataTable();

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.Code, TableStorageType.String);

            table.SetPrimaryKeyColumn(Fields.id);

            var loadState = table.BeginLoad();

            var newRow1 = table.NewRow();
            newRow1[Fields.id] = 1;
            newRow1[Fields.Code] = 1;

            table.AddRow(newRow1);

            var newRow2 = table.NewRow();
            newRow2[Fields.id] = 2;
            newRow2[Fields.Code] = 2;

            table.AddRow(newRow2);

            var newRow3 = table.NewRow();
            newRow3[Fields.id] = 3;
            newRow3[Fields.Code] = 3;

            table.AddRow(newRow3);

            loadState.EndLoad();

            table.AcceptChanges();

            var colChanged = false;
            var codeColChanged = false;
            var xPropChanged = false;
            var rowChanged = false;

            var colChangedCnt = 0;
            var codeColChangedCnt = 0;
            var xPropChangedCnt = 0;
            var rowChangedCnt = 0;

            table.SubscribeColumnChanged(Fields.Code, (args, c) =>
            {
                codeColChanged = true;
                codeColChangedCnt++;
            });

            table.ColumnChanged.Subscribe((args, c) =>
            {
                colChanged = true;
                colChangedCnt++;
            });
            table.RowXPropertyChanged.Subscribe((args, c) =>
            {
                xPropChanged = true;
                xPropChangedCnt++;
            });
            table.DataRowChanged.Subscribe((args, c) =>
            {
                rowChanged = true;
                rowChangedCnt++;
                
                return false;
            });

            var r1 = table.GetRowBy(1);

            r1[Fields.Code] = 5;
            r1.SetXProperty(Fields.Code, 5);

            Assert.True(codeColChanged);
            Assert.True(colChanged);
            Assert.True(xPropChanged);
            Assert.True(rowChanged);

            codeColChanged = false;
            colChanged = false;
            xPropChanged = false;
            rowChanged = false;

            var lockEvents = r1.LockEvents();

            r1[Fields.Code] = 6;
            r1.SetXProperty(Fields.Code, 6);

            lockEvents.UnlockEvents();

            Assert.True(codeColChanged);
            Assert.True(colChanged);
            Assert.True(xPropChanged);
            Assert.True(rowChanged);

            codeColChanged = false;
            colChanged = false;
            xPropChanged = false;
            rowChanged = false;

            var transaction = table.StartTransaction();
            {
                lockEvents = r1.LockEvents();

                r1[Fields.Code] = 7;
                r1.SetXProperty(Fields.Code, 7);

                transaction.Rollback();

                lockEvents.UnlockEvents();
            }

            Assert.False(codeColChanged);
            Assert.False(colChanged);
            Assert.False(xPropChanged);
            Assert.False(rowChanged);

            codeColChanged = false;
            colChanged = false;
            xPropChanged = false;
            rowChanged = false;

            colChangedCnt = 0;
            codeColChangedCnt = 0;
            xPropChangedCnt = 0;
            rowChangedCnt = 0;

            transaction = table.StartTransaction();
            {
                r1[Fields.Code] = 8;
                r1.SetXProperty(Fields.Code, 8);

                transaction.Rollback();
            }

            Assert.True(codeColChanged);
            Assert.True(colChanged);
            Assert.True(xPropChanged);
            Assert.True(rowChanged);

            Assert.AreEqual(2, codeColChangedCnt);
            Assert.AreEqual(2, colChangedCnt);
            Assert.AreEqual(2, xPropChangedCnt);
            Assert.AreEqual(4, rowChangedCnt);
        }

        [Test]
        public void TestArrayImmutability()
        {
            var data = new DataTable();

            data.AddColumn(Fields.id, TableStorageType.Int32);
            data.AddColumn(Fields.Code, TableStorageType.Int32, TableStorageTypeModifier.Array);

            var newRow = data.NewRow();

            var testArray = new int[3] { 1, 2, 3 };
            var expectedArray = testArray.ToArray();

            newRow[Fields.id] = 1;
            newRow[Fields.Code] = testArray;

            var dataRow = data.AddRow(newRow);

            testArray[0] = int.MaxValue;

            var rowData1 = dataRow.Field<int[]>(Fields.Code);

            Assert.True(testArray.SequenceEqual(rowData1) == false);

            rowData1[2] = int.MinValue;

            var rowData2 = dataRow.Field<int[]>(Fields.Code);

            Assert.False(rowData1.SequenceEqual(rowData2));

            Assert.False(ReferenceEquals(dataRow.Field<int[]>(dataRow.GetColumn(Fields.Code)),
                dataRow.Field<int[]>(new ColumnHandle(dataRow.GetColumn(Fields.Code).ColumnHandle))));
            Assert.False(ReferenceEquals(dataRow[dataRow.GetColumn(Fields.Code)],
                dataRow[new ColumnHandle(dataRow.GetColumn(Fields.Code).ColumnHandle)]));

            var ref1 = dataRow.FieldArray<int>(Fields.Code);
            var ref2 = dataRow.FieldArray<int>(Fields.Code);

            Assert.True(ReferenceEquals(ref1, ref2));

            var copy = data.Copy();

            var copyRow = copy.GetRow(Fields.id, 1);

            var copyRef1 = copyRow.FieldArray<int>(Fields.Code);

            Assert.False(ReferenceEquals(ref1, copyRef1));

            var copyRef2 = copyRow.FieldArray<int>(Fields.Code);

            Assert.True(ReferenceEquals(copyRef1, copyRef2));

            Assert.True(expectedArray.SequenceEqual(copyRef1));
        }

        [Test]
        public void TestColunmsHandles()
        {
            var dataTable = new DataTable();

            var d1 = dataTable.AddColumn("d1");
            var d2 = dataTable.AddColumn("d2");

            var r1 = dataTable.NewRow();
            var rd1 = r1.GetColumn("d1");
            var rd2 = r1.GetColumn("d2");
            
            Assert.AreEqual(0, d1.ColumnHandle);
            Assert.AreEqual(1, d2.ColumnHandle);
            
            Assert.AreEqual(0, rd1.ColumnHandle);
            Assert.AreEqual(1, rd2.ColumnHandle);
        }

        [Test]
        public void TestAllowNullStorage()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn("test1", TableStorageType.Int32, allowNull: false);
            dataTable.AddColumn("test2", TableStorageType.Int32, allowNull: true);

            var dataRow = dataTable.AddRow(_.MapObj(("test1", 1), ("test2", 2)));

            dataRow.Set("test1", new int?(5));
            dataRow.Set("test2", 6);
            
            Assert.AreEqual(5, dataRow.Field<int>("test1"));
            Assert.AreEqual(5, dataRow.Field<int?>("test1"));
            Assert.AreEqual(6, dataRow.Field<int>("test2"));
            Assert.AreEqual(6, dataRow.Field<int?>("test2"));
            
            Assert.AreEqual(5m, dataRow.Field<decimal>("test1"));
            Assert.AreEqual(5m, dataRow.Field<decimal?>("test1"));
            
            Assert.AreEqual("6", dataRow.Field<string>("test2"));
        }
        
        [Test]
        public void TestGetFieldNotNullDefaults()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(TableStorageType.Int32.ToString(), TableStorageType.Int32, allowNull: true);
            dataTable.AddColumn(TableStorageType.Int32 + "." + TableStorageTypeModifier.Array, TableStorageType.Int32, TableStorageTypeModifier.Array, allowNull: true);
            dataTable.AddColumn(TableStorageType.String.ToString(), TableStorageType.String, allowNull: true);
            dataTable.AddColumn(TableStorageType.TimeSpan + "." + TableStorageTypeModifier.Range, TableStorageType.TimeSpan, TableStorageTypeModifier.Range, allowNull: true);

            var newRow = (IDataRowReadOnlyAccessor)dataTable.NewRow();
            
            TestNullAndNotNullExpectations(newRow);

            var dataRow = dataTable.AddRow(newRow);
            
            TestNullAndNotNullExpectations(dataRow);
        }

        private static void TestNullAndNotNullExpectations(IDataRowReadOnlyAccessor newRow)
        {
            Assert.Null(newRow[TableStorageType.Int32.ToString()]);
            Assert.Null(newRow.Field<int?>(TableStorageType.Int32.ToString()));

            Assert.Null(newRow[TableStorageType.Int32 + "." + TableStorageTypeModifier.Array]);
            Assert.Null(newRow[TableStorageType.String.ToString()]);
            Assert.Null(newRow[TableStorageType.TimeSpan+ "." + TableStorageTypeModifier.Range]);

            Assert.AreEqual(0, newRow.Field<int>(TableStorageType.Int32.ToString()));
            Assert.IsEmpty(newRow.Field<int[]>(TableStorageType.Int32+ "." + TableStorageTypeModifier.Array));
            Assert.IsEmpty(newRow.FieldArray<int>(TableStorageType.Int32+ "." + TableStorageTypeModifier.Array));
            Assert.IsEmpty(newRow.Field<string>(TableStorageType.String.ToString()));
            Assert.True(newRow.Field<Range<TimeSpan>>(TableStorageType.TimeSpan + "." + TableStorageTypeModifier.Range).GetLenghtD() == 0);

            Assert.AreEqual(0, newRow.GetOriginalValue<int>(TableStorageType.Int32.ToString()));
            Assert.IsEmpty(newRow.GetOriginalValue<int[]>(TableStorageType.Int32 + "." + TableStorageTypeModifier.Array));
            Assert.IsEmpty(newRow.GetOriginalValue<string>(TableStorageType.String.ToString()));
            Assert.True(newRow.GetOriginalValue<Range<TimeSpan>>(TableStorageType.TimeSpan + "." + TableStorageTypeModifier.Range).GetLenghtD() == 0);
        }
        
        [Test]
        public void TestRowAnnotationEvent()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(TableStorageType.Int32.ToString(), TableStorageType.Int32, allowNull: true);

            var newRow = (IDataRowReadOnlyAccessor)dataTable.NewRow();
            
            var dataRow = dataTable.AddRow(newRow);

            var changes = new Map<RowMetadataType, int>();

            dataTable.RowMetadataChangedEvent.Subscribe((args, c) =>
            {
                changes[args.MetadataType] = args.GetValue<int>();
            });
            
            TestAnnotationEvents(dataRow);
        }

        private static void TestAnnotationEvents(IDataRowAccessor dataRow)
        {
            dataRow.SetXProperty("Any", "test");

            var annotationAge = dataRow.GetAnnotationAge();

            Assert.AreEqual(1, annotationAge);

            dataRow.SetRowAnnotation("Any", 1);
            dataRow.SetCellAnnotation(TableStorageType.Int32.ToString(), "Any", 1);
            dataRow.SetXPropertyAnnotation("Any", "Any", 1);

            Assert.AreEqual(4, dataRow.GetAnnotationAge());
        }

        [Test]
        public void TestRowContainerAnnotationEvent()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(TableStorageType.Int32.ToString(), TableStorageType.Int32, allowNull: true);

            var newRow = dataTable.NewRow();

            var changes = new Map<RowMetadataType, int>();

            newRow.MetaDataChangedEvent.Subscribe((args, c) =>
            {
                changes[args.MetadataType] = args.GetValue<int>();
            });

            TestAnnotationEvents(newRow);
        }
    }
}