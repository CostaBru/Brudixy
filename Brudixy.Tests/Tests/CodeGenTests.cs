using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Brudixy.Converter;
using Brudixy.Delegates;
using Brudixy.TypeGenerator;
using Brudixy.TypeGenerator.Core;
using Flexols.Core.Common.Base.Data.BaseTables;
using Flexols.Data;
using Flexols.Production.sNomenclature;
/*using Flexols.Core.Common.Base.Data.BaseTables;
using Flexols.Data;
using Flexols.Production.sNomenclature;*/
using Konsarpoo.Collections;
using NUnit.Framework; //


namespace Brudixy.Tests
{
    [TestFixture]
    public class CodeGenTests
    {
        private const string c_fkNmtNmtitem = "FK_nmt_nmtitem";
        private const string c_fkNmtitemNested = "FK_nmtitem_nmtitem";

      
        [Test]
        public void TestDeadRefWeakEvent()
        {
            var unexpectedCalled = false; 

            var disposableCollection = new DisposableCollection();

            var weakEvent = new DataEvent<object>(disposableCollection);
            
            {
                weakEvent.Subscribe((arg, c) => unexpectedCalled = true);

                weakEvent.Raise(1);
                
                Assert.True(unexpectedCalled);
            }
            
            disposableCollection.Dispose();

            unexpectedCalled = false;
            
            GC.Collect();

            GC.WaitForFullGCComplete(1000);
            
            weakEvent.Raise(1);
            
            Assert.False(unexpectedCalled);
        }

        [Test]
        public void TestWeakEventExc()
        {
            var disposableCollection = new DisposableCollection();

            var weakEvent = new DataEvent<string>(disposableCollection);

            var l = new ConsoleTraceListener();

            var stringBuilder = new StringBuilder();
            
            l.Writer =  new StringWriter(stringBuilder);

            Trace.Listeners.Add(l);
            
            weakEvent.Subscribe((arg, c) => throw new FileNotFoundException(arg), "test");

            Assert.Throws<FileNotFoundException>(() => weakEvent.Raise("Err"));

            var s = stringBuilder.ToString().ToLower();

            var value = "dataevent<string> handler 'test' unhandled exception:";
            
            Assert.True(s.StartsWith(value));
        }

