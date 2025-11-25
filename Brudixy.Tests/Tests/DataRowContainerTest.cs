using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class DataRowContainerTest
    {
        private const int rowCount = 23;

        private const int initialCapacity = 101;

        private DataTable dataTable;
        
        private IDataOwner m_dataOwner = new SomeModel();

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            table.AddColumn(Fields.id, TableStorageType.Int32);
            table.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            table.AddColumn(Fields.Name, TableStorageType.String);
            table.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmId, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String, defaultValue: "[CODE-NULL]");
            table.AddColumn("Expr1", TableStorageType.String, dataExpression: "name + ' ' + Code");

            table.SetPrimaryKeyColumn(Fields.id);

            table.Capacity = initialCapacity;
            
            table.Owner = m_dataOwner = new SomeModel();

            var dataEdit = table.BeginLoad();

            for (int i = rowCount; i > 0; i--)
            {
                var id = rowCount - i;

                var newRow = table.NewRow();

                newRow[Fields.id] = id;
                newRow[Fields.Sn] = i;
                newRow[Fields.Name] = "Name " + id;
                newRow[Fields.Directories.CreateDt] = DateTime.Now;
                newRow[Fields.Directories.LmDt] = DateTime.Now;
                newRow[Fields.Directories.LmId] = -i;
                newRow[Fields.groupid] = -i;
                newRow[Fields.Directories.Guid] = Guid.NewGuid();
                newRow[Fields.Directories.Code] = "Code " + id % 3;

                var dataRow = table.AddRow(newRow);

                Assert.True(dataRow.IsAddedRow);
            }

            dataEdit.EndLoad();


            dataTable = table;
        }

        private class SomeModel : IDataOwner
        {
        }

        [TearDown]
        public void TearDown()
        {
            dataTable.Dispose();
        }

        [Test]
        public void TestHashcodeEqualsCompare()
        {
            var dataRowContainer = new DataRowContainer();

            var coreDataColumnContainer = new DataColumnContainer() { ColumnName = "Test", Type = TableStorageType.Int32, IsReadOnly = true};
            
            var metadataProps = new CoreContainerMetadataProps("Table",
                new [] {coreDataColumnContainer},
                new Map<string, CoreDataColumnContainer>() { {"Test", coreDataColumnContainer} },
                keyColumn: new string[]{ "Test" },
                0);
            
            var containerProps = new ContainerDataProps(-1, new Data<object>() { 15 });
            
            dataRowContainer.Init(metadataProps, containerProps);
            
            Assert.AreEqual(-1, dataRowContainer.RowHandle);
            
            Assert.AreEqual(15, dataRowContainer["Test"]);
            
            Assert.AreEqual(15.GetHashCode(), dataRowContainer.GetHashCode());
            
            Assert.AreEqual(0, dataRowContainer.CompareTo(dataRowContainer));
            Assert.AreEqual(0, dataRowContainer.CompareTo((IDataRowContainer)dataRowContainer));
            Assert.AreEqual(true, dataRowContainer.Equals(dataRowContainer));
            
            Assert.IsEmpty(dataRowContainer.GetXProperty<string>(Guid.NewGuid().ToString()));

            var rowContainer = dataRowContainer.Clone();
            
            Assert.True(rowContainer.Equals(dataRowContainer));
            Assert.True(rowContainer.Equals((IDataRowContainer)dataRowContainer));
            Assert.True(rowContainer.Equals((IDataRowReadOnlyContainer)dataRowContainer));

            dataRowContainer.Set("Test", 5);
            Assert.AreNotEqual(5, dataRowContainer.Field<int>("Test"));
            
            dataRowContainer.Set(dataRowContainer.GetColumn("Test"), 5);
            Assert.AreNotEqual(5, dataRowContainer.Field<int>("Test"));
            
            dataRowContainer[dataRowContainer.GetColumn("Test")] = 5;
            Assert.AreNotEqual(5, dataRowContainer.Field<int>("Test"));
            
            dataRowContainer["Test"] = 5;
            Assert.AreNotEqual(5, dataRowContainer.Field<int>("Test"));
            
            dataRowContainer[0] = 5;
            Assert.AreNotEqual(5, dataRowContainer.Field<int>("Test"));
            
            dataRowContainer.SilentlySetValue("Test", 5);
            
            Assert.AreEqual(5, dataRowContainer.Field<int>("Test"));
            
            rowContainer.Dispose();
        }

        public enum TestType
        {
            Direct,
            Copy,
            Xml,
            Json
        }
        
        [Test]
        public void TestCommon([Values(TestType.Direct, TestType.Copy, TestType.Xml, TestType.Json)] TestType testType)
        {
            var table = dataTable.Copy();
            
            table.AcceptChanges();
            
            var dataRow = (DataRow)table.GetRowBy(3);

            dataRow.SetRowError("Error");
            dataRow.SetRowWarning("Warning");
            dataRow.SetRowInfo("Info");
            
            dataRow.SetColumnError(Fields.Name, "Error");
            dataRow.SetColumnWarning(Fields.Name, "Warning");
            dataRow.SetColumnInfo(Fields.Name, "Info");
            
            dataRow.SetXProperty("Test", "Value");

            var row4Container = table.GetRowBy(4).ToContainer();
            
            dataRow.SetXProperty("X", row4Container);

            Assert.True(dataRow.GetXProperty<DataRowContainer>("X").Equals(row4Container));
            
            Assert.NotNull(table.Owner);

            DataRowContainer dataRowContainer = null;

            switch (testType)
            {
                case TestType.Direct:
                {
                    dataRowContainer = dataRow.ToContainer();
                    
                    Assert.NotNull(dataRowContainer);
            
                    Assert.NotNull(dataRowContainer.GetOwner());
                    
                    var changedXProperties = dataRowContainer.GetChangedXProperties().ToData();
            
                    Assert.AreEqual(2, changedXProperties.Count);
                    Assert.AreEqual("Test", changedXProperties[0]);
                    
                    break;
                }
                case TestType.Copy:
                {
                    dataRowContainer = dataRow.ToContainer().Clone();
                    
                    Assert.NotNull(dataRowContainer);
            
                    Assert.NotNull(dataRowContainer.GetOwner());
                    
                    break;
                }
                case TestType.Xml:
                {
                    var xElement = dataRow.ToContainer().ToXml();

                    var container = new DataRowContainer();
                    
                    container.FromXml(xElement);

                    dataRowContainer = container; break;
                }
                case TestType.Json:
                {
                    var json = dataRow.ToContainer().ToJson();

                    var container = new DataRowContainer();
                    
                    container.FromJson(json);

                    dataRowContainer = container; break;
                }
            }

            foreach (var dataColumn in dataTable.GetColumns())
            {
                Assert.True(dataRowContainer.IsExistsField(dataColumn.ColumnName));

                Assert.AreEqual(dataRow[dataColumn.ColumnName], dataRowContainer[dataColumn.ColumnName], dataColumn.ColumnName);

                if (dataRowContainer.RowRecordState == RowState.Modified)
                {
                    Assert.AreEqual(dataRow.GetOriginalValue(dataColumn.ColumnName), dataRowContainer.GetOriginalValue(dataColumn.ColumnName));
                }
                Assert.AreEqual(dataRow.ToString(dataColumn.ColumnName), dataRowContainer.ToString(dataColumn.ColumnName), dataColumn.ColumnName);

              //  Assert.Throws<MissingMetadataException>(() => dataRow.GetOriginalValue(Guid.NewGuid().ToString()));
            }
            
            Assert.AreEqual("Value", dataRowContainer.GetXProperty<string>("Test"));
            
            Assert.True(dataRowContainer.GetXProperty<DataRowContainer>("X").Equals(row4Container));
            
            Assert.True(dataRowContainer.GetChangedFields().Count() == 0);
            
            Assert.AreEqual(dataRow.Field<int>(Fields.Sn), dataRowContainer.Field<int>(Fields.Sn));
            Assert.AreEqual(dataRow.Field<int>(Fields.Sn), dataRowContainer.Field<int>(Fields.Sn, -1));
            Assert.AreEqual(dataRow.Field<int>(Fields.Sn).ToString(), dataRowContainer.Field<string>(Fields.Sn));
            Assert.AreEqual(dataRow.ToString(Fields.Sn), dataRowContainer.Field<string>(Fields.Sn));

            
            Assert.AreEqual(dataRow.Field<string>(Fields.Name), dataRowContainer.Field<string>(Fields.Name));
            Assert.AreEqual(dataRow.Field<DateTime>(Fields.Directories.CreateDt), dataRowContainer.Field<DateTime>(Fields.Directories.CreateDt));
            Assert.AreEqual(dataRow.Field<Guid>(Fields.Directories.Guid), dataRowContainer.Field<Guid>(Fields.Directories.Guid));

            var container3 = table.GetRowBy(3).ToContainer();
            Assert.True(dataRowContainer.Equals(container3));
            var container4 = table.GetRowBy(4).ToContainer();
            Assert.False(dataRowContainer.Equals(container4));
            

            for (int i = 0; i < dataRow.GetColumnCount(); i++)
            {
                Assert.AreEqual(dataRow[new ColumnHandle(i)], dataRowContainer[i]);
            }

            var c1 = DataRowContainer.CreateFrom<DataRowContainer>(table.GetRowBy(3).ToContainer());
            
            Assert.AreEqual(dataRow.GetRowFault(), c1.GetRowFault());
            Assert.AreEqual(dataRow.GetRowError(), c1.GetRowError());
            Assert.AreEqual(dataRow.GetRowWarning(), c1.GetRowWarning());
            Assert.AreEqual(dataRow.GetRowInfo(), c1.GetRowInfo());
            
            
            Assert.AreEqual(dataRow.GetRowError(), c1.GetCellError(Fields.Name));
            Assert.AreEqual(dataRow.GetRowWarning(), c1.GetCellWarning(Fields.Name));
            Assert.AreEqual(dataRow.GetRowInfo(), c1.GetCellInfo(Fields.Name));

            var dump = row4Container.GetDump();
            
            Assert.NotNull(dump);

            var column = Guid.NewGuid().ToString();
            
            dataRowContainer.SetXProperty(column, column);
            
            var val = dataRowContainer[column];
            
            Assert.AreEqual(val, column);

            var dataRowReadOnlyContainer = (IDataRowReadOnlyContainer)dataRowContainer.ToReadOnly();
          
            val = dataRowReadOnlyContainer[column];
            
            Assert.AreEqual(val, column);

            var rowContainer =  (IDataRowReadOnlyContainer)dataRowContainer.Clone();
          
            val = rowContainer[column];
            
            Assert.AreEqual(val, column);
            
            dataRowContainer.FieldValueChangingEvent.Subscribe((arg, c) =>
            {
                if (arg.ColumnName == Fields.Sn)
                {
                    if (((int?)arg.NewValue) < 0)
                    {
                        arg.IsCancel = true;
                        
                        return;
                    }
                    
                    arg.NewValue = int.MinValue;
                }
            });
            
            dataRowContainer.FieldValueChangedEvent.Subscribe((arg, c) =>
            {
                if (arg.ChangedColumnNames.Contains(Fields.groupid))
                {
                    arg.Row.SetNull(Fields.Code); 
                }
            });
            
            dataRowContainer[0] = int.MaxValue;

            Assert.AreEqual(int.MaxValue, dataRowContainer.Field<int>(Fields.id));

            var snVal = dataRowContainer.Field<int>(dataRowContainer.GetColumn(Fields.Sn), -1);

            dataRowContainer[Fields.Sn] = -5;
            
            Assert.AreEqual(snVal, dataRowContainer.Field<int>(dataRowContainer.GetColumn(Fields.Sn)));
            
            dataRowContainer[Fields.Sn] = 5;
            
            Assert.AreEqual(int.MinValue, dataRowContainer[dataRowContainer.GetColumn(Fields.Sn)]);

            Assert.False(dataRowContainer.IsNull(Fields.Code));
            
            dataRowContainer.Set(Fields.groupid, new int?(5));
            
            Assert.True(dataRowContainer.IsNull(Fields.Code));
            
            dataRowContainer.Set(Fields.Code, "Test");
            
            Assert.AreEqual("Test", dataRowContainer[Fields.Code]);
            Assert.AreEqual("Test", dataRowContainer.SilentlyGetValue(Fields.Code));
            Assert.Null(dataRowContainer.SilentlyGetValue(Guid.NewGuid().ToString()));
            
            dataRowContainer.Set(dataRowContainer.GetColumn(Fields.groupid), 5);
            
            Assert.True(dataRowContainer.IsNull(Fields.Code));
            
            dataRowContainer.SetNull(dataRowContainer.GetColumn(Fields.groupid));
            
            Assert.True(dataRowContainer.IsNull(Fields.groupid));

            dataRowContainer.Set<int?>(dataRowContainer.GetColumn(Fields.groupid), 5);
            
            Assert.AreEqual(5.ToString(), dataRowContainer.Field<string>(Fields.groupid));
            
            dataRowContainer.Set<int?>(dataRowContainer.GetColumn(Fields.groupid), null);
            
            Assert.True(dataRowContainer.IsNull(Fields.groupid));
            
            dataRowContainer.Set<string>(dataRowContainer.GetColumn(Fields.groupid), "5");
            
            Assert.AreEqual(5.ToString(), dataRowContainer.Field<string>(Fields.groupid));
            
            dataRowContainer.Set<string>(dataRowContainer.GetColumn(Fields.groupid), string.Empty);
            
            Assert.True(dataRowContainer.IsNull(Fields.groupid));
            
            dataRowContainer.Set<double>(dataRowContainer.GetColumn(Fields.groupid), 5);
            
            Assert.AreEqual(5, dataRowContainer.Field<long>(Fields.groupid));
            
            dataRowContainer.Set<string>(dataRowContainer.GetColumn(Fields.groupid), null);
            
            Assert.True(dataRowContainer.IsNull(Fields.groupid));
            
            dataRowContainer.Set<int?>(dataRowContainer.GetColumn(Fields.groupid), 5);

            dataRowContainer.Set(Fields.Code, "Code 0");
            
            var expected = dataRowContainer.ToString(Fields.Name) + " " + dataRowContainer.ToString(Fields.Code);
            
            Assert.AreEqual(expected ,dataRowContainer.Field<string>("Expr1"));
            Assert.AreEqual(expected ,dataRowContainer["Expr1"]);

            dataRowContainer[Fields.Name] = Guid.NewGuid().ToString();
            
            expected = dataRowContainer.ToString(Fields.Name) + " " + dataRowContainer.ToString(Fields.Code);
            
            Assert.AreEqual(expected ,dataRowContainer.Field<string>("Expr1"));
            Assert.AreEqual(expected ,dataRowContainer["Expr1"]);
            
            ((IDataRowContainer)dataRowContainer).SetColumnXProperty(Fields.id, "Test", true);

            Assert.False(dataRow.GetColumn(Fields.id).GetXProperty<bool>("Test"));
            Assert.True(dataRowContainer.GetColumn(Fields.id).GetXProperty<bool>("Test"));

            dataRowContainer.Set<string>(Fields.groupid, "-1");
            
            Assert.AreEqual(-1 ,dataRowContainer.Field<int?>(Fields.groupid));
            
            ((IDataRowContainer)dataRowContainer).Set<string>(Fields.groupid, "");
            
            Assert.AreEqual(true ,dataRowContainer.IsNull(Fields.groupid));

            ((IDataRowContainer)dataRowContainer).Set<int?>(Fields.groupid, 5);
            
            Assert.AreEqual(5 ,dataRowContainer.Field<int?>(Fields.groupid));
            
            ((IDataRowContainer)dataRowContainer).Set<int?>(Fields.groupid, null);
            
            Assert.AreEqual(true ,dataRowContainer.IsNull(Fields.groupid));
            
            Assert.IsNull(dataRowContainer.Field<int?>(Fields.groupid));
            
            ((IDataRowContainer)dataRowContainer).Set<int?>(Fields.groupid, 5);
            
            ((IDataRowContainer)dataRowContainer).Set<string>(Fields.groupid, null);
            
            Assert.IsNull(dataRowContainer.Field<int?>(Fields.groupid));
            
            ((IDataRowContainer)dataRowContainer).Set<int?>(Fields.groupid, 5);
            
            ((IDataRowContainer)dataRowContainer).Set<string>(Fields.groupid, "5");
            
            Assert.AreEqual(5 ,dataRowContainer.Field<int?>(Fields.groupid));
            
            dataRowContainer.Set<string>(Fields.groupid, null);
            
            Assert.IsNull(dataRowContainer.Field<int?>(Fields.groupid));
            
            dataRowContainer.Set<int?>(Fields.groupid, 5);
            
            dataRowContainer.Set<string>(Fields.groupid, "5");
            
            Assert.AreEqual(5 ,dataRowContainer.Field<int?>(Fields.groupid));
            
            dataRowContainer.Set<int?>(Fields.groupid, null);
            
            Assert.AreEqual(true ,dataRowContainer.IsNull(Fields.groupid));

            dataRowContainer[0] = 5;
            
            Assert.AreEqual(5, dataRowContainer[0]);
        }
        
        [Test]
        public void TestCompare()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var dataRowContainer1 = (DataRowContainer)dataRow.ToContainer();
            var dataRowContainer2 = (DataRowContainer)dataRow.ToContainer();

            Assert.AreEqual(0, dataRowContainer1.CompareTo(dataRowContainer2));

            var rowContainer4  = table.GetRowBy(4).ToContainer();
            
            Assert.AreEqual(-1, dataRowContainer1.CompareTo(rowContainer4));
            
            var container1 = (DataRowContainer)dataRow.ToContainer();
            var container2 = (DataRowContainer)dataRow.ToContainer();

            Assert.AreEqual(0, container1.CompareTo(container2));

            var dataRowContainer4  = table.GetRowBy(4).ToContainer();
            
            Assert.AreEqual(-1, container1.CompareTo(dataRowContainer4));
        }

        [Test]
        public void TestChangingEvents()
        {
            var table = dataTable.Copy();

            var row = table.NewRow(new Map<string, object>() { { Fields.id, -1 }, { Fields.Name, "init" } });

            row.RowRecordState = RowState.Unchanged;;

            Assert.NotNull(row[Fields.Name]);
            Assert.NotNull(row[Fields.id]);
            Assert.NotNull(row[Fields.Sn]);
            
            row.XPropertyChangingEvent.Subscribe((args, c) =>
            {
               Assert.AreEqual(row, args.Row);
               
               Assert.False(args.IsCancel);
               
               Assert.AreNotEqual(args.GetOldValue<string>(), args.GetNewValue<string>());

               if (args.PropertyCode == "TEST")
               {
                   args.IsCancel = true;
               }
               
               if (args.PropertyCode == "SomeX")
               {
                   args.SetNewValue("SomeX");
               }
            });
            
            row.FieldValueChangingEvent.Subscribe((args, c) =>
            {
                Assert.AreEqual(row, args.Row);
               
                Assert.False(args.IsCancel);
               
                Assert.AreNotEqual(args.OldValue, args.NewValue);

                if (args.ColumnName == Fields.Name)
                {
                    args.IsCancel = true;
                }
               
                if (args.ColumnName == Fields.Sn)
                {
                    args.NewValue = (object)-1;
                }
            });

            row[Fields.Name] = Guid.NewGuid().ToString();
            row[Fields.Sn] = row.Field<int>(Fields.Sn) + 1;
            row[Fields.groupid] = 5;
            
            Assert.AreEqual("init", row.Field<string>(Fields.Name));
            Assert.AreEqual(-1, row.Field<int>(Fields.Sn));
            Assert.AreEqual(5,row.Field<int>(Fields.groupid));

            row.SetXProperty("SomeX", "SomeVal");
            row.SetXProperty("TEST", "----");
            row.SetXProperty("Prop", "Prop");

            Assert.AreEqual("SomeX", row.GetXProperty<string>("SomeX"));
            Assert.AreEqual(string.Empty, row.GetXProperty<string>("TEST"));
            Assert.AreEqual("Prop",row.GetXProperty<string>("Prop"));
        }

        [Test]
        public void TestLockEvents([Values(true, false)] bool initialize)
        {
            var table = dataTable.Copy();

            var row = table.NewRow(new Map<string, object>() { { Fields.id, -1 }, { Fields.Name, "detached" } });

            row.RowRecordState = RowState.Unchanged;;

            Assert.NotNull(row[Fields.Name]);
            Assert.NotNull(row[Fields.id]);
            Assert.NotNull(row[Fields.Sn]);

            IDataLockEventState dataLockEvent = null;
            IDataLoadState dataLoadState = null;
            
            if (initialize)
            {
                dataLoadState = row.BeginLoad();
            }
            else
            {
                dataLockEvent = row.LockEvents();
            }

            var changedXPropertiesByEvent = new HashSet<string>();
            var changingXPropertiesByEvent = new HashSet<string>();
            
            row.XPropertyChangedEvent.Subscribe((args, c) =>
            {
                Assert.AreEqual(row, args.Row);

                Assert.IsNotEmpty(args.ChangedPropertyCodes);
                
                changedXPropertiesByEvent.UnionWith(args.ChangedPropertyCodes);
                
                foreach (var propertyCode in args.ChangedPropertyCodes)
                {
                    Assert.AreNotEqual(args.GetOldValue<string>(propertyCode), args.GetNewValue<string>(propertyCode));
                }
            });
            
            row.XPropertyChangingEvent.Subscribe((args, c) =>
            {
                changingXPropertiesByEvent.Add(args.PropertyCode);
            });
            
            var changedColumnsByEvent = new HashSet<string>();

            row.FieldValueChangedEvent.Subscribe((args, c) =>
                    {
                        foreach (var columnName in args.ChangedColumnNames)
                        {
                            changedColumnsByEvent.Add(columnName);
                            
                            Assert.AreNotEqual(args.GetOldValue(columnName), args.GetNewValue(columnName));
                        };
                        
                        Assert.AreEqual(row, args.Row);
                    });

            var changingColumnsByEvent = new HashSet<string>();

            row.FieldValueChangingEvent.Subscribe((args, c) => { changingColumnsByEvent.Add(args.ColumnName); });

            row[Fields.Name] = Guid.Empty.ToString();
            row[Fields.Sn] = row.Field<int>(Fields.Sn) + 1;

            row[0] = -100;

            row.SetXProperty("SomeX", "SomeVal");
            row.SetXProperty("TEST", "----");
            
            if (initialize)
            {
                dataLoadState.EndLoad();
            }
            else
            {
                dataLockEvent.UnlockEvents();
            }

            var changedFields = row.GetChangedFields().ToData();

            if (initialize)
            {
                Assert.False(changedFields.Contains(Fields.Name));
                Assert.False(changedFields.Contains(Fields.Sn));
            }
            else
            {
                Assert.True(changedFields.Contains(Fields.Name));
                Assert.True(changedFields.Contains(Fields.Sn));
            }

            Assert.False(changedFields.Contains(Fields.groupid));

            if (initialize)
            {
                Assert.False(changedColumnsByEvent.Contains(Fields.Name));
                Assert.False(changedColumnsByEvent.Contains(Fields.Sn));
                
                Assert.False(changedXPropertiesByEvent.Contains("SomeX"));
                Assert.False(changedXPropertiesByEvent.Contains("TEST"));
            }
            else
            {
                Assert.True(changedColumnsByEvent.Contains(Fields.Name));
                Assert.True(changedColumnsByEvent.Contains(Fields.Sn));
                 
                Assert.True(changedXPropertiesByEvent.Contains("SomeX"));
                Assert.True(changedXPropertiesByEvent.Contains("TEST"));
            }
            
            Assert.AreEqual("----", row.GetXProperty<string>("TEST"));
            Assert.AreEqual("SomeVal", row.GetXProperty<string>("SomeX"));

            Assert.False(changedFields.Contains(Fields.groupid));

            Assert.False(changingColumnsByEvent.Contains(Fields.Name));
            Assert.False(changingColumnsByEvent.Contains(Fields.Sn));

            Assert.False(changingColumnsByEvent.Contains(Fields.groupid));
            Assert.False(changingXPropertiesByEvent.Contains(("TEST")));
            Assert.False(changingXPropertiesByEvent.Contains(("SomeX")));
        }

        [Test]
        public void TestStringNullSafe()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            dataRow.SetNull(Fields.Name);

            Assert.AreEqual(string.Empty, dataRow.Field<string>(Fields.Name));
            Assert.Null(dataRow[Fields.Name]);
            
            var container = dataRow.ToContainer();

            for (int i = 0; i < dataRow.GetColumnCount(); i++)
            {
                var columnName = table.GetColumn(i).ColumnName;
                
                Assert.AreEqual(dataRow[new ColumnHandle(i)], container[i], columnName);
            }
            
            Assert.AreEqual(string.Empty, container.Field<string>(Fields.Name));
            
            dataRow.SetNull(Fields.Code);

            Assert.AreEqual("[CODE-NULL]", dataRow.Field<string>(Fields.Code));
            
            container = dataRow.ToContainer();
            
            Assert.AreEqual("[CODE-NULL]", container.Field<string>(Fields.Code));

            container.Set<string>(Fields.Code, "1");
            
            Assert.AreEqual(true, container.Field<bool>(Fields.Code));
            
            dataRow.Set(Fields.Code, "1");
            
            Assert.AreEqual(true, dataRow.Field<bool>(Fields.Code));
        }
        

        [Test]
        public void TestDataModifying()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var dataRowContainer = dataRow.ToContainer();
            
            dataRowContainer.SetXProperty("Test", true);

            dataRowContainer.RowRecordState = RowState.Unchanged;
            
            dataRowContainer.SetXProperty("Test", false);
            
            var oldName = dataRowContainer.Field<string>(Fields.Name);

            dataRowContainer[Fields.Name] = 12123;

            Assert.AreEqual(RowState.Modified, dataRowContainer.RowRecordState);

            dataRowContainer.RejectChanges();

            Assert.AreEqual(RowState.Unchanged, dataRowContainer.RowRecordState);

            Assert.AreEqual(oldName, dataRowContainer.Field<string>(Fields.Name));
            Assert.AreEqual(oldName, dataRow.Field<string>(Fields.Name));
            Assert.AreEqual(true, dataRowContainer.GetXProperty<bool>("Test"));
            
            var expected = dataRowContainer.Field<int>(Fields.groupid);
            
            dataRowContainer.AcceptChanges();

            dataRowContainer.Set(Fields.groupid, 5);
            
            Assert.AreNotEqual(expected, dataRowContainer.Field<int>(Fields.groupid));
            
            Assert.AreEqual(expected, dataRowContainer.GetOriginalValue<int>(dataRowContainer.GetColumn(Fields.groupid)));
            Assert.AreEqual(expected.ToString(), dataRowContainer.GetOriginalValue<string>(dataRowContainer.GetColumn(Fields.groupid)));
        }

        [Test]
        public void TestDataEvents()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var rowAge = dataRow.GetRowAge();

            var dataRowContainer = dataRow.ToContainer();

            Assert.AreEqual(rowAge, dataRowContainer.GetRowAge());

            dataRowContainer.FieldValueChangingEvent.Subscribe((args, c) =>
                    {
                        args.IsCancel = args.ColumnName == Fields.Code;

                        if (args.ColumnName == Fields.Name)
                        {
                            args.NewValue = args.NewValue + args.Row[Fields.id].ToString();
                        }
                    });

            var fieldChanged = new HashSet<string>();

            dataRowContainer.FieldValueChangedEvent.Subscribe((args, c) =>
                    {
                        foreach (var changedColumnName in args.ChangedColumnNames)
                        {
                            fieldChanged.Add(changedColumnName);
                        }
                    });

            var oldCode = dataRowContainer.Field<string>(Fields.Code);

            dataRowContainer[Fields.Code] = Guid.NewGuid().ToString();

            Assert.AreEqual(oldCode, dataRowContainer.Field<string>(Fields.Code));

            dataRowContainer[Fields.Name] = "Test";

            Assert.AreEqual("Test" + dataRowContainer[Fields.id], dataRowContainer.Field<string>(Fields.Name));
            
            Assert.True(fieldChanged.Contains(Fields.Name));

            Assert.False(fieldChanged.Contains(Fields.Code));
            
            fieldChanged.Clear();
            
            dataRowContainer.SilentlySetValue(Fields.Name, "Test");
            
            Assert.False(fieldChanged.Contains(Fields.Name));
            
            Assert.AreEqual("Test", dataRowContainer.Field<string>(Fields.Name));
            
            dataRowContainer.SilentlySetValue(dataRowContainer.GetColumn(Fields.Name), "Test1");
            
            Assert.False(fieldChanged.Contains(Fields.Name));
            
            Assert.AreEqual("Test1", dataRowContainer.Field<string>(Fields.Name));
        }
        
        [Test]
        public void TestDataAggrEvents([Values(true, false)] bool resetAggregate)
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var rowAge = dataRow.GetRowAge();

            var dataRowContainer = dataRow.ToContainer();

            Assert.AreEqual(rowAge, dataRowContainer.GetRowAge());

            dataRowContainer.FieldValueChangingEvent.Subscribe((args, c) =>
                    {
                        args.IsCancel = args.ColumnName == Fields.Code;

                        if (args.ColumnName == Fields.Name)
                        {
                            args.NewValue = args.NewValue + args.Row[Fields.id].ToString();
                        }
                    });

            var fieldChanged = new HashSet<string>();

            dataRowContainer.FieldValueChangedEvent.Subscribe((args, c) =>
                    {
                        foreach (var changedColumnName in args.ChangedColumnNames)
                        {
                            fieldChanged.Add(changedColumnName);
                        }
                    });
            
            var xPropsChanged = new HashSet<string>();
            
            dataRowContainer.XPropertyChangedEvent.Subscribe((args, c) =>
            {
                foreach (var changedX in args.ChangedPropertyCodes)
                {
                    xPropsChanged.Add(changedX);
                    
                    Assert.True(args.IsPropertyChanged(changedX));
                }
            });
            
            table.RowXPropertyChanging.Subscribe((args, c) =>
            {
                args.IsCancel = args.PropertyCode == "Test3";

                if (args.PropertyCode == "Test2")
                {
                    args.SetNewValue(args.GetNewValue<string>() + args.Row[Fields.id].ToString());
                }
            });
            
            var lockEvents = dataRowContainer.LockEvents();

            dataRowContainer.Set(Fields.Name, 1);
            
            Assert.AreEqual(1.ToString(), dataRowContainer.Field<string>(Fields.Name));
            
            dataRowContainer.Set(Fields.Code, "Code");
            
            Assert.AreEqual("Code", dataRowContainer.Field<string>(Fields.Code));
            
            dataRowContainer.SetXProperty("Test1", 1);
            dataRowContainer.SetXProperty("Test2", true);
            dataRowContainer.SetXProperty("Test3", DateTime.Now.Date);
            
            Assert.AreEqual(1, dataRowContainer.GetXProperty<int>("Test1"));
            Assert.AreEqual(true, dataRowContainer.GetXProperty<bool>("Test2"));
            Assert.AreEqual(DateTime.Now.Date, dataRowContainer.GetXProperty<DateTime>("Test3").ToLocalTime());
            
            Assert.IsEmpty(fieldChanged);
            Assert.IsEmpty(xPropsChanged);
            
            if (resetAggregate)
            {
                lockEvents.ResetAggregatedEvents();
            }
            
            lockEvents.UnlockEvents();

            if (resetAggregate)
            {
                Assert.IsEmpty(fieldChanged);
                Assert.IsEmpty(xPropsChanged);
            }
            else
            {
                Assert.True(fieldChanged.Contains(Fields.Code));
                Assert.True(fieldChanged.Contains(Fields.Name));
                
                Assert.True(xPropsChanged.Contains("Test1"));
                Assert.True(xPropsChanged.Contains("Test2"));
                Assert.True(xPropsChanged.Contains("Test3"));
            }
        }

        [Test]
        public void TestAge()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var dataRowContainer = dataRow.ToContainer();

            var age = dataRowContainer.GetRowAge();

            dataRowContainer[Fields.Name] = 1235;

            Assert.Greater(dataRowContainer.GetRowAge(), age);

            age = dataRowContainer.GetRowAge();

            dataRowContainer[Fields.Name] = "123123";

            Assert.Greater(dataRowContainer.GetRowAge(), age);
        }

        [Test]
        public void TestReadOnly()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var dataRowContainer = dataRow.ToContainer();

            var age = dataRowContainer.GetRowAge();

            dataRowContainer[Fields.Name] = 1235;

            Assert.Greater(dataRowContainer.GetRowAge(), age);

            age = dataRowContainer.GetRowAge();

            dataRowContainer[Fields.Name] = "123123";

            Assert.Greater(dataRowContainer.GetRowAge(), age);
        }

        [Test]
        public void Test2()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            dataRow.SetRowError("Some error");
            dataRow.SetRowWarning("Some warning");
            dataRow.SetRowInfo("Some info");

            dataRow.SetColumnError(Fields.Name, "Name error");
            dataRow.SetColumnWarning(Fields.Name, "Name warning");
            dataRow.SetColumnInfo(Fields.Name, "Name info");

            dataRow.SetXProperty("Height", "1300");
            dataRow.SetXProperty("Width", "800");

            var container = dataRow.ToContainer();

            Assert.NotNull(container);

            foreach (var dataColumn in dataTable.GetColumns())
            {
                Assert.AreEqual(dataRow[dataColumn.ColumnName], container[dataColumn.ColumnName]);
            }

            Assert.AreEqual(container.GetRowError(), "Some error");
            Assert.AreEqual(container.GetRowWarning() ,"Some warning");
            Assert.AreEqual(container.GetRowInfo() , "Some info");

            Assert.AreEqual(container.GetCellError(Fields.Name), "Name error");
            Assert.AreEqual(container.GetCellWarning(Fields.Name), "Name warning");
            Assert.AreEqual(container.GetCellInfo(Fields.Name), "Name info");

            Assert.AreEqual(container.GetXProperty<int>("Height"), 1300);
            Assert.AreEqual(container.GetXProperty<int>("Width"), 800);

            var readOnlyDataRowContainer = container.ToReadOnly();

            Assert.NotNull(readOnlyDataRowContainer);

            Assert.AreEqual(readOnlyDataRowContainer.GetRowError(), "Some error");
            Assert.AreEqual(readOnlyDataRowContainer.GetRowWarning(), "Some warning");
            Assert.AreEqual(readOnlyDataRowContainer.GetRowInfo(), "Some info");

            Assert.AreEqual(readOnlyDataRowContainer.GetCellError(Fields.Name), "Name error");
            Assert.AreEqual(readOnlyDataRowContainer.GetCellWarning(Fields.Name), "Name warning");
            Assert.AreEqual(readOnlyDataRowContainer.GetCellInfo(Fields.Name), "Name info");

            Assert.AreEqual(readOnlyDataRowContainer.GetXProperty<long>("Height"), 1300L);
            Assert.AreEqual(readOnlyDataRowContainer.GetXProperty<long>("Width"), 800L);

            foreach (var dataColumn in dataTable.GetColumns())
            {
                Assert.AreEqual(dataRow[dataColumn.ColumnName], readOnlyDataRowContainer[dataColumn.ColumnName]);
            }

            var readOnlyDataContainer = dataRow.ToContainer();

            Assert.NotNull(readOnlyDataContainer);

            Assert.AreEqual(readOnlyDataContainer.GetRowError(), "Some error");
            Assert.AreEqual(readOnlyDataContainer.GetRowWarning(), "Some warning");
            Assert.AreEqual(readOnlyDataContainer.GetRowInfo(), "Some info");

            Assert.AreEqual(readOnlyDataContainer.GetCellError(Fields.Name), "Name error");
            Assert.AreEqual(readOnlyDataContainer.GetCellWarning(Fields.Name), "Name warning");
            Assert.AreEqual(readOnlyDataContainer.GetCellInfo(Fields.Name), "Name info");

            Assert.AreEqual(readOnlyDataContainer.GetXProperty<double>("Height"), 1300d);
            Assert.AreEqual(readOnlyDataContainer.GetXProperty<double>("Width"), 800d);

            foreach (var dataColumn in dataTable.GetColumns())
            {
                Assert.AreEqual(dataRow[dataColumn.ColumnName], readOnlyDataContainer[dataColumn.ColumnName]);
            }

            container.SetRowError("Another error");
            container.SetRowWarning("Another warning");
            container.SetRowInfo("Another info");
            
            Assert.AreEqual(container.GetRowError(), "Another error");
            Assert.AreEqual(container.GetRowWarning() ,"Another warning");
            Assert.AreEqual(container.GetRowInfo() , "Another info");
        }

        [Test]
        public void TestColumns()
        {
            var table = dataTable.Copy();

            var dataRow = table.GetRowBy(3);

            var dataRowContainer = dataRow.ToContainer();

            Assert.NotNull(dataRowContainer);

            var column = dataRowContainer.GetColumn(Fields.id.ToUpper());

            Assert.NotNull(column);

            Assert.NotNull(dataRowContainer.Field<int?>(Fields.id.ToUpper()));
            
            column.SetXProperty("Test", 1);
            
            Assert.AreEqual(1, column.GetXProperty<int>("Test"));
        }

        /*[Test]
        public void TestTypedContainer()
        {
            var baseTable = new NmtTable();

            var row = baseTable.NewRow();

            row.id = 1;
            row.createdt = DateTime.Now;
            row.employee_creator_name = "This";
            row.creatorid = 1;
            row.volumemeasureid = 1;
            row.dimsysid = 1;
            row.fixtype = 1;
            row.price = 1;
            row.guid = Guid.NewGuid();
            row.isdeleted = null;
            row["lmdt"] = DateTime.UtcNow.Date;
            row.lmlogin = "login";
            
            Assert.AreEqual(DateTime.UtcNow.Date, row.lmdt);

            baseTable.AddRow(row);

            NmtTableRowContainer container1 = (NmtTableRowContainer)baseTable.GetRow(0).ToContainer();

            NmtTableRowContainer container2 = (NmtTableRowContainer)baseTable.GetRow(Fields.id, 1).ToContainer();
            
            NmtTableRowContainer container3 = (NmtTableRowContainer)baseTable.GetRow(Fields.id, 1).ToContainer();
            
            Assert.True(container1.Equals(container2));
            Assert.True(container3.Equals(container1));
            
            var newRow = baseTable.NewRow();
            
            newRow.CopyFrom(container1);

            newRow.id = 2;
            newRow.dimsysid = 2;
            newRow.fixtype = 2;
            newRow.volumemeasureid = 2;
            newRow.guid = Guid.NewGuid();
            
            baseTable.AddRow(newRow);
            
            container1 = baseTable.GetRow(Fields.id, 1).ToContainer();
            
            container2 = baseTable.GetRow(Fields.id, 2).ToContainer();
            
            Assert.False(container1.Equals(container2));
        }*/

        [Test]
        public void TestXPropLogChanges()
        {
            var table = dataTable.Copy();

            var dataRow = table.Rows.First();

            var dataRowContainer = dataRow.ToContainer();

            Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);
            Assert.False(dataRowContainer.GetIsLoggingChanges());

            var dateTime = new DateTime(2000, 1, 1);

            dataRowContainer.StartTrackingChangeTimes(dateTime);

            var newCode1 = Guid.NewGuid().ToString();
            var newCode2 = Guid.NewGuid().ToString();

            using (dataRowContainer.StartLoggingChanges("Test 1"))
            {
                Assert.True(dataRowContainer.GetIsLoggingChanges());

                Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);

                dataRowContainer.SetXProperty(Fields.Code, newCode1);

                var dataLogEntries = dataRowContainer.GetLoggedChanges();

                Assert.AreEqual(1, dataLogEntries.Count);
                Assert.AreEqual(newCode1, dataLogEntries[0].Value);
                Assert.True(dataLogEntries[0].UtcTimestamp > dateTime);

                dataRowContainer.ClearLoggedChanges();

                dataRowContainer.RejectChanges();

                Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);

                dataRowContainer.SetXProperty(Fields.Code, newCode1);
                dataRowContainer.SetXProperty(Fields.Name, newCode2);

                var test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(2, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[0].Value);
                Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[0].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[1].Value);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[1].RowHandle);
                Assert.AreEqual(Fields.Name, test1Changes[1].ColumnOrXProperty);

                dataRowContainer.SetXProperty<object>(Fields.Code, null);
                dataRowContainer.SetXProperty<object>(Fields.Name, null);

                using (dataRowContainer.StartLoggingChanges("Test 1"))
                {
                    dataRowContainer.SetXProperty(Fields.Code, newCode1);
                    dataRowContainer.SetXProperty(Fields.Name, newCode2);

                    test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                    Assert.AreEqual(6, test1Changes.Length);

                    Assert.AreEqual(newCode1, test1Changes[4].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[4].ColumnOrXProperty);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[4].RowHandle);
                    Assert.AreEqual(newCode2, test1Changes[5].Value);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[5].RowHandle);
                    Assert.AreEqual(Fields.Name, test1Changes[5].ColumnOrXProperty);
                }

                dataRowContainer.SetXProperty<object>(Fields.Code, null);
                dataRowContainer.SetXProperty<object>(Fields.Name, null);

                dataRowContainer.SetXProperty(Fields.Code, newCode1);
                dataRowContainer.SetXProperty(Fields.Name, newCode2);

                test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Name, test1Changes[9].ColumnOrXProperty);

                using (dataRowContainer.StartLoggingChanges("Test 2"))
                {
                    dataRowContainer.SetXProperty<object>(Fields.Code, null);
                    dataRowContainer.SetXProperty<object>(Fields.Name, null);

                    dataRowContainer.SetXProperty(Fields.Code, newCode1);
                    dataRowContainer.SetXProperty(Fields.Name, newCode2);

                    test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 2")).ToArray();

                    Assert.AreEqual(4, test1Changes.Length);

                    Assert.AreEqual(newCode1, test1Changes[2].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[2].ColumnOrXProperty);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[2].RowHandle);
                    Assert.AreEqual(newCode2, test1Changes[3].Value);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[3].RowHandle);
                    Assert.AreEqual(Fields.Name, test1Changes[3].ColumnOrXProperty);
                }

                test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Name, test1Changes[9].ColumnOrXProperty);
            }

            dataRowContainer.RejectChanges();

            Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);

            dataRowContainer.StopTrackingChangeTimes();
        }
        
        [Test]
        public void TestLogChanges()
        {
            var table = dataTable.Copy();

            var dataRow = table.Rows.First();

            var dataRowContainer = dataRow.ToContainer();

            Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);
            Assert.False(dataRowContainer.GetIsLoggingChanges());

            var dateTime = new DateTime(2000, 1, 1);

            dataRowContainer.StartTrackingChangeTimes(dateTime);

            var newCode1 = Guid.NewGuid().ToString();
            var newCode2 = Guid.NewGuid().ToString();

            using (dataRowContainer.StartLoggingChanges("Test 1"))
            {
                Assert.True(dataRowContainer.GetIsLoggingChanges());

                Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);

                dataRowContainer.Set(Fields.Code, newCode1);

                var dataLogEntries = dataRowContainer.GetLoggedChanges();

                Assert.AreEqual(1, dataLogEntries.Count);
                Assert.AreEqual(newCode1, dataLogEntries[0].Value);
                Assert.True(dataLogEntries[0].UtcTimestamp > dateTime);

                dataRowContainer.ClearLoggedChanges();

                dataRowContainer.RejectChanges();

                Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);

                dataRowContainer.Set(Fields.Code, newCode1);
                dataRowContainer.Set(Fields.Name, newCode2);

                var test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(2, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[0].Value);
                Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[0].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[1].Value);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[1].RowHandle);
                Assert.AreEqual(Fields.Name, test1Changes[1].ColumnOrXProperty);

                dataRowContainer.Set<object>(Fields.Code, null);
                dataRowContainer.Set<object>(Fields.Name, null);

                using (dataRowContainer.StartLoggingChanges("Test 1"))
                {
                    dataRowContainer.Set(Fields.Code, newCode1);
                    dataRowContainer.Set(Fields.Name, newCode2);

                    test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                    Assert.AreEqual(6, test1Changes.Length);

                    Assert.AreEqual(newCode1, test1Changes[4].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[4].ColumnOrXProperty);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[4].RowHandle);
                    Assert.AreEqual(newCode2, test1Changes[5].Value);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[5].RowHandle);
                    Assert.AreEqual(Fields.Name, test1Changes[5].ColumnOrXProperty);
                }

                dataRowContainer.SetXProperty<object>(Fields.Code, null);
                dataRowContainer.SetXProperty<object>(Fields.Name, null);

                dataRowContainer.SetXProperty(Fields.Code, newCode1);
                dataRowContainer.SetXProperty(Fields.Name, newCode2);

                test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Name, test1Changes[9].ColumnOrXProperty);

                using (dataRowContainer.StartLoggingChanges("Test 2"))
                {
                    dataRowContainer.SetXProperty<object>(Fields.Code, null);
                    dataRowContainer.SetXProperty<object>(Fields.Name, null);

                    dataRowContainer.SetXProperty(Fields.Code, newCode1);
                    dataRowContainer.SetXProperty(Fields.Name, newCode2);

                    test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 2")).ToArray();

                    Assert.AreEqual(4, test1Changes.Length);

                    Assert.AreEqual(newCode1, test1Changes[2].Value);
                    Assert.AreEqual(Fields.Code, test1Changes[2].ColumnOrXProperty);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[2].RowHandle);
                    Assert.AreEqual(newCode2, test1Changes[3].Value);
                    Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[3].RowHandle);
                    Assert.AreEqual(Fields.Name, test1Changes[3].ColumnOrXProperty);
                }

                test1Changes = dataRowContainer.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();

                Assert.AreEqual(10, test1Changes.Length);

                Assert.AreEqual(newCode1, test1Changes[8].Value);
                Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[8].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[9].Value);
                Assert.AreEqual(dataRowContainer.RowHandle, test1Changes[9].RowHandle);
                Assert.AreEqual(Fields.Name, test1Changes[9].ColumnOrXProperty);
            }

            dataRowContainer.RejectChanges();

            Assert.AreEqual(0, dataRowContainer.GetLoggedChanges().Count);

            dataRowContainer.StopTrackingChangeTimes();
        }

        [Test]
        public void TestEvents()
        {
            var dataRow = dataTable.Rows.First();

            var dataRowContainer = dataRow.ToContainer();

            var f = false;
            var x = false;

            PropertyChangedEventHandler dataRowContainerOnPropertyChanged = (o, e) =>
            {
                if (e.PropertyName == "XProp")
                {
                    x = true;
                }
                else
                {
                    f = true;
                }
            };
            
            dataRowContainer.PropertyChanged += dataRowContainerOnPropertyChanged;
            
            dataRowContainer.SetXProperty("XProp", true);
            
            Assert.True(x);
            Assert.False(f);
            
            f = false;
            x = false;

            dataRowContainer.Set(Fields.Directories.Code, "321");
            
            Assert.True(f);
            Assert.False(x);
            
            f = false;
            x = false;
            
            dataRowContainer.PropertyChanged -= dataRowContainerOnPropertyChanged;
            
            dataRowContainer.SetXProperty("XProp", true);
            dataRowContainer.Set(Fields.Directories.Code, "321");
            
            Assert.False(f);
            Assert.False(x);
            
            PropertyChangingEventHandler dataRowContainerOnPropertyChanging = (o, e) =>
            {
                if (e.PropertyName == "XProp")
                {
                    x = true;
                }
                else
                {
                    f = true;
                }
            };
            
            f = false;
            x = false;

            dataRowContainer.PropertyChanging += dataRowContainerOnPropertyChanging;
            
            dataRowContainer.SetXProperty("XProp", true);
            
            Assert.True(x);
            Assert.False(f);
            
            f = false;
            x = false;
            
            
            dataRowContainer.Set(Fields.Directories.Code, "321");
            
            Assert.True(f);
            Assert.False(x);
            
            f = false;
            x = false;
            
            var er = false;
            EventHandler<DataErrorsChangedEventArgs> dataRowContainerOnErrorsChanged = (sender, args) => { er = true; };
            
            dataRowContainer.ErrorsChanged += dataRowContainerOnErrorsChanged;
            
            dataRowContainer.SetRowError("errr");
            
            Assert.True(er);

            er = false;
            
            dataRowContainer.SetRowInfo("test");
            
            Assert.False(er);
            
            dataRowContainer.ErrorsChanged -= dataRowContainerOnErrorsChanged;
            
            dataRowContainer.SetRowError("12312312");
            
            Assert.False(er);
            
            er = false;
            dataRowContainer.ErrorsChanged += dataRowContainerOnErrorsChanged;
            
            dataRowContainer.SetColumnError(Fields.Directories.Code, "12312312");
            
            Assert.True(er);
        }

        [Test]
        public void TestRollbackEdit()
        {
            var dataRow = dataTable.Rows.First();

            var dataRowContainer = dataRow.ToContainer();
            
            dataRowContainer.BeginEdit();
            
            dataRowContainer.Set(Fields.Directories.Code, "321");
            
            dataRowContainer.SetXProperty("Test", "test");
            
            dataRowContainer.EndEdit();
            
            Assert.AreEqual("321", dataRowContainer.Field<string>(Fields.Directories.Code));
            Assert.AreEqual("test", dataRowContainer.GetXProperty<string>("Test"));
            
            dataRowContainer.BeginEdit();
            
            dataRowContainer.Set(Fields.Directories.Code, "421");
            dataRowContainer.SetXProperty("Test", "test2");
            
            dataRowContainer.CancelEdit();

            Assert.AreEqual("321", dataRowContainer.Field<string>(Fields.Directories.Code));
            Assert.AreEqual("test", dataRowContainer.GetXProperty<string>("Test"));
        }
    }
}