        [Test]
        public void BloomUniqueCheckTestInt()
        {
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).ToArray());
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(s => (float)s).ToArray());
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(s => (double)s).ToArray());
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(s => (uint)s).ToArray());
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(s => (ushort)s).ToArray());
            CheckUnique(Enumerable.Range(byte.MinValue, byte.MaxValue * 2).Select(s => (byte)s).ToArray());
            CheckUnique(Enumerable.Range(byte.MinValue, byte.MaxValue * 2).Select(s => (long)s).ToArray());
            CheckUnique(Enumerable.Range(byte.MinValue, byte.MaxValue * 2).Select(s => (ulong)s).ToArray());
            CheckUnique(Enumerable.Range(-50000, 100000).ToArray());
            

            var list = Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).ToList();
            list.AddRange(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2));

            list.Sort();

            CheckUnique(list.ToArray());
        }
        
        [Test]
        public void TestTool()
        {
            Assert.True(Tool.IsNullable<int?>());
            Assert.False(Tool.IsNullable<int>());
            Assert.False(Tool.IsNullable<string>());

            var s1 = "s1";
            var s2 = "s2";
            
            Tool.Swap(ref s1, ref s2);
            
            Assert.AreEqual("s1", s2);
            Assert.AreEqual("s2", s1);

            foreach (var ch in Enumerable.Range(0, 9).Select(i => $"{i}"[0]).ToArray())
            {
                Assert.True(Tool.IsNumber(ch), ch.ToString());
            }
            
            Assert.False(Tool.IsNumber('s'));
            
            Assert.True(new int[] {1}.SequenceEqual(Tool.ToEnumerable(1)));
        }

        [Test]
        public void TestGenericConvertorT()
        {
            TestListFail<string, FormatException>( "s");

            TestList<string>( "1");
            TestList<decimal>(1);
            TestList<double>(1);
            TestList<float>(1);
            TestList<ushort>(1);
            TestList<short>(1);
            TestList<byte>(1);
            TestList<sbyte>(1);
            TestList<long>(1);
            TestList<ulong>(1);
            TestList<int>(1);
            TestList<uint>(1);
            TestList<bool>( true);
        }

        [Test]
        public void TestConvertDateTime()
        {
            DateTime rez = default;
            var strValue = DateTime.UtcNow.ToString();
            GenericConverter.ConvertTo(ref strValue, ref rez);
            Assert.AreEqual(DateTime.Parse(strValue), rez);
            
            var value = DateTime.UtcNow;
            GenericConverter.ConvertTo(ref value, ref rez);
            Assert.AreEqual(value, rez);
        }

        private void TestList<T>(T def)
        {
            TestNum<T, decimal>(def, 1);
            TestNum<T, double>(def, 1);
            TestNum<T, float>(def, 1);
            TestNum<T, ushort>(def, 1);
            TestNum<T, short>(def, 1);
            TestNum<T, byte>(def, 1);
            TestNum<T, sbyte>(def, 1);
            TestNum<T, long>(def, 1);
            TestNum<T, ulong>(def, 1);
            TestNum<T, int>(def, 1);
            TestNum<T, uint>(def, 1);
            TestNum<T, bool>(def, true);
        }

        private void TestListFail<T, W>(T def) where W : System.Exception
        {
            Assert.Throws<InvalidCastException>(() => TestNum<T, ushort>(def, 1));

            Assert.Throws<W>(() => TestNum<T, decimal>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, double>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, float>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, short>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, byte>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, sbyte>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, long>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, ulong>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, int>(def, 1));
            Assert.Throws<InvalidCastException>(() => TestNum<T, uint>(def, 1));
            Assert.Throws<W>(() => TestNum<T, bool>(def, true));
        }

        private void TestNum<T, W>(T val, W exp)
        {
            W rez = default;
            GenericConverter.ConvertTo(ref val, ref rez);
            Assert.AreEqual(exp, rez);
        }

        [Test]
        public void BloomUniqueCheckTestString()
        {
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => c.ToString()).ToArray());
            CheckUnique(Enumerable.Range(-50000, 100000).Select(c => c.ToString()).ToArray());

            var list = Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => c.ToString()).ToList();
            list.AddRange(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => c.ToString()));

            list.Sort();

            CheckUnique(list.ToArray());
        }

        [Test]
        public void BloomUniqueCheckTestGuid()
        {
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => Guid.NewGuid()).ToArray());
            CheckUnique(Enumerable.Range(-50000, 100000).Select(c => Guid.NewGuid()).ToArray());

            var list = Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => Guid.NewGuid()).ToList();
            list.AddRange(list);

            list.Sort();

            CheckUnique(list.ToArray());
        }

        [Test]
        public void BloomUniqueCheckTestBool()
        {
            CheckUnique(Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => false).ToArray());
            CheckUnique(Enumerable.Range(-50000, 100000).Select(c => true).ToArray());

            var list = Enumerable.Range(sbyte.MinValue, sbyte.MaxValue * 2).Select(c => c % 2 == 0).ToList();
            list.AddRange(list);

            list.Sort();

            CheckUnique(list.ToArray());
        }

        private static void CheckUnique<T>(T[] ints)
        {
            var bloomFilter = new BloomFilter<T>(ints.Length * 2);

            bool unique = true;

            int fullScanCount = 0;

            var notUnique = ints.GroupBy(c => c).Any(c => c.Take(2).Count() > 1);

            foreach (var i in ints)
            {
                if (bloomFilter.Contains(i))
                {
                    if (ints.Where(item => Equals(item, i)).Take(2).Count() > 1)
                    {
                        unique = false;
                    }

                    fullScanCount++;
                }

                bloomFilter.Add(i);
            }

            Assert.AreEqual(notUnique, !unique);
        }

        [Test]
        public void TestGenTypedDs()
        {
            var dataSet = new DataTable() { TableName = "dsNmt" };

            var table1 = new DataTable(dataSet);

            table1.Name = "t_nmt";

            table1.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table1.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            table1.AddColumn(Fields.Name, TableStorageType.String);
            table1.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            table1.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            table1.AddColumn(Fields.Directories.LmId, TableStorageType.Int32);
            table1.AddColumn(Fields.groupid, TableStorageType.Int32);
            table1.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table1.AddColumn(Fields.Directories.Code, TableStorageType.String, columnMaxLength: 5);
            table1.AddColumn(TableStorageType.DateTimeOffset.ToString(), TableStorageType.DateTimeOffset);
            table1.AddColumn(TableStorageType.Decimal.ToString(), TableStorageType.Decimal);
            table1.AddColumn(TableStorageType.Boolean.ToString(), TableStorageType.Boolean);
            table1.AddColumn(TableStorageType.Double.ToString(), TableStorageType.Double);
            table1.AddColumn(TableStorageType.Single.ToString(), TableStorageType.Single);
            table1.AddColumn(TableStorageType.Byte.ToString(), TableStorageType.Byte, defaultValue: (byte)1);
            table1.AddColumn(TableStorageType.TimeSpan.ToString(), TableStorageType.TimeSpan, displayName: "DURATION");
            table1.AddColumn(TableStorageType.Uri.ToString(), TableStorageType.Uri);
            table1.AddColumn(TableStorageType.Type.ToString(), TableStorageType.Type, readOnly: true);
            table1.AddColumn("Expr1", TableStorageType.String, dataExpression: "Name + ' - ' + Code");

            table1.AddIndex(Fields.Directories.Code);

            var table2 = new DataTable(dataSet);

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

            dataSet.AddTable(table1);
            dataSet.AddTable(table2);

            dataSet.AddRelation(new DataRelation(c_fkNmtNmtitem, table1.GetColumn("id"), table2.GetColumn("nmtid")));
            dataSet.AddRelation(
                new DataRelation(c_fkNmtitemNested, table2.GetColumn("id"), table2.GetColumn("parentid"))
                    { Nested = true });
        }

        [Test]
        public void TestEmptyContainerSetup()
        {
            var nmtGroupRowContainer = new NmtGroupRowContainer();
            nmtGroupRowContainer.id = 1;

            Assert.AreEqual(1, nmtGroupRowContainer.id);
        }

        [Test]
        public void TestDatasetLogging()
        {
            var dsNmt = new dsNmt();

            var grTable = dsNmt.GroupsTable;

            grTable
                .Append(id: 1, name: "root", guid: Guid.NewGuid(), storeTypeCode: TableStorageType.Boolean, storeType: TableStorageType.Guid)
                .Append(id: 2, name: "nmt", guid: Guid.NewGuid(), parentid: 1, storeTypeCode: TableStorageType.Int32, storeType: TableStorageType.Decimal);

            int val = 0;

            var nmtTable = dsNmt.ItemsTable;

            var nmtItem1 = nmtTable.NewRow(_.MapObj(("id", 1)));

            nmtItem1.guid = Guid.NewGuid();
            nmtItem1.dim1 = 1000;
            nmtItem1.fullname = "nmt code 1";
            nmtItem1.extcode = "ext";
            nmtItem1.fixtype = 1;
            nmtItem1.price = 15;
            nmtItem1.groupid = 2;
            nmtItem1.systemobjectid = 1;
            nmtItem1.volumemeasureid = 1;

            nmtTable.AddRow(nmtItem1);
            
            var nmtItem2 = nmtTable.NewRow(_.MapObj(("id", 2)));

            nmtItem2.guid = Guid.NewGuid();
            nmtItem2.dim1 = 1000;
            nmtItem2.fullname = "nmt code 1";
            nmtItem2.extcode = "ext";
            nmtItem2.fixtype = 2;
            nmtItem2.price = 16;
            nmtItem2.groupid = 2;
            nmtItem2.systemobjectid = 1;
            nmtItem2.volumemeasureid = 2;

            nmtTable.AddRow(nmtItem2);

            var proptTable = dsNmt.PropertiesTable;

            var propItem = proptTable.NewRow(_.MapObj(("id", 1)));
            propItem.systemobjectid = 1;
            propItem.objectid = 1;
            propItem.guid = Guid.NewGuid();
            propItem.property_code = "ext_property";
            propItem.value = "123456";
            proptTable.AddRow(propItem);

            dsNmt.LogTable.Log(id: 1, nmtid: 1, log: "Some log", dt: DateTime.UtcNow);
            
            dsNmt.AcceptChanges();

            var dateTime = new DateTime(2000, 1, 1);
            
            dsNmt.StartTrackingChangeTimes(dateTime);

            var newCode1 = Guid.NewGuid().ToString();
            var newCode2 = Guid.NewGuid().ToString();

            using (dsNmt.StartLoggingChanges("Test 1"))
            {
                Assert.True(dsNmt.GetIsLoggingTableChanges());

                Assert.AreEqual(0, dsNmt.GetTableLoggedChanges("t_nmt").Count);

                var firstRow = nmtTable.Rows.First();

                firstRow[Fields.Code] = newCode1;

                var dataLogEntries = dsNmt.GetTableLoggedChanges("t_nmt");

                Assert.AreEqual(1, dataLogEntries.Count);
                Assert.AreEqual(newCode1, dataLogEntries[0].Value);
                Assert.True(dataLogEntries[0].UtcTimestamp > dateTime);

                dsNmt.ClearTableLoggedChanges("t_nmt");

                dsNmt.RejectChanges();

                Assert.AreEqual(0, dsNmt.GetTableLoggedChanges("t_nmt").Count);

                var lastRow = nmtTable.Rows.Last();

                firstRow[Fields.Code] = newCode1;
                lastRow[Fields.Code] = newCode2;

                var test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(2, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[0].Value);
                Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[0].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[1].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[1].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[1].ColumnOrXProperty);

                firstRow[Fields.Code] = null;
                lastRow[Fields.Code] = null;

                using (dsNmt.StartLoggingChanges("Test 1"))
                {
                    firstRow[Fields.Code] = newCode1;
                    lastRow[Fields.Code] = newCode2;

                    test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 1")).ToArray();

                    Assert.AreEqual(6, test1Changes.Length);

                    Assert.AreEqual(newCode1, test1Changes[4].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[4].ColumnOrXProperty);
                    Assert.AreEqual(firstRow.RowHandle, test1Changes[4].RowHandle);
                    Assert.AreEqual(newCode2, test1Changes[5].Value);
                    Assert.AreEqual(lastRow.RowHandle, test1Changes[5].RowHandle);
                    Assert.AreEqual(Fields.Code, test1Changes[5].ColumnOrXProperty);
                }

                firstRow[Fields.Code] = null;
                lastRow[Fields.Code] = null;

                firstRow[Fields.Code] = newCode1;
                lastRow[Fields.Code] = newCode2;

                test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

                using (dsNmt.StartLoggingChanges("Test 2"))
                {
                    firstRow[Fields.Code] = null;
                    lastRow[Fields.Code] = null;

                    firstRow[Fields.Code] = newCode1;
                    lastRow[Fields.Code] = newCode2;

                    test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 2")).ToArray();

                    Assert.AreEqual(4, test1Changes.Length);

                    Assert.AreEqual(newCode1, test1Changes[2].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[2].ColumnOrXProperty);
                    Assert.AreEqual(firstRow.RowHandle, test1Changes[2].RowHandle);
                    Assert.AreEqual(newCode2, test1Changes[3].Value);
                    Assert.AreEqual(lastRow.RowHandle, test1Changes[3].RowHandle);
                    Assert.AreEqual(Fields.Code, test1Changes[3].ColumnOrXProperty);
                }

                test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

                var dataEditTransaction = dsNmt.StartTransaction();

                firstRow[Fields.Code] = "123123";
                lastRow[Fields.Code] = "4124124";

                dataEditTransaction.Rollback();

                Assert.AreEqual(newCode1, firstRow[Fields.Code]);
                Assert.AreEqual(newCode2, lastRow[Fields.Code]);

                test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

                using (dsNmt.StartLoggingChanges("Test 3"))
                {
                    var t1 = dsNmt.StartTransaction();

                    var t1g = Guid.NewGuid().ToString();

                    firstRow[Fields.Code] = t1g;

                    var t2 = dsNmt.StartTransaction();

                    var t2g = Guid.NewGuid().ToString();

                    lastRow[Fields.Code] = t2g;

                    t2.Rollback();

                    Assert.AreEqual(t1g, firstRow[Fields.Code]);
                    Assert.AreEqual(newCode2, lastRow[Fields.Code]);

                    test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 3")).ToArray();

                    Assert.AreEqual(1, test1Changes.Length);

                    Assert.AreEqual(t1g, test1Changes[0].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
                    Assert.AreEqual(firstRow.RowHandle, test1Changes[0].RowHandle);

                    t1.Rollback();

                    Assert.AreEqual(newCode1, firstRow[Fields.Code]);
                    Assert.AreEqual(newCode2, lastRow[Fields.Code]);

                    var t3 = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 3")).ToArray();

                    Assert.AreEqual(0, t3.Length);
                }

                test1Changes = dsNmt.GetTableLoggedChanges("t_nmt").Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);
            }

            dsNmt.RejectChanges();

            Assert.AreEqual(0, dsNmt.GetTableLoggedChanges("t_nmt").Count);

            dsNmt.StopTrackingChangeTimes();
        }

        [Test]
        public void TestNmtTypedDs()
        {
            void TestRelations(dsNmt nmt, DateTime dateTime)
            {
                var nmtRow = (NmtTableRow)nmt.ItemsTable.GetRowByPk(1);

                Assert.NotNull(nmtRow);
                
                new DataRowDebugView(nmtRow).Items.ToArray();

                Assert.AreEqual(15, nmtRow.price);
                Assert.AreEqual("nmt code 1", nmtRow.fullname);

                var nmtLogs = nmtRow.NmtLogRows.ToArray();

                var groupRow = nmtRow.NmtGroupTableParent;

                Assert.AreEqual("nmt", groupRow.name);
                Assert.AreEqual("root", groupRow.TestGroupTableParent.name);

                Assert.AreEqual(1, nmtLogs.Length);
                Assert.AreEqual("Some log", nmtLogs[0].log);

                var logEntry = (NmtLogRow)nmt.LogTable.GetRowByArray(new IComparable[] { dateTime, 1 });

                Assert.AreEqual("Some log", logEntry.log);
                
                var logEntry2 = nmt.LogTable.GetRowByPk(dateTime, 1);

                Assert.AreEqual("Some log", logEntry2.log);
            }

            var dsNmt = new dsNmt();
            
            var grTable = dsNmt.GroupsTable;

            grTable.AddRow(grTable.NewRow(_.MapObj(("id", 1), ("name", "root"), ("guid", Guid.NewGuid()))));
            grTable.AddRow(grTable.NewRow(_.MapObj(("id", 2), ("name", "nmt"), ("parentid", 1), ("guid", Guid.NewGuid()))));

            var nmtTable = dsNmt.ItemsTable;

            var nmtItem1 = nmtTable.NewRow(_.MapObj(("id", 1)));

            nmtItem1.guid = Guid.NewGuid();
            nmtItem1.dim1 = 1000;
            nmtItem1.fullname = "nmt code 1";
            nmtItem1.extcode = "ext";
            nmtItem1.fixtype = 1;
            nmtItem1.price = 15;
            nmtItem1.systemobjectid = 1;
            nmtItem1.volumemeasureid = 1;
            nmtItem1.groupid = 2;
            
            var productTypeItem = nmtItem1.AsProductType();
            Assert.NotNull(productTypeItem);

            productTypeItem.ExternalCode = "Ext";

            var nmtTableRow = nmtTable.AddRow(productTypeItem);

            var productTypeRow = nmtTableRow.AsProductType();
            Assert.NotNull(productTypeRow);
            
            Assert.AreEqual("Ext", productTypeRow.ExternalCode);

            var productTypeContainer = productTypeRow.ToContainer();
            Assert.AreEqual("Ext", productTypeContainer.ExternalCode);

            var piEq = productTypeItem.EqualsExt(productTypeContainer);
            Assert.True(piEq.value, piEq.name + "." + piEq.type);

            var proptTable = dsNmt.PropertiesTable;

            var propItem = proptTable.NewRow(_.MapObj(("id", 1)));
            propItem.systemobjectid = 1;
            propItem.objectid = 1;
            propItem.guid = Guid.NewGuid();
            propItem.property_code = "ext_property";
            propItem.value = "123456";
            proptTable.AddRow(propItem);

            var logTable = dsNmt.LogTable;

            var logItem = logTable.NewRow(_.MapObj(("id", 1)));

            var logItemDt = DateTime.UtcNow;
            
            logItem.dt = logItemDt;
            logItem.id = 1;
            logItem.nmtid = 1;
            logItem.log = "Some log";

            logTable.AddRow(logItem);

            TestRelations(dsNmt, logItemDt);
            TestRelations(dsNmt.Copy(), logItemDt);
            var clone = dsNmt.Clone();
            clone.MergeData(dsNmt);
            TestRelations(clone, logItemDt);
            var fromXml = new dsNmt();
            fromXml.LoadFromXml(dsNmt.ToXml());
            TestRelations(fromXml, logItemDt);
            var fromJson = new dsNmt();

            var jElement = dsNmt.ToJson();

            fromJson.LoadFromJson(jElement);
            TestRelations(fromJson, logItemDt);
            
            var nmtItem2 = nmtTable.NewRow(_.MapObj(("id", 2)));

            nmtItem2.dim1 = 2000;
            nmtItem2.guid = Guid.NewGuid();
            nmtItem2.fullname = "nmt code 2";
            nmtItem2.extcode = "ext";
            nmtItem2.fixtype = 2;
            nmtItem2.price = 25;
            nmtItem2.volumemeasureid = 25;
            nmtItem2.systemobjectid = 1;

            nmtTable.AddRow(nmtItem2);

            var rootGr = (NmtGroupRow)grTable.GetRowByPk(1);

            var row2 = (NmtTableRow)nmtTable.GetRowByPk(2);

            row2.NmtGroupTableParent = rootGr;
            
            Assert.AreEqual(row2.NmtGroupTableParent, rootGr);

            rootGr.NmtTableRows.Clear();
            
            Assert.AreEqual(0, rootGr.NmtTableRows.ToArray().Length);
            
            Assert.Null(row2.NmtGroupTableParent);
            
            var logItem1 = logTable.NewRow(_.MapObj(("id", 2)));

            logItem1.dt = logItemDt.AddDays(1);
            logItem1.log = "Some log 1";

            var logRow1 = logTable.AddRow(logItem1);
            
            var logItem2 = logTable.NewRow(_.MapObj(("id", 3)));

            logItem2.dt = logItemDt.AddDays(2);
            logItem2.log = "Some log 2";

            var logRow2 = logTable.AddRow(logItem2);
            
            row2.NmtLogRows.AddRange(_.List(logRow1, logRow2));
            
            Assert.AreEqual(row2, logRow1.NmtTableParent);
            Assert.AreEqual(row2, logRow2.NmtTableParent);

            logRow2.NmtTableParent = null;
            
            Assert.Null(logRow2.NmtTableParent);
            Assert.AreEqual(row2, logRow1.NmtTableParent);
            
            Assert.AreEqual(1, row2.NmtLogRows.ToArray().Length);
            
            row2.NmtLogRows.AddRange(_.List(logRow1, logRow2));
            
            Assert.AreEqual(row2, logRow1.NmtTableParent);
            Assert.AreEqual(row2, logRow2.NmtTableParent);
            
            row2.NmtLogRows.Clear();
            
            Assert.Null(logRow1.NmtTableParent);
            Assert.Null(logRow2.NmtTableParent);
            
            row2.NmtLogRows.AddRange(_.List(logRow1, logRow2));
            
            Assert.AreEqual(row2, logRow1.NmtTableParent);
            Assert.AreEqual(row2, logRow2.NmtTableParent);
            
            row2.NmtLogRows.Remove(logRow1);
            
            Assert.Null(logRow1.NmtTableParent);
            Assert.AreEqual(row2, logRow2.NmtTableParent);
            
            nmtTable.ClearRows();
            
            Assert.AreEqual(0, nmtTable.RowCount);
            
            nmtTable.Rows.Add(nmtItem1);
            nmtTable.Rows.Add(nmtItem2);
            
            Assert.AreEqual(2, nmtTable.RowCount);
            
            var row1 = (NmtTableRow)nmtTable.GetRowByPk(2);
            row2 = (NmtTableRow)nmtTable.GetRowByPk(2);
            
            Assert.NotNull(row2);
            Assert.NotNull(row1);
            
            nmtTable.ClearRows();
            
            nmtTable.Rows.AddRange(_.List(nmtItem1, nmtItem2));
            
            Assert.AreEqual(2, nmtTable.RowCount);
            
            row1 = (NmtTableRow)nmtTable.GetRowByPk(2);
            row2 = (NmtTableRow)nmtTable.GetRowByPk(2);
            
            Assert.NotNull(row2);
            Assert.NotNull(row1);
        }

        [Test]
        public void TestTypedStandaloneGroupTable()
        {
            void CheckRelations(TestGroupTable groupTable1)
            {
                var rootRow = (TestGroupTableRow)groupTable1.GetRowByPk(1);
                var rootChild1Row = (TestGroupTableRow)groupTable1.GetRowByPk(2);
                var rootChild2Row = (TestGroupTableRow)groupTable1.GetRowByPk(3);
                
                Assert.Null(rootRow.TestGroupTableParent);
                
                var childRows = rootRow.TestGroupTableRows.ToArray();

                Assert.AreEqual(childRows.Select(r => r.id), new[] { 2, 3 });

                Assert.AreEqual(rootRow, rootChild1Row.TestGroupTableParent);
                Assert.AreEqual(rootRow, rootChild2Row.TestGroupTableParent);
            }

            var groupTable = new TestGroupTable();

            var root = groupTable.NewRow();
            
            root.id = 1;
            root.name = "c:/";
            root.guid = Guid.NewGuid();
            
            groupTable.AddRow(root);

            var rootChild1 = groupTable.NewRow();
            
            rootChild1.id = 2;
            rootChild1.parentid = 1;
            rootChild1.name = "path";
            rootChild1.guid = Guid.NewGuid();
            
            groupTable.AddRow(rootChild1);
            
            var rootChild2 = groupTable.NewRow();
            
            rootChild2.id = 3;
            rootChild2.parentid = 1;
            rootChild2.name = "test";
            rootChild2.guid = Guid.NewGuid();
            
            groupTable.AddRow(rootChild2);
            
            CheckRelations(groupTable);

            var groupTableCopy = (TestGroupTable)groupTable.Copy();
            
            CheckRelations(groupTableCopy);

            var fromXml = new TestGroupTable();

            fromXml.LoadFromXml(groupTable.ToXml());
            
            CheckRelations(fromXml);
            
            var fromJson = new TestGroupTable();

            fromJson.LoadFromJson(groupTable.ToJson());
            
            CheckRelations(fromJson);
        }

        [Test]
        public void TestTypedContainerBeginEndEdit()
        {
            var nmt = new t_nmt();
            var nmtRowDto = nmt.NewRow();
            nmtRowDto.id = 1;
            nmtRowDto.name = "test1";
            nmtRowDto.groupid = 1;
            nmtRowDto.sn = 1;
            nmtRowDto.supplierorderdoctypeid = 1;
         
            var clone = nmtRowDto.Clone();

            Assert.AreEqual(1, nmtRowDto.id);
            Assert.AreEqual( "test1", nmtRowDto.name);
            Assert.AreEqual( 1, nmtRowDto.groupid);
            Assert.AreEqual( 1, nmtRowDto.sn);
            Assert.AreEqual( 1, nmtRowDto.supplierorderdoctypeid);
            
            Assert.AreEqual(clone.id, nmtRowDto.id);
            Assert.AreEqual( clone.name, nmtRowDto.name);
            Assert.AreEqual( clone.groupid, nmtRowDto.groupid);
            Assert.AreEqual( clone.sn, nmtRowDto.sn);
            Assert.AreEqual( clone.supplierorderdoctypeid, nmtRowDto.supplierorderdoctypeid);
            
            var edit = clone.BeginEdit();

            clone.id = 2;
            clone.name = "test2";
            clone.groupid = 2;
            clone.sn = 2;
            clone.supplierorderdoctypeid = 2;
            
            Assert.AreEqual(2,  clone.id);
            Assert.AreEqual("test2",  clone.name);
            Assert.AreEqual(2,  clone.sn);
            Assert.AreEqual(2,  clone.groupid);
            Assert.AreEqual(2,  clone.supplierorderdoctypeid);
            
            Assert.AreEqual(1, nmtRowDto.id);
            Assert.AreEqual( "test1", nmtRowDto.name);
            Assert.AreEqual( 1, nmtRowDto.groupid);
            Assert.AreEqual( 1, nmtRowDto.sn);
            Assert.AreEqual( 1, nmtRowDto.supplierorderdoctypeid);

            var cloneOfClone = clone.Clone();
            
            Assert.AreEqual(2,  cloneOfClone.id);
            Assert.AreEqual("test2",  cloneOfClone.name);
            Assert.AreEqual(2,  cloneOfClone.sn);
            Assert.AreEqual(2,  cloneOfClone.groupid);
            Assert.AreEqual(2,  cloneOfClone.supplierorderdoctypeid);
          
            edit.CancelEdit();
              
            Assert.AreEqual(1, clone.id);
            Assert.AreEqual( "test1", clone.name);
            Assert.AreEqual( 1, clone.groupid);
            Assert.AreEqual( 1, clone.sn);
            Assert.AreEqual( 1, clone.supplierorderdoctypeid);
            
            Assert.AreEqual(2,  cloneOfClone.id);
            Assert.AreEqual("test2",  cloneOfClone.name);
            Assert.AreEqual(2,  cloneOfClone.sn);
            Assert.AreEqual(2,  cloneOfClone.groupid);
            Assert.AreEqual(2,  cloneOfClone.supplierorderdoctypeid);
            
            edit = clone.BeginEdit();

            clone.id = 2;
            clone.name = "test2";
            clone.groupid = 2;
            clone.sn = 2;
            clone.supplierorderdoctypeid = 2;
            
            edit.EndEdit();
            
            Assert.AreEqual(2,  clone.id);
            Assert.AreEqual("test2",  clone.name);
            Assert.AreEqual(2,  clone.sn);
            Assert.AreEqual(2,  clone.groupid);
            Assert.AreEqual(2,  clone.supplierorderdoctypeid);
            
            Assert.AreEqual(1, nmtRowDto.id);
            Assert.AreEqual( "test1", nmtRowDto.name);
            Assert.AreEqual( 1, nmtRowDto.groupid);
            Assert.AreEqual( 1, nmtRowDto.sn);
            Assert.AreEqual( 1, nmtRowDto.supplierorderdoctypeid);
            
            edit = clone.BeginEdit();

            clone.id = 3;
            clone.name = "test3";
            clone.groupid = 3;
            clone.sn = 3;
            clone.supplierorderdoctypeid = 3;

            clone.RejectChanges();
            
            Assert.Null(edit.Editing);
            
            Assert.AreEqual(2,  clone.id);
            Assert.AreEqual("test2",  clone.name);
            Assert.AreEqual(2,  clone.sn);
            Assert.AreEqual(2,  clone.groupid);
            Assert.AreEqual(2,  clone.supplierorderdoctypeid);

        }

        [Test]
        public void TestTypedContainerInitNew()
        {
            var container = new t_nmtRowContainer();

            container.id = 1;
            container.name = "test";

            var nmt = new t_nmt();

            nmt.AddRow(container);

            var tableRow = (t_nmtRow)nmt.GetRowByPk(container.id);
            
            Assert.AreEqual(container.name, tableRow.name);

            var columnContainer = new DataColumnContainer();
            columnContainer.ColumnName = "id";
            columnContainer.Type = Brudixy.TableStorageType.Int32;
            columnContainer.Expression = null;
            columnContainer.IsReadOnly = false;
            columnContainer.IsUnique = false;
            columnContainer.Caption = null;
            columnContainer.AllowNull = false;
            columnContainer.DefaultValue = null;
            columnContainer.MaxLength = null;
            columnContainer.IsAutomaticValue = false;
            columnContainer.TableName = "BaseTable";
        }
    }
}