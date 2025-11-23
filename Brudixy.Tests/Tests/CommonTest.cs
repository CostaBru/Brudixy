using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Xml.Linq;
using Brudixy.Constraints;
using Brudixy.Converter;
using Brudixy.Delegates;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Brudixy.Tests
{
internal class Fields
{
    public const string id = "id";
    public const string Name = "name";
    public const string Sn = "sn";
    public const string Code = "code";
    public const string groupid = "groupid";
    public const string parentid = "parentid";

    internal class Directories
    {
        public const string CreateDt = "createdt";
        public const string LmDt = "lmdt";
        public const string LmId = "lmid";
        public const string Guid = "guid";
        public const string Code = "code";
        public const string Data = "data";
        public const string TimeRange = "time_range";
        public const string DataTypes = "data_types";
    }
}


public class CommonTest
{
    private const int rowCount = 20;
    private const int initialCapacity = 101;

    private DataTable m_sourceDataTable;
    private DisposableCollection m_dispCollection = new DisposableCollection();

    private static DateTime m_timeNow = DateTime.Now;
    private static Guid m_guid = Guid.NewGuid();
   
        
    [SetUp]
    public void Setup()
    {
        var table = new DataTable();

        table.Name = "t_nmt";

        FillTable(table);

        m_sourceDataTable = table;

        var dataRowContainer1 = m_sourceDataTable.Rows.First().Field<DataRowContainer>("DataRowContainer");
        
        dataRowContainer1.Dispose();
        
        var dataRowContainer2 = m_sourceDataTable.Rows.First().Field<DataRowContainer>("DataRowContainer");
    }
        
   
        
    private class DataOwner : IDataOwner
    {
    }

    internal static DataRowContainer CreateTestRowContainer(int id)
    {
        var dataRowContainer = new DataRowContainer();

        var container = new DataColumnContainer() { ColumnName = Fields.id, Type = TableStorageType.Int32, IsReadOnly = true};
        
        var metadataProps = new CoreContainerMetadataProps("Test",
            new [] {container},
            new Map<string, CoreDataColumnContainer>() { {Fields.id, container} }, 
            Array.Empty<string>(), 
            0);
        
        var containerProps = new ContainerDataProps(-1, _.List((object)id));
            
        dataRowContainer.Init(metadataProps, containerProps);

        return dataRowContainer;
    }

    internal static void FillTable(DataTable table, bool reverseColumns = true)
    {
        DataTable.RegisterUserTypeStringMethods((s) => s.ToXml().ToString(), s => ComplexUserClass.LoadFromXml(XElement.Parse(s)));
        
        DataTable.RegisterUserType<ComplexPoint2D>();
        DataTable.RegisterUserType<ComplexUserClass>();
        
        var actions = new Data<Action>()
        {
             () => table.AddColumn(Fields.id, TableStorageType.Int32, unique: true),
             () => table.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true),
             () => table.AddColumn(Fields.Name, TableStorageType.String),
             () => table.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime, serviceColumn: true),
             () => table.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime, serviceColumn: true),
             () => table.AddColumn(Fields.Directories.LmId, TableStorageType.Int32, serviceColumn: true),
             () => table.AddColumn(Fields.groupid, TableStorageType.Int32, builtin: true),
             () => table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid),
             () => table.AddColumn(Fields.Directories.Code, TableStorageType.String),
             () => table.AddColumn("Expr1", TableStorageType.String, dataExpression: "name + ' ' + id"),
             () => table.AddColumn("Expr2", TableStorageType.Int32, dataExpression: "id + id"),
             () => table.AddColumn("Default", TableStorageType.String, defaultValue: "test4312"),
             () => table.AddColumn<ComplexUserClass>("UserClass"),
             () => table.AddColumn<ComplexPoint2D>("Point2D"),
             () => table.AddColumn<DataRowContainer>("DataRowContainer"),
             () => table.AddColumn(Fields.Directories.Data, TableStorageType.Double, TableStorageTypeModifier.Array),
             () => table.AddColumn(Fields.Directories.DataTypes, TableStorageType.Type, TableStorageTypeModifier.Array),
             () => table.AddColumn(Fields.Directories.TimeRange, TableStorageType.TimeSpan, TableStorageTypeModifier.Range),
 
             () => table.AddIndex("UserClass", true),
             () => table.AddIndex("Point2D"),
 
             () => table.GetColumn(Fields.Code).SetXProperty(DataTable.StringIndexCaseSensitiveXProp, false),
 
             () => table.AddColumn(TableStorageType.SByte.ToString(), TableStorageType.SByte, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Byte.ToString(), TableStorageType.Byte, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.UInt16.ToString(), TableStorageType.UInt16, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.UInt32.ToString(), TableStorageType.UInt32, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.UInt64.ToString(), TableStorageType.UInt64, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Int16.ToString(), TableStorageType.Int16, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Int32.ToString(), TableStorageType.Int32, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Int64.ToString(), TableStorageType.Int64, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Decimal.ToString(), TableStorageType.Decimal, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Single.ToString(), TableStorageType.Single, auto: true, allowNull: false),
             () => table.AddColumn(TableStorageType.Double.ToString(), TableStorageType.Double, auto: true, allowNull: false),
 
             () => table.AddColumn(TableStorageType.BigInteger.ToString(), TableStorageType.BigInteger),
             () => table.AddColumn(TableStorageType.Type.ToString(), TableStorageType.Type),
             () => table.AddColumn(TableStorageType.Json.ToString(), TableStorageType.Json),
             () => table.AddColumn(TableStorageType.Xml.ToString(), TableStorageType.Xml),
             () => table.AddColumn(TableStorageType.Byte + "." + TableStorageTypeModifier.Array, TableStorageType.Byte, TableStorageTypeModifier.Array),
             () => table.AddColumn(TableStorageType.Char + "." + TableStorageTypeModifier.Array, TableStorageType.Char, TableStorageTypeModifier.Array),
             () => table.AddColumn(TableStorageType.Boolean.ToString(), TableStorageType.Boolean),
             () => table.AddColumn(TableStorageType.Uri.ToString(), TableStorageType.Uri),
             () => table.AddColumn(TableStorageType.Char.ToString(), TableStorageType.Char),
        };

        var load = table.BeginLoad();

        if (reverseColumns)
        {
            foreach (var action in actions)
            {
                action();
            }
        }
        else
        {
            actions.Reverse();
            
            foreach (var action in actions)
            {
                action();
            }
        }

        foreach (var modifier in Enum.GetValues<TableStorageTypeModifier>())
        {
            if (modifier == TableStorageTypeModifier.Complex)
            {
                continue;
            }
            
            foreach (var type in Enum.GetValues<TableStorageType>())
            {
                if (type is TableStorageType.Empty or TableStorageType.Object or TableStorageType.UserType)
                {
                    continue;
                }

                if (modifier == TableStorageTypeModifier.Range && (int)type > (int)TableStorageType.DateTimeOffset)
                {
                    continue;
                }

                var column = type + "." + modifier;
                
                if (table.ContainsColumn(column) == false)
                {
                    table.AddColumn(column, type, modifier);
                }
            }
        }

        table.AddIndex(Fields.Code);
        table.AddIndex(Fields.Directories.TimeRange);
        table.SetPrimaryKeyColumn(Fields.id);

        table.Capacity = initialCapacity;
            
        Assert.True(table.CanAcceptChanges);
        Assert.True(table.CanRejectChanges);

        var nameColumn = table.GetColumn(Fields.Name);

        nameColumn.SetXProperty("Test", true);
        nameColumn.SetXProperty("RC", CreateTestRowContainer(1));
        
        table.SetXProperty("TableTest", true);
            
        var bigInteger = new BigInteger(ulong.MaxValue) * new BigInteger(ulong.MaxValue);

        Action<DataRowContainer, string, object, Set<string>> setNewRow = (r, f, v, s) =>
        {
            r[f] = v;

            s.Add(f);
        }; 
        
        Action<DataRow, string, object, Set<string>> setRow = (r, f, v, s) =>
        {
            r[f] = v;

            s.Add(f);
        }; 
        
        for (int i = rowCount; i > 0; i--)
        {
            var id = i;

            var newRowFieldSet = new Set<string>();
            
            DataRowContainer newRow = table.NewRow();
                
            Assert.AreEqual(RowState.New, newRow.RowRecordState);

            setNewRow(newRow, Fields.id, id, newRowFieldSet);
            setNewRow(newRow, Fields.Name,"Name " + id, newRowFieldSet);

            setNewRow(newRow, Fields.Directories.CreateDt, m_timeNow.AddMilliseconds(i), newRowFieldSet);
            setNewRow(newRow, Fields.Directories.LmDt, m_timeNow.AddSeconds(i), newRowFieldSet);
            setNewRow(newRow, Fields.Directories.LmId, -i, newRowFieldSet);
            setNewRow(newRow, Fields.groupid, -i, newRowFieldSet);
            setNewRow(newRow, Fields.Directories.Guid, m_guid, newRowFieldSet);
            setNewRow(newRow, Fields.Directories.Code, "Code " + id % 2, newRowFieldSet);
            setNewRow(newRow, "UserClass", new ComplexUserClass() { ID = id }, newRowFieldSet);
            setNewRow(newRow, "Point2D", new ComplexPoint2D(id % 2, id % 3), newRowFieldSet);
            setNewRow(newRow, "DataRowContainer", CreateTestRowContainer(id), newRowFieldSet);
            setNewRow(newRow, Fields.Directories.Data, new double[] {1,2,3}, newRowFieldSet);
            setNewRow(newRow, Fields.Directories.DataTypes, new Type[] {typeof(object),typeof(int),typeof(DataTable)}, newRowFieldSet);
            setNewRow(newRow, Fields.Directories.TimeRange, new Range<TimeSpan>(TimeSpan.FromHours(1 + i), TimeSpan.FromHours(1 + i) + TimeSpan.FromSeconds(1 + i)), newRowFieldSet);
                
                
            newRow.SetXProperty("TEST", id.ToString());
            newRow.SetXProperty("Point2D", new ComplexPoint2D(id % 2, id % 3));
            newRow.SetRowInfo("TEST INFO");
            newRow.SetXPropertyAnnotation("TEST", "Info", "This is test property");
            
            setNewRow(newRow, TableStorageType.BigInteger.ToString(), bigInteger, newRowFieldSet);
            setNewRow(newRow, TableStorageType.Type.ToString(),typeof(object), newRowFieldSet);
            setNewRow(newRow, TableStorageType.Json.ToString(), new JsonObject() { {"ID", "max" }}, newRowFieldSet);
            setNewRow(newRow, TableStorageType.Xml.ToString(), new XElement("Root",
                new XElement("Child1", 1, new XAttribute("ChildAttr", "test_1")),
                new XElement("Child1", 2, new XAttribute("ChildAttr", "test_2")),
                new XElement("Child1", 3, new XAttribute("ChildAttr", "test_3")),
                new XElement("Child2", 4, new XAttribute("ChildAttr", "test_4")),
                new XElement("Child2", 5, new XAttribute("ChildAttr", "test_5")),
                new XElement("Child2", 6, new XAttribute("ChildAttr", "test_6")),
                new XAttribute("Attr1", "test1"),
                new XAttribute("Attr2", "test2")
            ), newRowFieldSet);
            setNewRow(newRow, TableStorageType.Boolean.ToString(),true, newRowFieldSet);
            setNewRow(newRow, TableStorageType.Uri.ToString(),new Uri("https://microsoft.com"), newRowFieldSet);
            setNewRow(newRow, TableStorageType.Byte + "." + TableStorageTypeModifier.Array, Encoding.UTF8.GetBytes("TableStorageType.ByteArray.ToString()"), newRowFieldSet);
            setNewRow(newRow, TableStorageType.Char+ "." + TableStorageTypeModifier.Array, "false".ToCharArray(), newRowFieldSet);
            setNewRow(newRow, TableStorageType.Char.ToString(),'f', newRowFieldSet);
            setNewRow(newRow, TableStorageType.Boolean.ToString(), "true", newRowFieldSet);
            
            var ir = new CoreDataRowDebugView(newRow).Items;

            Assert.True(ir.Length > 0);
                
            var dataRow = table.AddRow(newRow);
            
            var ird = new DataRowDebugView(newRow).Items;
            
            Assert.True(ird.Length > 0);
                
            Assert.True(dataRow.IsValidRowState);
            Assert.True(dataRow.IsAddedRow);
            Assert.True(dataRow.IsModifiedOrAdded);
                
            Assert.False(dataRow.IsModified);
            Assert.False(dataRow.IsDeletedRow);
            Assert.False(dataRow.IsDetachedRow);
            Assert.False(dataRow.IsRowInTransaction);
                
            Assert.True(new ComplexPoint2D(id % 2, id % 3).Equals(dataRow.Field<ComplexPoint2D>("Point2D")));
            Assert.True(new ComplexPoint2D(id % 2, id % 3).Equals((ComplexPoint2D)dataRow["Point2D"]));
            Assert.True(new ComplexPoint2D(id % 2, id % 3).Equals(dataRow.GetXProperty<ComplexPoint2D>("Point2D")));

            var rowContainer = dataRow.Field<DataRowContainer>("DataRowContainer");
            
            Assert.True(CreateTestRowContainer(id).Equals(rowContainer));

            Assert.AreEqual(id.ToString(), dataRow.GetXProperty<string>("TEST"));
            Assert.AreEqual(0, dataRow.GetXProperty<int>(Guid.NewGuid().ToString()));
            Assert.AreEqual(string.Empty, dataRow.GetXProperty<string>(Guid.NewGuid().ToString()));
            Assert.AreEqual(newRow.GetRowInfo(), dataRow.GetRowInfo());
            
            Assert.True(Tool.ArraysDeepEqual(newRow.Field<double[]>(Fields.Directories.Data), dataRow.Field<double[]>(Fields.Directories.Data), (x, y) => x == y));
            Assert.True(Tool.ArraysDeepEqual(newRow.Field<Type[]>(Fields.Directories.DataTypes), dataRow.Field<Type[]>(Fields.Directories.DataTypes), (x, y) => x == y));
            Assert.AreEqual(newRow.Field<Range<TimeSpan>>(Fields.Directories.TimeRange), dataRow.Field<Range<TimeSpan>>(Fields.Directories.TimeRange));
            
            dataRow.SetRowInfo(string.Empty);
            
            var rowFieldSet = new Set<string>();
                
            setRow(dataRow, Fields.Name, "Name " + id, rowFieldSet);
            setRow(dataRow, Fields.Directories.CreateDt, m_timeNow.AddMilliseconds(i), rowFieldSet);
            setRow(dataRow, Fields.Directories.LmDt, m_timeNow.AddDays(i), rowFieldSet);
            setRow(dataRow, Fields.Directories.LmId,  -i, rowFieldSet);
            setRow(dataRow, Fields.groupid,  -i, rowFieldSet);
            setRow(dataRow, Fields.Directories.Guid,  m_guid, rowFieldSet);
            setRow(dataRow, Fields.Directories.Code,  "Code " + id % 2, rowFieldSet);
            setRow(dataRow, "UserClass", new ComplexUserClass() { ID = id }, rowFieldSet);
            setRow(dataRow, "Point2D",  new ComplexPoint2D(id % 2, id % 3), rowFieldSet);
                
            setRow(dataRow, "DataRowContainer", CreateTestRowContainer(id), rowFieldSet);          
            
            dataRow.Set(TableStorageType.BigInteger.ToString(), bigInteger);
            dataRow.Set(TableStorageType.Type.ToString(), typeof(object));
            dataRow.Set(TableStorageType.Json.ToString(), new JsonObject() { { "ID", "max" } });

            if (i + 1 % 13 == 0)
            {
                dataRow.Set(TableStorageType.Xml.ToString(), new XElement("max"));
            }
            
            dataRow.Set(TableStorageType.Boolean.ToString(), true);
            dataRow.Set(TableStorageType.Uri.ToString(), new Uri("https://microsoft.com"));
            dataRow.Set(TableStorageType.Byte + "." + TableStorageTypeModifier.Array, Encoding.UTF8.GetBytes("TableStorageType.ByteArray.ToString()"));
            dataRow.Set(TableStorageType.Char + "." + TableStorageTypeModifier.Array, "false".ToCharArray());
            dataRow.Set(TableStorageType.Char.ToString(), 'f');

            dataRow[TableStorageType.Boolean.ToString()] = "true";
            dataRow.Set(TableStorageType.Boolean.ToString(), "true");
            dataRow.Set(TableStorageType.Boolean.ToString(), 1);
                
            dataRow.SetXProperty("Point2D_", new ComplexPoint2D(id % 2, id % 3));
            dataRow.SetXPropertyAnnotation("Point2D_", "Point2D_", new ComplexPoint2D(id % 2, id % 3));
                
            Assert.True(new ComplexPoint2D(id % 2, id % 3).Equals(dataRow.GetXProperty<ComplexPoint2D>("Point2D_")));
                  
            dataRow.SilentlySetValue(TableStorageType.BigInteger.ToString(), bigInteger);
            dataRow.SilentlySetValue(TableStorageType.Type.ToString(), typeof(object));
            dataRow.SilentlySetValue(TableStorageType.Json.ToString(), new JsonObject() { { "ID", "max" } });

            if (i + 1 % 13 == 0)
            {
                dataRow.SilentlySetValue(TableStorageType.Xml.ToString(), new XElement("max"));
            }

            dataRow.SilentlySetValue(TableStorageType.Boolean.ToString(), true);
            dataRow.SilentlySetValue(TableStorageType.Uri.ToString(), new Uri("https://microsoft.com"));
            dataRow.SilentlySetValue(TableStorageType.Byte + "." + TableStorageTypeModifier.Array, Encoding.UTF8.GetBytes("TableStorageType.ByteArray.ToString()"));
            dataRow.SilentlySetValue(TableStorageType.Char+ "." + TableStorageTypeModifier.Array, "false".ToCharArray());
            dataRow.SilentlySetValue(TableStorageType.Char.ToString(), 'f');
            dataRow.SilentlySetValue("UserClass", new ComplexUserClass() { ID = id });
            dataRow.SilentlySetValue("Point2D", new ComplexPoint2D(id % 2, id % 3));
                
            dataRow.SilentlySetValue("DataRowContainer", CreateTestRowContainer(id));

            dataRow.SilentlySetValue(TableStorageType.Boolean.ToString(), "true");
            dataRow.SilentlySetValue(TableStorageType.Boolean.ToString(), 1);

            Assert.True(dataRow.IsAddedRow);
        }

        load.EndLoad();
            
        var transaction = table.StartTransaction();
            
        Assert.False(table.CanAcceptChanges);
        Assert.False(table.CanRejectChanges);

        var maxId = (int)table.Max(Fields.id) + 1000;

        var toRemove = new List<DataRow>();
            
        for (int i = 1; i < rowCount; i++)
        {
            var localTran = table.StartTransaction();

            var id = i + maxId;

            var newRow = table.NewRow();

            newRow[Fields.id] = id;
            newRow[Fields.Name] = "Name " + id;
            newRow[Fields.Directories.CreateDt] = DateTime.Now;
            newRow[Fields.Directories.LmDt] = DateTime.Now;
            newRow[Fields.Directories.LmId] = -i;
            newRow[Fields.groupid] = -i;
            newRow[Fields.Directories.Guid] = Guid.NewGuid();
            newRow[Fields.Directories.Code] = "Code " + id % 2;
            newRow["UserClass"] = new ComplexUserClass() { ID = id };
                
            if (i % 2 != 0)
            {
                newRow.SetXProperty("TEST", id + " REJECTED");
            }
            else
            {
                newRow.SetXProperty("TEST", id + " COMMITTED");
            }
                
            newRow.SetRowInfo("TEST INFO");
                
            newRow[TableStorageType.BigInteger.ToString()] = BigInteger.MinusOne;
            newRow[TableStorageType.Type.ToString()] = typeof(DataTable);
            newRow[TableStorageType.Json.ToString()] = new JsonObject() { { "ID", "min" } };
            newRow[TableStorageType.Boolean.ToString()] = false;

            if (i % 2 != 0)
            {
                newRow[TableStorageType.Xml.ToString()] = new XElement("min");
            }
                
            var dataRow = table.AddRow(newRow);
                
            Assert.True(dataRow.IsValidRowState);
            Assert.True(dataRow.IsAddedRow);
            Assert.True(dataRow.IsModifiedOrAdded);
            Assert.True(dataRow.IsChangedRow);
                
            Assert.False(dataRow.IsModified);
            Assert.False(dataRow.IsDeletedRow);
            Assert.False(dataRow.IsDetachedRow);
                
            Assert.True(dataRow.IsRowInTransaction);
                
            if (i % 2 == 0)
            {
                localTran.Commit();
                    
                Assert.True(dataRow.IsAddedRow);
                    
                Assert.True(dataRow.IsRowInTransaction);
                    
                Assert.AreEqual(newRow.GetXProperty<string>("TEST"), dataRow.GetXProperty<string>("TEST"));
                Assert.AreEqual(newRow.GetRowInfo(), dataRow.GetRowInfo());
                    
                dataRow.SetRowInfo(string.Empty);
                    
                toRemove.Add(dataRow);
            }
            else
            {
                localTran.Rollback();
                    
                Assert.True(dataRow.IsDetachedRow);
            }
        }

        transaction.Commit();
            
        maxId = (int)table.Max(Fields.id);

        foreach (var row in toRemove)
        {
            row.Delete();
                
            Assert.True(row.IsDetachedRow);
            Assert.False(row.IsChangedRow);
        }

        var dataTableRows = new Data<IDataTableRow>();

        for (int i = 1; i < 5; i++)
        {
            var id = i + maxId;

            var newRow = table.NewRow();

            newRow[Fields.id] = id;
            newRow[Fields.Name] = "Name " + id;
            newRow[Fields.Directories.CreateDt] = DateTime.Now;
            newRow[Fields.Directories.LmDt] = DateTime.Now;
            newRow[Fields.Directories.LmId] = -i;
            newRow[Fields.groupid] = -i;
            newRow[Fields.Directories.Guid] = Guid.NewGuid();
            newRow[Fields.Directories.Code] = "Code " + id % 2;
            newRow["UserClass"] = new ComplexUserClass() { ID = id };
                
            newRow.SetXProperty("TEST", id.ToString());

            newRow.SetRowInfo("DELETED TEST INFO");
                
            var dataRow = table.AddRow(newRow);

                
            Assert.AreEqual(id.ToString(), dataRow.GetXProperty<string>("TEST"));
            Assert.AreEqual(newRow.GetRowInfo(), dataRow.GetRowInfo());

            Assert.True(dataRow.IsAddedRow);
                
            dataTableRows.Add(dataRow);
        }
        
        table.AcceptChanges();

        foreach (var row in dataTableRows)
        {
            Assert.True(row.IsUnchangedRow);

            row.Delete();
                
            Assert.True(row.IsDeletedRow);
            Assert.True(row.IsChangedRow);
            Assert.False(row.IsUnchangedRow);
        }

        var tableOwner = new DataOwner();
            
        table.Owner = tableOwner;
            
        Assert.AreEqual(table.Owner, tableOwner);
    }

    [TearDown]
    public void TearDown()
    {
        var disposeCalled = false;

        var tableName = m_sourceDataTable.Name;

        m_sourceDataTable.Disposed.Subscribe((a, c) =>
        {
            Assert.AreEqual(tableName, a.TableName);
            disposeCalled = true;
        });
            
        m_sourceDataTable.Dispose();
            
        Assert.True(disposeCalled);
    }

    [Test]
    public void TestTypedSetColumnGeneric()
    {
        var testTable = GetTestTable();

        var dataRow = testTable.Rows.First();

        var newGuid = Guid.NewGuid();

        dataRow.Set(dataRow.GetColumn(TableStorageType.Guid.ToString()), newGuid);

        Assert.AreEqual(newGuid, dataRow.Field<Guid>(testTable.GetColumn(TableStorageType.Guid.ToString()), Guid.Empty));
        Assert.AreEqual(newGuid, dataRow.Field<Guid>(dataRow.GetColumn(TableStorageType.Guid.ToString()), Guid.Empty));
        Assert.AreEqual(newGuid, dataRow.Field<Guid>(new ColumnHandle(testTable.GetColumn(TableStorageType.Guid.ToString()).ColumnHandle), Guid.Empty));

        dataRow.SetNull(dataRow.GetColumn(TableStorageType.Guid.ToString()));
            
        Assert.AreEqual(Guid.Empty, dataRow.Field<Guid>(testTable.GetColumn(TableStorageType.Guid.ToString()), Guid.Empty));
        Assert.AreEqual(Guid.Empty, dataRow.Field<Guid>(dataRow.GetColumn(TableStorageType.Guid.ToString()), Guid.Empty));
        Assert.AreEqual(Guid.Empty, dataRow.Field<Guid>(new ColumnHandle(testTable.GetColumn(TableStorageType.Guid.ToString()).ColumnHandle), Guid.Empty));

    }

    [Test]
    public void TestTypedSet()
    {
        var testTable = GetTestTable();

        var dataRow = testTable.Rows.First();

        var xml = new XElement("TEST_123");
            
        dataRow.Set(testTable.GetColumn(TableStorageType.Xml.ToString()), xml);
            
        Assert.AreEqual(xml, dataRow.Field<XElement>(TableStorageType.Xml.ToString()));

        var json = new JsonObject() { { "Prop", "Test_123" } };
            
        dataRow.Set(testTable.GetColumn(TableStorageType.Json.ToString()), json);
            
        Assert.AreEqual(json, dataRow.Field<JsonObject>(TableStorageType.Json.ToString()));
            
        var uri = new Uri("https://google.com");
            
        dataRow.Set(testTable.GetColumn(TableStorageType.Uri.ToString()), uri);
            
        Assert.AreEqual(uri, dataRow.Field<Uri>(TableStorageType.Uri.ToString()));
            
        var type = typeof(Guid);
            
        dataRow.Set(testTable.GetColumn(TableStorageType.Type.ToString()), type);
            
        Assert.AreEqual(type, dataRow.Field<Type>(TableStorageType.Type.ToString()));
            
        var bytes = Encoding.UTF8.GetBytes("TEST");
            
        dataRow.Set(testTable.GetColumn(TableStorageType.Byte + "." + TableStorageTypeModifier.Array), bytes);
            
        Assert.AreEqual(bytes, dataRow.Field<byte[]>(TableStorageType.Byte+ "." + TableStorageTypeModifier.Array));
            
        var charArr = "TEST".ToCharArray();
            
        dataRow.Set(testTable.GetColumn(TableStorageType.Char + "." + TableStorageTypeModifier.Array), charArr);
            
        Assert.AreEqual(charArr, dataRow.Field<char[]>(TableStorageType.Char+ "." + TableStorageTypeModifier.Array));
    }
        
    [Test]
    public void TestTypedSet2()
    {
        var testTable = GetTestTable();

        var clone = testTable.Clone();

        var dataRow = testTable.Rows.First();

        var xml = new XElement("TEST_123");
            
        dataRow.Set((IDataTableColumn)testTable.GetColumn(TableStorageType.Xml.ToString()), xml);
            
        Assert.AreEqual(xml, dataRow.Field<XElement>(TableStorageType.Xml.ToString()));
            
        var json = new JsonObject() {{"Prop", "Test_123"}};
            
        dataRow.Set((IDataTableColumn)clone.GetColumn(TableStorageType.Json.ToString()), json);
            
        Assert.AreEqual(json, dataRow.Field<JsonObject>(TableStorageType.Json.ToString()));
            
        var uri = new Uri("https://google.com");
            
        dataRow.Set((IDataTableColumn)testTable.GetColumn(TableStorageType.Uri.ToString()), uri);
            
        Assert.AreEqual(uri, dataRow.Field<Uri>(TableStorageType.Uri.ToString()));
            
        var type = typeof(Guid);
            
        dataRow.Set((IDataTableColumn)clone.GetColumn(TableStorageType.Type.ToString()), type);
            
        Assert.AreEqual(type, dataRow.Field<Type>(TableStorageType.Type.ToString()));
            
        var bytes = Encoding.UTF8.GetBytes("TEST");
            
        dataRow.Set((IDataTableColumn)testTable.GetColumn(TableStorageType.Byte + "." + TableStorageTypeModifier.Array), bytes);
            
        Assert.AreEqual(bytes, dataRow.Field<byte[]>(TableStorageType.Byte + "." + TableStorageTypeModifier.Array));
            
        var charArr = "TEST".ToCharArray();
            
        dataRow.Set((IDataTableColumn)clone.GetColumn(TableStorageType.Char + "." + TableStorageTypeModifier.Array), charArr);
            
        Assert.AreEqual(charArr, dataRow.Field<char[]>(TableStorageType.Char + "." + TableStorageTypeModifier.Array));
    }

    [Test]
    public void TestTypedSetUsingHandle()
    {
        var testTable = GetTestTable();

        var dataRow = testTable.Rows.First();

        var xml = new XElement("TEST_123");

        dataRow.Set(new ColumnHandle(testTable.GetColumn(TableStorageType.Xml.ToString()).ColumnHandle), xml);

        Assert.AreEqual(xml, dataRow.Field<XElement>(TableStorageType.Xml.ToString()));

        var json = new JsonObject() { { "Prop", "Test_123" } };

        dataRow.Set(new ColumnHandle(testTable.GetColumn(TableStorageType.Json.ToString()).ColumnHandle), json);
            
        Assert.AreEqual(json, dataRow.Field<JsonObject>(TableStorageType.Json.ToString()));
            
        var uri = new Uri("https://google.com");
            
        dataRow.Set(new ColumnHandle(testTable.GetColumn(TableStorageType.Uri.ToString()).ColumnHandle), uri);
            
        Assert.AreEqual(uri, dataRow.Field<Uri>(TableStorageType.Uri.ToString()));
            
        var type = typeof(Guid);
            
        dataRow.Set(new ColumnHandle(testTable.GetColumn(TableStorageType.Type.ToString()).ColumnHandle), type);
            
        Assert.AreEqual(type, dataRow.Field<Type>(TableStorageType.Type.ToString()));
            
        var bytes = Encoding.UTF8.GetBytes("TEST");
            
        dataRow.Set(new ColumnHandle(testTable.GetColumn(TableStorageType.Byte + "." + TableStorageTypeModifier.Array).ColumnHandle), bytes);
            
        Assert.AreEqual(bytes, dataRow.Field<byte[]>(TableStorageType.Byte + "." + TableStorageTypeModifier.Array));
            
        var charArr = "TEST".ToCharArray();
            
        dataRow.Set(new ColumnHandle(testTable.GetColumn(TableStorageType.Char + "." + TableStorageTypeModifier.Array).ColumnHandle), charArr);
            
        Assert.AreEqual(charArr, dataRow.Field<char[]>(TableStorageType.Char + "." + TableStorageTypeModifier.Array));
    }
        
    [Test]
    public void TestTypedSetString()
    {
        var testTable = GetTestTable();

        var dataRow = testTable.Rows.First();

        var xml = new XElement("TEST_123");
            
        dataRow.Set(TableStorageType.Xml.ToString(), xml);
            
        Assert.AreEqual(xml, dataRow.Field<XElement>(TableStorageType.Xml.ToString()));

        var json = new JsonObject() { { "Prop", "Test_123" } };
            
        dataRow.Set(TableStorageType.Json.ToString(), json);
            
        Assert.AreEqual(json, dataRow.Field<JsonObject>(TableStorageType.Json.ToString()));
            
        var uri = new Uri("https://google.com");
            
        dataRow.Set(TableStorageType.Uri.ToString(), uri);
            
        Assert.AreEqual(uri, dataRow.Field<Uri>(TableStorageType.Uri.ToString()));
            
        var type = typeof(Guid);
            
        dataRow.Set(TableStorageType.Type.ToString(), type);
            
        Assert.AreEqual(type, dataRow.Field<Type>(TableStorageType.Type.ToString()));
            
        var bytes = Encoding.UTF8.GetBytes("TEST");
            
        dataRow.Set(TableStorageType.Byte + "." + TableStorageTypeModifier.Array, bytes);
            
        Assert.AreEqual(bytes, dataRow.Field<byte[]>(TableStorageType.Byte + "." + TableStorageTypeModifier.Array));
            
        var charArr = "TEST".ToCharArray();
            
        dataRow.Set(TableStorageType.Char + "." + TableStorageTypeModifier.Array, charArr);
            
        Assert.AreEqual(charArr, dataRow.Field<char[]>(TableStorageType.Char + "." + TableStorageTypeModifier.Array));
    }

    [Test]
    public void TestRowChangedColumns1()
    {
        var table = GetTestTable();

        var clone = table.Clone();

        clone.ImportRows(table.Rows.Take(1));

        foreach (var dataRow in clone.Rows)
        {
            dataRow.SetModified();

            foreach (var column in clone.GetColumns())
            {
                if (string.IsNullOrEmpty(column.Expression) == false)
                {
                    continue;
                }

                if (column.IsServiceColumn)
                {
                    continue;
                }

                if (dataRow.IsNotNull(column))
                {
                    Assert.True(dataRow.IsChanged(column), column.ColumnName);
                    Assert.True(dataRow.IsChanged(column.ColumnName));
                    Assert.True(dataRow.IsChanged(new ColumnHandle(column.ColumnHandle)));
                }
            }
        }
    }

    [Test]
    public void TestRowChangedColumns2()
    {
        var table = GetTestTable();

        var clone = table.Clone();

        var beforeImportState = table.Rows.First().RowRecordState;

        clone.ImportRows(table.Rows.Take(1));

        var first = clone.Rows.First();

        var afterImportState = first.RowRecordState;

        var afterImportChangedFields = first.GetChangedFields().ToArray();

        Action<DataRow> f1 = (r) => r.Set(Fields.Code, Guid.NewGuid());
        Action<DataRow> f2 = (r) => r.Set(Fields.Code, "12312312312");
        Action<DataRow> f3 = (r) => r.Set(Fields.Code, (object)"4124");

        var updates = new Action<DataRow>[3]
        {
            f1, f2, f3
        };
        
        var codeColArray = table.GetColumn(Fields.Code).SingleToArray();

        for (int i = 0; i < updates.Length; i++)
        {
            clone.AcceptChanges();

            updates[i](first);
            
            var changedFields = first.GetChangedFields();
            var changes = first.GetChangedFields(codeColArray);

            Assert.True(changedFields.Any());
            Assert.True(changes.Any());
        }
        
        Assert.AreEqual(beforeImportState, afterImportState);
        Assert.IsEmpty(afterImportChangedFields);
    }

    [Test]
    public void TestRowChangedColumns3()
    {
        var table = GetTestTable();

        var clone = table.Clone();

        clone.ImportRows(table.Rows.Take(1));

        clone.AcceptChanges();

        foreach (var dataRow in clone.Rows)
        {
            foreach (var column in clone.GetColumns())
            {
                Assert.False(dataRow.IsChanged(column), column.ColumnName);
                Assert.False(dataRow.IsChanged(column.ColumnName));
                Assert.False(dataRow.IsChanged(new ColumnHandle(column.ColumnHandle)));
            }
        }

        var row = clone.Rows.First();

        Assert.AreEqual(0, row.GetChangedFields().Count());
        Assert.AreEqual(0, ((IDataRowReadOnlyAccessor)row).GetChangedFields().Count());
    }

    [Test]
    public void TestRowChangedColumns4()
    {
        var table = GetTestTable();

        var clone = table.Clone();
        
        clone.ImportRows(table.Rows.Take(1));
        
        clone.AcceptChanges();

        var row = clone.Rows.First();

        row.Set(Fields.Code, "SomeValue");

        Assert.AreEqual(1, row.GetChangedFields().Count());
        Assert.AreEqual(1, ((IDataRowReadOnlyAccessor)row).GetChangedFields().Count());

        foreach (var column in clone.GetColumns())
        {
            if (column.ColumnName == Fields.Code)
            {
                Assert.True(row.IsChanged(column), column.ColumnName);
                Assert.True(row.IsChanged(column.ColumnName));
                Assert.True(row.IsChanged(new ColumnHandle(column.ColumnHandle)));

                continue;
            }

            Assert.False(row.IsChanged(column), column.ColumnName);
            Assert.False(row.IsChanged(column.ColumnName));
            Assert.False(row.IsChanged(new ColumnHandle(column.ColumnHandle)));
        }
    }

    [Test]
    public void TestRowChangedColumns5()
    {
        var table = GetTestTable();

        var clone = table.Clone();
        
        clone.ImportRows(table.Rows.Take(1));

        clone.AcceptChanges();

        var row = clone.Rows.First();

        var columnCodeAge1 = row.GetColumnAge(clone.GetColumn(Fields.Code));
        var columnNameAge1 = row.GetColumnAge(clone.GetColumn(Fields.Name));

        Assert.AreEqual(clone.DataAge, row.GetColumnAge(clone.GetColumn("Expr1")));
        Assert.AreEqual(clone.DataAge, row.GetColumnAge("Expr1"));

        ((IDataRowAccessor)row)[clone.GetColumn(Fields.Code)] = null;

        var columnCodeAge2 = row.GetColumnAge(new ColumnHandle(clone.GetColumn(Fields.Code).ColumnHandle));
        var columnNameAge2 = row.GetColumnAge(new ColumnHandle(clone.GetColumn(Fields.Name).ColumnHandle));

        Assert.AreEqual(columnCodeAge1 + 1, columnCodeAge2);
        Assert.AreEqual(columnNameAge1, columnNameAge2);
    }

    [Test]
    public void TestRowIsNull()
    {
        var table = GetTestTable();

        var clone = table.Clone();
        
        clone.ImportRows(table.Rows.Take(1));

        clone.AcceptChanges();

        var cloneRow = clone.Rows.First();

        cloneRow.Set(Fields.Code, "SomeValue");
        
        clone.AcceptChanges();
        
        cloneRow.SetNull(Fields.Code);

        Assert.True(cloneRow.IsNull(clone.GetColumn(Fields.Code)));
        Assert.True(cloneRow.IsNull((IDataTableReadOnlyColumn)clone.GetColumn(Fields.Code)));
        Assert.False(cloneRow.IsNotNull((IDataTableReadOnlyColumn)clone.GetColumn(Fields.Code)));
        Assert.False(cloneRow.IsNotNull(clone.GetColumn(Fields.Code)));

        Assert.True(cloneRow.IsNull(clone.GetColumn(Fields.Code)));
        Assert.True(cloneRow.IsNull((IDataTableReadOnlyColumn)table.GetColumn(Fields.Code)));
       
        Assert.False(cloneRow.IsNotNull((IDataTableReadOnlyColumn)clone.GetColumn(Fields.Code)));
        Assert.False(cloneRow.IsNotNull(clone.GetColumn(Fields.Code)));
        Assert.False(cloneRow.IsNotNull(new ColumnHandle(clone.GetColumn(Fields.Code).ColumnHandle)));
    }
    
     [Test]
    public void TestRowOriginalValue()
    {
        var table = GetTestTable();

        var clone = table.Clone();
        
        clone.ImportRows(table.Rows.Take(1));

        clone.AcceptChanges();

        var cloneRow = clone.Rows.First();

        cloneRow.Set(Fields.Code, "SomeValue");
        
        clone.AcceptChanges();
        
        cloneRow.SetNull(Fields.Code);

        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue(table.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue(clone.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue<string>(clone.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue<string>(table.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue(new ColumnHandle(table.GetColumn(Fields.Code).ColumnHandle)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue((IDataTableColumn)table.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue((IDataTableReadOnlyColumn)table.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue<string>((IDataTableReadOnlyColumn)clone.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue<string>((IDataTableReadOnlyColumn)clone.GetColumn(Fields.Code)));
        Assert.AreEqual("SomeValue", cloneRow.GetOriginalValue<string>(Fields.Code));
    }

    [Test]
    public void TestRowContainerOriginalValue()
    {
        var table = GetTestTable();

        var clone = table.Clone();
        
        clone.ImportRows(table.Rows.Take(1));

        var cloneRow = clone.Rows.First();

        var container = cloneRow.ToContainer();

        cloneRow.DetachRow();

        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue(table.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue(clone.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue<string>(clone.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue<string>(table.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue(new ColumnHandle(table.GetColumn(Fields.Code).ColumnHandle)));
        Assert.AreEqual(container.GetOriginalValue<string>(container.GetColumn(Fields.Code)),
            cloneRow.GetOriginalValue((IDataTableColumn)table.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue((IDataTableReadOnlyColumn)table.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue<string>((IDataTableReadOnlyColumn)table.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code),
            cloneRow.GetOriginalValue<string>((IDataTableReadOnlyColumn)clone.GetColumn(Fields.Code)));
        Assert.AreEqual(container.GetOriginalValue<string>(Fields.Code), cloneRow.GetOriginalValue<string>(Fields.Code));

        var cloneRow1 = clone.GetRow(cloneRow.RowHandle);

        Assert.True(ReferenceEquals(cloneRow1, cloneRow));

        cloneRow.AttachRow(clone, cloneRow.RowHandle);

        Assert.True(ReferenceEquals(cloneRow1, cloneRow));
    }

    [Test]
    public void TestRowChanged8()
    {
        var table = GetTestTable();

        var clone = table.Clone();
        
        clone.ImportRows(table.Rows.Take(1));
        
        clone.AcceptChanges();

        var row = clone.Rows.First();

        var copy = row.Clone();

        copy.CopyChanges(row);

        Assert.AreEqual(copy[Fields.Code], row[Fields.Code]);

        Assert.AreEqual(RowState.Modified, copy.RowRecordState);

        var newGuid = Guid.NewGuid();

        Assert.AreEqual(newGuid, row.GetValueOrDefault<Guid>(newGuid.ToString(), newGuid));

        Assert.False(row.IsAddedRow);

        row.SetAdded();

        Assert.True(row.IsAddedRow);

        clone.AcceptChanges();

        copy.Set(Fields.Code, "New_Value");
        copy.Set(Fields.Name, "New name");

        Assert.AreNotEqual(copy[Fields.Code], row[Fields.Code]);
        Assert.AreNotEqual(copy[Fields.Name], row[Fields.Name]);
    }

    [Test]
    public void TestRowChangedColumns9()
    {
        var table = GetTestTable();

        var clone = table.Clone();

        var newRow = clone.NewRow();

        newRow.SetXProperty("Test1", 1);
        newRow.SetXProperty("Test2", 1);

        var fields = newRow.GetChangedFields().ToData();

        Assert.AreEqual(clone.ColumnCount, fields.Count());
        var cols = newRow.Columns.Select(c => c.ColumnName).ToArray();
        Assert.AreEqual(cols, fields);

        var xProps = newRow.GetChangedXProperties().ToData();

        Assert.AreEqual(2, xProps.Count());
        Assert.True(_.List("Test1", "Test2").SequenceEqual(xProps));

        Assert.IsNull(newRow.GetOriginalValue(Fields.id));
        Assert.IsNull(newRow.GetOriginalValue(Fields.Code));
        Assert.IsEmpty((string)newRow.GetOriginalValue<string>(Fields.Code));

        Assert.AreEqual(null, newRow.GetOriginalValue<int?>(Fields.id));
        Assert.AreEqual(0, newRow.GetOriginalValue<int>(Fields.id));
        Assert.AreEqual(0, newRow.GetOriginalValue<int>(newRow.GetColumn(Fields.id)));
    }

    [Test]
    public void TestDataAggrEvents([Values(true, false)] bool resetAggregate)
    {
        var table = m_sourceDataTable.Copy();

        var dataRow = table.GetRowBy(3);

        table.ColumnChanging.Subscribe((args, c) =>
        {
            args.IsCancel = args.ColumnName == Fields.Code;

            if (args.ColumnName == Fields.Name)
            {
                args.NewValue = args.NewValue + args.Row[Fields.id].ToString();
            }
        });

        var fieldChanged = new HashSet<string>();

        table.ColumnChanged.Subscribe((args, c) =>
        {
            foreach (var changedColumnName in args.ChangedColumnNames)
            {
                fieldChanged.Add(changedColumnName);
            }
        });
            
        var xPropsChanged = new HashSet<string>();
            
        table.RowXPropertyChanged.Subscribe((args, c) =>
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
            
        var lockEvents = dataRow.LockEvents();

        dataRow.Set(Fields.Name, 1);
            
        Assert.AreEqual(1.ToString(), dataRow.Field<string>(Fields.Name));
            
        dataRow.Set(Fields.Code, "Code");
            
        Assert.AreEqual("Code", dataRow.Field<string>(Fields.Code));

        dataRow.SetXProperty("Test1", 1);
        dataRow.SetXProperty("Test2", true);
        dataRow.SetXProperty("Test3", DateTime.Now.Date);
            
        Assert.AreEqual(1, dataRow.GetXProperty<int>("Test1"));
        Assert.AreEqual(true, dataRow.GetXProperty<bool>("Test2"));
        Assert.AreEqual(DateTime.Now.Date, dataRow.GetXProperty<DateTime>("Test3").ToLocalTime());
            
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
    public void TestRowClone()
    {
        var table = GetTestTable();

        foreach (var dataRow in table.Rows)
        {
            Assert.True(dataRow.HasVersion(DataRowVersion.Current));
        }
        
        table.AcceptChanges();

        foreach (var dataRow in table.Rows)
        {
            var clone = dataRow.Clone();
            
            Assert.True(dataRow.HasVersion(DataRowVersion.Current));
            Assert.False(dataRow.HasVersion(DataRowVersion.Original));
                
            Assert.IsNotEmpty((string)dataRow.DebugKeyValue);

            var dca = dataRow.GetColumns().Select(c => c.ColumnName).ToArray();
            var dcc = clone.GetColumns().Select(c => c.ColumnName).ToArray();
            
            Assert.AreEqual(dca, dcc);

            Assert.True(dataRow.GetTableColumnNames().SequenceEqual(clone.GetColumns().Select(c => c.ColumnName)));
            Assert.True(EqualsList(dataRow.GetPrimaryKeyColumns().Select(c => c.ColumnName).ToData(),clone.PrimaryKeyColumn.Select(c => c.ColumnName).ToData()));
            Assert.True(EqualsList(dataRow.GetRowKeyValue().ToData(),clone.GetRowKeyValue().ToData()));
                
            foreach (var column in table.GetColumns())
            {
                if (column.ColumnName == "Expr1")
                {
                    
                }

                var c_isNull = clone.IsNull(column.ColumnName);
                var d_isNull = dataRow.IsNull(column.ColumnName);

                Assert.True(c_isNull == d_isNull, column.ColumnName);
                Assert.True(clone.IsNull(column) == dataRow.IsNull(column.ColumnName), column.ColumnName);
                    
                Assert.True(clone.IsNotNull(column.ColumnName) == dataRow.IsNotNull(column.ColumnName));
                Assert.True(clone.IsNotNull(column) == dataRow.IsNotNull(column.ColumnName));

                Assert.AreEqual(dataRow[column, DataRowVersion.Current], clone[column.ColumnName]);
                Assert.AreEqual(dataRow[column], clone[column.ColumnName]);
                Assert.AreEqual(dataRow[new ColumnHandle(column.ColumnHandle)], clone[column.ColumnHandle]);
                Assert.AreEqual(dataRow[new ColumnHandle(column.ColumnHandle), DataRowVersion.Current], clone[column.ColumnHandle]);
                   
                var readOnlyRow = (IDataTableReadOnlyRow)dataRow;

                Assert.AreEqual(readOnlyRow[column], clone[column.ColumnName]);
                    
                var iRow = (IDataTableRow)dataRow;

                Assert.AreEqual(iRow[column], clone[column.ColumnName]);
                Assert.AreEqual(iRow[column.ColumnHandle], clone[column.ColumnHandle]);
                    
                var iRowAcc = (IDataRowAccessor)dataRow;
                Assert.AreEqual(iRowAcc[column], clone[column.ColumnName]);
                Assert.AreEqual(iRowAcc[column.ColumnHandle], clone[column.ColumnHandle]);
                    
                var iReadOnlyRowAcc = (IDataRowReadOnlyAccessor)dataRow;

                Assert.AreEqual(iReadOnlyRowAcc[column], clone[column.ColumnName]);
                Assert.AreEqual(iReadOnlyRowAcc[column.ColumnHandle], clone[column.ColumnHandle]);
            }
                
            Assert.AreEqual(dataRow.RowRecordState, clone.RowRecordState);
        }
    }
    protected static bool EqualsList<T>(Data<T> a, Data<T> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        
        for (int index = 0; index < a.Count; ++index)
        {
            if (a[index].Equals(b[index]) == false)
                return false;
        }
        return true;
    }

    [Test]
    public void TestTypedAutoincrement()
    {
        var table = GetTestTable();
            
        var objects = new Map<TableStorageType, object>();
            
        TestNextAutoIncrement<SByte>(table, objects);
        TestNextAutoIncrement<Byte>(table, objects);
        TestNextAutoIncrement<UInt16>(table, objects);
        TestNextAutoIncrement<UInt32>(table, objects);
        TestNextAutoIncrement<UInt64>(table, objects);
        TestNextAutoIncrement<Int16>(table, objects);
        TestNextAutoIncrement<Int32>(table, objects);
        TestNextAutoIncrement<Int64>(table, objects);
        TestNextAutoIncrement<Decimal>(table, objects);
        TestNextAutoIncrement<Single>(table, objects);
        TestNextAutoIncrement<Double>(table, objects);
    }
        
    private static void TestNextAutoIncrement<T>(DataTable table, Map<TableStorageType, object> objects) where T: struct, IComparable, IComparable<T>
    {
        var tableStorageType = DataTable.GetColumnType(typeof(T));

        var sb1 = table.NextAutoIncrementValue<T>(table.GetColumn(tableStorageType.type + "." + tableStorageType.typeModifier));

        if (objects.MissingKey(tableStorageType.type))
        {
            objects[tableStorageType.type] = sb1;
        }
        else
        {
            Assert.True(sb1.CompareTo((T)objects[tableStorageType.type]) > 0);
        }
    }
        
    [Test]
    public void TestAggregates()
    {
        var table = GetTestTable();

        TestAggregates<UInt32>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<UInt64>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<Int16>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<Int32>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<Int64>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<Decimal>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<Single>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
        TestAggregates<Double>(table, f => f.Select(fi => (int)fi).Average(), f => f.Select(fi => (int)fi).Sum(), f => f.Select(fi => (double)fi).StdDev());
    }
        
    private static void TestAggregates<T>(DataTable table, Func<IEnumerable<T>, double> avgFunc, Func<IEnumerable<T>, double> sumFunc, Func<IEnumerable<T>, double> stDevFunc) 
    {
        var tableStorageType = DataTable.GetColumnType(typeof(T));

        var columnName = tableStorageType.type.ToString();
        
        var cnt = (int)table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.Count);

        Assert.AreEqual(cnt, table.RowCount);
            
        Assert.Null(table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.None));
            
        Assert.AreEqual(table.Rows.First()[columnName], table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.First));

        var max = table.Rows.SelectFieldValue<T>(columnName).Max();

        var aggMax = table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.Max);
        
        Assert.AreEqual(max, aggMax);
            
        var min = table.Rows.SelectFieldValue<T>(columnName).Min();

        var aggMin = table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.Min);
        Assert.AreEqual(min, aggMin);
            
        var avg = avgFunc(table.Rows.SelectFieldValue<T>(columnName));

        var aggregatedValue = table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.Mean);
        
        var aggregatedAvg = (double)Convert.ChangeType(aggregatedValue, typeof(double));
            
        Assert.True(Math.Abs(avg - aggregatedAvg) < 0.0001);

        var sum = sumFunc(table.Rows.SelectFieldValue<T>(columnName));
            
        var aggregatedSum = (double)Convert.ChangeType(table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.Sum), typeof(double));
            
        Assert.True(Math.Abs(sum - aggregatedSum) < 0.0001);
            
        var stdev = stDevFunc(table.Rows.SelectFieldValue<T>(columnName));
            
        var aggregatedStdev = (double)Convert.ChangeType(table.GetAggregatedValue(table.GetColumn(columnName), AggregateType.StDev), typeof(double));
            
        Assert.True(Math.Abs(stdev - aggregatedStdev) < 0.9);
    }

    [Test]
    public void TestBytesDeserialization()
    {
        var table = GetTestTable();

        var dataRows = table.Rows.Where(TableStorageType.Byte + "." + TableStorageTypeModifier.Array).AsString().Not().Equals(null).ToData();

        foreach (var row in dataRows)
        {
            var bytes = row.Field<byte[]>(TableStorageType.Byte + "." + TableStorageTypeModifier.Array);

            var str = Encoding.UTF8.GetString(bytes);

            Assert.AreEqual("TableStorageType.ByteArray.ToString()", str);
        }
    }

    [Test]
    public void TestTableExtendedProperties()
    {
        var table = GetTestTable();

        Assert.IsEmpty(table.GetXProperty<string>("SomeProp"));

        table.SetXProperty("SomeProp", true);

        Assert.AreEqual(true, table.GetXProperty<bool>("SomeProp"));

        var clone = table.Clone();

        Assert.AreEqual(true, clone.GetXProperty<bool>("SomeProp"));
        
        clone.ClearTableXProperties();
        
        Assert.IsEmpty(clone.GetXProperty<string>("SomeProp"));
    }

   
    [Test]
    public void TestDetachedRowAdd()
    {
        var table = GetTestTable();

        var tableRowCount = table.RowCount;

        var detachedRow = table.NewRow();

        Assert.AreEqual(tableRowCount, table.RowCount);

        detachedRow[Fields.id] = ushort.MaxValue;
        detachedRow[Fields.Name] = ushort.MaxValue.ToString();
        detachedRow["UserClass"] =  new ComplexUserClass() { ID = detachedRow.Field<int>(Fields.id) };

        table.ImportRow(detachedRow);

        Assert.AreEqual(tableRowCount + 1, table.RowCount);

        var row = table.GetRowBy(detachedRow.Field<int>(Fields.id));

        Assert.NotNull(row);

        Assert.AreEqual(row.Field<string>(Fields.Name), detachedRow.Field<string>(Fields.Name));
    }


    public enum LockEventsMode
    {
        BeginInit, LockRow, LockTable
    }
       

    [Test]
    public void TestLockEvents([Values(LockEventsMode.BeginInit, LockEventsMode.LockRow, LockEventsMode.LockTable)] LockEventsMode mode)
    {
        var table = GetTestTable();

        var rowLocked = table.GetRowBy(3);
        var row = table.GetRowBy(5);

        table.AcceptChanges();

        var initialize = mode == LockEventsMode.BeginInit;
        var rowLock = mode == LockEventsMode.LockRow;
        var tableLock = mode == LockEventsMode.LockTable;

        IDataEditTransaction dataEditTransaction = null;
        IDataLoadState dataLoad = null;
        IDataLockEventState dataLockEventState = null;
            
        if (initialize)
        {
            dataLoad = table.BeginLoad();
        }
        else if (tableLock)
        {
            dataLockEventState = table.LockEvents();
            dataEditTransaction = table.StartTransaction();
        }
        else if(rowLock)
        {
            dataLockEventState = rowLocked.LockEvents();
            dataEditTransaction = rowLocked.StartTransaction();
        }

        var rowChangedColumnsByEvent = new HashSet<string>();
        var rowChangingColumnsByEvent = new HashSet<string>();

        var lockedRowChangedColumnsByEvent = new HashSet<string>();
        var lockedRowChangingColumnsByEvent = new HashSet<string>();

        table.ColumnChanged.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                    
            if (args.Row.Equals(rowLocked))
            {
                foreach (var columnName in args.ChangedColumnNames)
                {
                    lockedRowChangedColumnsByEvent.Add(columnName);
                            
                    Assert.AreNotEqual(args.GetOldValue(columnName), args.GetNewValue(columnName));
                    Assert.True(args.IsColumnChanged(columnName));
                }
            }
            else
            {
                foreach (var columnName in args.ChangedColumnNames)
                {
                    rowChangedColumnsByEvent.Add(columnName);

                    Assert.AreNotEqual(args.GetOldValue(columnName), args.GetNewValue(columnName));
                    Assert.True(args.IsColumnChanged(columnName));
                }
            }
        });

        table.ColumnChanging.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                        
            if (args.Row.Equals(rowLocked))
            {
                lockedRowChangingColumnsByEvent.Add(args.ColumnName);
            }
            else
            {
                rowChangingColumnsByEvent.Add(args.ColumnName);
            }
        });

        rowLocked[Fields.Name] = Guid.Empty.ToString();
        rowLocked[Fields.Sn] = rowLocked.Field<int>(Fields.Sn) + 1;

        row[Fields.Name] = Guid.Empty.ToString();
        row[Fields.Sn] = rowLocked.Field<int>(Fields.Sn) + 1;

        Assert.False(lockedRowChangedColumnsByEvent.Contains(Fields.Name));
        Assert.False(lockedRowChangedColumnsByEvent.Contains(Fields.Sn));

        if (initialize == false && tableLock == false)
        {
            Assert.True(rowChangedColumnsByEvent.Contains(Fields.Name));
            Assert.True(rowChangedColumnsByEvent.Contains(Fields.Sn));
        }

        if (initialize)
        {
            dataLoad.EndLoad();
        }
        else if (tableLock)
        {
            dataEditTransaction.Commit();
            dataLockEventState.UnlockEvents();
        }
        else if(rowLock)
        {
            dataEditTransaction?.Commit();
            dataLockEventState.UnlockEvents();
        }

        var lockedRowChangedFields = rowLocked.GetChangedFields().ToData();
        var rowChangedFields = row.GetChangedFields().ToData();

        if (initialize == false)
        {
            Assert.True(lockedRowChangedFields.Contains(Fields.Name));
            Assert.True(lockedRowChangedFields.Contains(Fields.Sn));
            Assert.False(lockedRowChangedFields.Contains(Fields.groupid));
            Assert.True(lockedRowChangedFields.SequenceEqual(rowChangedFields));
        }

        if (initialize)
        {
            Assert.False(lockedRowChangedColumnsByEvent.Contains(Fields.Name));
            Assert.False(lockedRowChangedColumnsByEvent.Contains(Fields.Sn));
        }
        else
        {
            Assert.True(lockedRowChangedColumnsByEvent.Contains(Fields.Name));
            Assert.True(lockedRowChangedColumnsByEvent.Contains(Fields.Sn));

            Assert.True(rowChangedColumnsByEvent.Contains(Fields.Name));
            Assert.True(rowChangedColumnsByEvent.Contains(Fields.Sn));

            if (tableLock == false)
            {
                Assert.True(rowChangingColumnsByEvent.Contains(Fields.Name));
                Assert.True(rowChangingColumnsByEvent.Contains(Fields.Sn));
            }
        }

        Assert.False(lockedRowChangedFields.Contains(Fields.groupid));

        Assert.False(lockedRowChangingColumnsByEvent.Contains(Fields.Name));
        Assert.False(lockedRowChangingColumnsByEvent.Contains(Fields.Sn));

        Assert.False(lockedRowChangingColumnsByEvent.Contains(Fields.groupid));
    }

    [Test]
    public void TestRemovingDependandColumn()
    {
        var table = GetTestTable();

        Assert.False(table.CanRemoveColumn(Fields.Name));
        Assert.Throws<DataException>(() => table.RemoveColumn(Fields.Name));

        table.RemoveColumn("Expr1");
        
        Assert.True(table.CanRemoveColumn(Fields.Name));
        table.RemoveColumn(Fields.Name);
        
        table.AddColumn(Fields.Name, TableStorageType.Single);

        var newNameColumn = table.GetColumn(Fields.Name);

        var value1 = newNameColumn.GetXProperty<bool?>("IsCustom");

        Assert.Null(value1);
        Assert.IsEmpty(newNameColumn.XProperties);
    }
    
   

    public DataTable GetTestTable()
    {
        var testTable = m_sourceDataTable.Copy();
            
        testTable.AcceptChanges();
            
        return testTable;
    }

    public static DataTable GetTestTable(TableTestMode tableTestMode, DataTable source, Func<string, DataTable> createTable)
    {
        switch (tableTestMode)
        {
            case TableTestMode.Copy:
            {
                return source.Copy();
            }
            case TableTestMode.Direct:
            {
                return createTable(source.Name);
            }
            case TableTestMode.CloneMerge:
            {
                var tbl = source.Clone();

                tbl.FullMerge(source);

                return tbl;
            }
            case TableTestMode.CopyMerge:
            {
                var tbl = source.Copy();

                tbl.MergeDataOnly(source);

                return tbl;
            }
            case TableTestMode.ReverseFullMerge:
            {
                var table = new DataTable();

                table.Name = "t_nmt";

                FillTable(table, reverseColumns: true);

                table.FullMerge(source);

                return table;
            }
            case TableTestMode.Xml:
            {
                var xElement = source.ToXml(SerializationMode.Full);

                var table = new DataTable();

                table.LoadFromXml(xElement);

                return table;
            }
            case TableTestMode.XmlSchemaMerge:
            {
                var xElement = source.ToXml(SerializationMode.SchemaOnly);

                var table = new DataTable();

                table.LoadMetadataFromXml(xElement);
                    
                table.MergeDataOnly(source);

                return table;
            }
            case TableTestMode.Json:
            {
                var json = source.ToJson(SerializationMode.Full);

                var table = new DataTable();

                table.LoadFromJson(json);

                return table;
            }
            case TableTestMode.JsonSchemaMerge:
            {
                var json = source.ToJson(SerializationMode.SchemaOnly);

                var table = new DataTable();

                table.LoadMetadataFromJson(json);
                    
                table.MergeDataOnly(source);

                return table;
            }
            case TableTestMode.XmlDataset:
            {
                var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                dataSet.AddTable(source.Copy());
                    
                var dsXElement = dataSet.ToXml(SerializationMode.Full);

                var testDs = new DataTable();

                testDs.LoadFromXml(dsXElement);
                    
                return (DataTable)testDs.GetTable(source.Name);
            }
            case TableTestMode.XmlDatasetMerge:
            {
                var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                dataSet.AddTable(source.Copy());
                    
                var dsXElement = dataSet.ToXml(SerializationMode.SchemaOnly);

                var testDs = new DataTable();

                testDs.DatasetSchemaFromXElement(dsXElement);

                var testTable = testDs.GetTable(source.Name);
                    
                testTable.MergeDataOnly(source);
                    
                return (DataTable)testTable;
            }
            case TableTestMode.XmlDatasetSerializeDataOnly:
            {
                var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                dataSet.AddTable(source.Copy());
                    
                var dsXElement = dataSet.ToXml(SerializationMode.DataOnly);

                var testDs = new DataTable();

                testDs.MergeMeta(dataSet);
                    
                testDs.LoadDataFromXml(dsXElement);

                var testTable = testDs.GetTable(source.Name);
                    
                return (DataTable)testTable;
            }
            case TableTestMode.JsonDataset:
            {
                var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                dataSet.AddTable(source.Copy());
                    
                var json = dataSet.ToJson(SerializationMode.Full);

                var testDs = new DataTable();

                testDs.LoadFromJson(json);
                    
                return (DataTable)testDs.GetTable(source.Name);
            }
            case TableTestMode.JsonDatasetMerge:
            {
                var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                dataSet.AddTable(source.Copy());
                    
                var dsXElement = dataSet.ToJson(SerializationMode.SchemaOnly);

                var testDs = new DataTable();

                testDs.LoadMetadataFromJson(dsXElement);

                var testTable = testDs.GetTable(source.Name);
                    
                testTable.MergeDataOnly(source);
                    
                return (DataTable)testTable;
            }
            case TableTestMode.JsonDatasetSerializeDataOnly:
            {
                var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                dataSet.AddTable(source.Copy());
                    
                var json = dataSet.ToJson(SerializationMode.DataOnly);

                var testDs = new DataTable();

                testDs.MergeMeta(dataSet);
                    
                testDs.LoadDataFromJson(json);

                var testTable = testDs.GetTable(source.Name);
                    
                return (DataTable)testTable;
            }
        }

        throw new NotSupportedException();
    }

    [Test]
    public void TestDeepEquals1()
    {
        var goldenSource = m_sourceDataTable.Copy();

        goldenSource.AcceptChanges();

        var test = GetTestTable();

        test.AcceptChanges();

        Assert.True(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) == 0);
    }

    [Test]
    public void TestDeepEquals2()
    {
        var goldenSource = m_sourceDataTable.Copy();

        var test = GetTestTable();

        test.AddRow(test.NewRow(_.MapObj((Fields.id, (int)ushort.MaxValue),
            ("UserClass", new ComplexUserClass() { ID = (int)ushort.MaxValue }))));

        Assert.False(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) != 0);

        test.RejectChanges();

        Assert.True(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) == 0);
    }

    [Test]
    public void TestDeepEquals3()
    {
        var goldenSource = m_sourceDataTable.Copy();

        var test = GetTestTable();

        var columnName = Guid.NewGuid().ToString();

        test.AddColumn(columnName);

        Assert.False(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) != 0);

        test.RemoveColumn(columnName);

        Assert.True(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) == 0);
    }

    [Test]
    public void TestDeepEquals4()
    {
        var goldenSource = m_sourceDataTable.Copy();

        var test = GetTestTable();

        var dataRow = test.Rows.Last();

        var transaction = dataRow.StartTransaction();

        dataRow[Fields.id] = ushort.MaxValue;

        Assert.False(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) != 0);

        transaction.Rollback();

        Assert.True(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) == 0);
    }

    [Test]
    public void TestDeepEquals5()
    {
        var goldenSource = m_sourceDataTable.Copy();

        var test = GetTestTable();

        var dataRow = test.Rows.Last();

        var transaction = dataRow.StartTransaction();

        dataRow[Fields.Code] = Guid.NewGuid().ToString();

        Assert.False(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) != 0);

        transaction.Rollback();

        Assert.True(goldenSource.Equals(test));

        Assert.True(goldenSource.CompareTo(test) == 0);
    }

    [Test]
    public void TestDeepEquals6()
    {
        var goldenSource = m_sourceDataTable.Copy();

        var test = GetTestTable();

        var dataRow = test.Rows.Last();
        
        var transaction = dataRow.StartTransaction();

        dataRow.SetXProperty("Test", Guid.NewGuid());
            
        Assert.False(goldenSource.Equals(test));
            
        Assert.True(goldenSource.CompareTo(test) != 0);

        transaction.Rollback();

        var valueTuple = goldenSource.EqualsExt(test);
        
        Assert.True(valueTuple.value, valueTuple.ToString());
            
        Assert.True(goldenSource.CompareTo(test) == 0);
    }

    [Test]
    public void TestInitEvents()
    {
        var table = GetTestTable();
            
        var loadState = table.BeginLoad();

        var addingRaised = false;
        var addedRaised = false;
        var nameColumnChangingRaised = false;
        var nameColumnChangedRaised = false;
        var rowDeletingRaised = false;
        var rowDeletedRaised = false;
        var dataRowChangedRaised = false;

        var dataChangingCanceled = false;
        var dataRowDeleteingCanceled = false;

        table.RowAdding.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.False(args.IsCancel);
                    
            addingRaised = true;
        });

        table.RowAdded.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.False(args.IsMultipleRow);
                    
            addedRaised = true;
                    
            Assert.IsNotEmpty(args.Rows);
        });

        table.ColumnChanging.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.False(args.IsCancel);
                    
            nameColumnChangingRaised = true;

            if (args.ColumnName == Fields.Name)
            {
                Assert.NotNull(args.OldValue);
                        
                if ((string)args.NewValue == "3333")
                {
                    dataChangingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.ColumnChanged.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                    
            nameColumnChangedRaised = true;
        });

        table.RowDeleted.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.IsNotEmpty(args.DeletedRows);
                    
            rowDeletedRaised = true;
        });

        table.RowDeleting.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.False(args.IsCancel);
                    
            rowDeletingRaised = true;

            if (args.Rows.Any())
            {
                if (args.Rows.First().Field<int?>(Fields.id) == 7777)
                {
                    dataRowDeleteingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.DataRowChanged.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                    
            dataRowChangedRaised = true;
        });

        var rowAccessor = table.NewRow().Set("id", 7777).Set("UserClass", new ComplexUserClass() { ID = 7777 }).Set("sn", 1).Set("name", "7777");

        var dataRow = table.AddRow(rowAccessor);

        var dataRow1 = table.AddRow(table.NewRow().Set("id", 77771).Set("UserClass", new ComplexUserClass() { ID = 77771 }).Set("sn", 2).Set("name", "77771"));

        var name = dataRow[Fields.Name];

        var changedName = name + "_";

        dataRow[Fields.Name] = changedName;

        Assert.AreEqual(changedName, dataRow.Field<string>(Fields.Name));

        dataRow.SetXProperty(Fields.groupid + "1", 77);

        Assert.AreEqual(77, dataRow.Field<int>(Fields.groupid + "1"));
        Assert.AreEqual(77, dataRow[Fields.groupid + "1"]);
        Assert.AreEqual("77", dataRow.Field<string>(Fields.groupid + "1"));
        Assert.AreEqual(77, dataRow.Field<int?>(Fields.groupid + "1"));

        dataRow[Fields.Name] = "3333";

        Assert.AreEqual("3333", dataRow.Field<string>(Fields.Name));

        table.AcceptChanges();
            
        dataRow.Delete();

        Assert.True(dataRow.IsDeletedRow);

        table.AcceptChanges();
            
        dataRow1.Delete();

        loadState.EndLoad();

        Assert.True(dataRow1.IsDeletedRow);
        Assert.True(addedRaised);
        Assert.True(addingRaised);

        Assert.False(nameColumnChangingRaised);
        Assert.False(rowDeletingRaised);
        Assert.False(dataChangingCanceled);
        Assert.False(dataRowDeleteingCanceled);

        Assert.False(nameColumnChangedRaised);
        Assert.False(rowDeletedRaised);
        Assert.False(dataRowChangedRaised);

        table.Dispose();
    }

    [Test]
    public void TestDetachClear()
    {
        var table = GetTestTable();
            
        var testRow = table.Rows.First();

        testRow.SetRowError("Error");
        testRow.SetRowWarning("Warning");
        testRow.SetRowInfo("Info");
            
        testRow.SetXProperty("Test", "Value");

        var copy = table.Copy();
            
        var expected = copy.Rows.First();
            
        Assert.AreNotEqual(RowState.Detached, testRow.RowRecordState);
            
        table.ClearRows();

        table.LoadRows(copy.Rows);
            
        Assert.AreEqual(RowState.Modified, testRow.RowRecordState);
            
        Assert.AreEqual(expected.Field<int>(Fields.id), testRow.Field<int>(Fields.id));
        
        Assert.True(testRow.Equals(expected));
    }

    [Test]
    public void TestDetachDispose()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.First();
        var lastRow = table.Rows.Last();

        lastRow.SetRowError("Error");
        lastRow.SetRowWarning("Warning");
        lastRow.SetRowInfo("Info");

        var nameCol = table.GetColumn(Fields.Name);
            
        nameCol.SetXProperty("H", 100);
        nameCol.SetXProperty("W", 200);
            
        lastRow.SetXProperty("Test", "Value");
            
        Assert.AreEqual(lastRow[Fields.id], lastRow.SilentlyGetValue(Fields.id));
            
        var lastRowDetached = table.Copy().Rows.Last();
        
        Assert.IsNotEmpty(new CoreDataRowDebugView(lastRowDetached).Items);
        
        Assert.AreEqual(lastRow.GetRowFault(), lastRowDetached.GetRowFault());
        Assert.AreEqual(lastRow.GetRowWarning(), lastRowDetached.GetRowWarning());
        Assert.AreEqual(lastRow.GetRowError(), lastRowDetached.GetRowError());
        Assert.AreEqual(lastRow.GetRowInfo(), lastRowDetached.GetRowInfo());

        lastRowDetached.SetColumnError(Fields.id, "Error");
        lastRowDetached.SetColumnWarning(Fields.id, "Warning");
        lastRowDetached.SetColumnInfo(Fields.id, "Info");
            
        var lastRowChangedXProps = lastRow.GetChangedXProperties().ToData();
        
        lastRowDetached.DetachRow();
        
        var detachedRowChangedXProps = lastRowDetached.GetChangedXProperties().ToData();
            
        Assert.AreEqual(lastRowChangedXProps.ToArray(), detachedRowChangedXProps.ToArray(),"GetChangedXProperties");
        
        Assert.IsNotEmpty(new CoreDataRowDebugView(lastRowDetached).Items);
        Assert.IsNotEmpty(new DataRowDebugView(lastRowDetached).Items);

        var lrEq = lastRow.EqualsExt(lastRow);
        Assert.True(lrEq.value, lrEq.name + "." + lrEq.type);

        var lrDetEq = lastRow.EqualsExt(lastRowDetached);
        
        Assert.True(lrDetEq.value, lrDetEq.name + "." + lrDetEq.type);
        
        var lrDetLrEq = lastRowDetached.EqualsExt(lastRow);
        Assert.True(lrDetLrEq.value, lrDetLrEq.name + "." + lrDetLrEq.type);
        
        Assert.True(lastRowDetached.Equals(lastRowDetached));
            
        Assert.AreEqual(lastRow.Field<int>(Fields.id), lastRowDetached.FieldNotNull<int>(Fields.id));
            
        Assert.AreEqual("Error", lastRowDetached.GetCellError(Fields.id));
        Assert.AreEqual("Info", lastRowDetached.GetCellInfo(Fields.id));
        Assert.AreEqual("Warning", lastRowDetached.GetCellWarning(Fields.id));
            
        Assert.AreEqual("Error", lastRowDetached.GetColumnError(new ColumnHandle(0)));
        Assert.AreEqual("Info", lastRowDetached.GetColumnInfo(new ColumnHandle(0)));
        Assert.AreEqual("Warning", lastRowDetached.GetColumnWarning(new ColumnHandle(0)));
            
        Assert.True(lastRow.GetTableColumnNames().SequenceEqual(lastRowDetached.GetTableColumnNames()));

        Assert.AreEqual(Fields.id, lastRowDetached.GetErrorColumns().SingleOrDefault());
        Assert.AreEqual(Fields.id, lastRowDetached.GetWarningColumns().SingleOrDefault());
        Assert.AreEqual(Fields.id, lastRowDetached.GetInfoColumns().SingleOrDefault());
            
        Assert.AreEqual(RowState.Detached, lastRowDetached.RowRecordState);
            
        Assert.AreEqual(lastRow.GetHashCode(), lastRowDetached.GetHashCode(), $"Hashcode");
        Assert.AreEqual(lastRow.GetColumnCount(), lastRowDetached.GetColumnCount(), $"ColumnCount");
        Assert.True(lastRow.GetChangedFields().SequenceEqual(lastRowDetached.GetChangedFields()),"GetChangedFields");
            
        var nameColDetached = lastRowDetached.GetColumn(Fields.Name);

        Assert.AreEqual(100, nameColDetached.GetXProperty<int>("H"));
        Assert.AreEqual(200, nameColDetached.GetXProperty<int>("W"));
            
        lastRowChangedXProps = lastRow.GetChangedXProperties().ToData();
        detachedRowChangedXProps = lastRowDetached.GetChangedXProperties().ToData();
            
        Assert.AreEqual(lastRowChangedXProps.ToArray(), detachedRowChangedXProps.ToArray(),"GetChangedXProperties");

        var clone = table.Clone();
        
        foreach (var dataColumn in table.GetColumns())
        {
            var expected = lastRow[dataColumn];
            var actual = lastRowDetached[dataColumn];
                
            Assert.True(lastRowDetached.IsExistsField(dataColumn.ColumnName));

            Assert.True(CoreDataRowContainer.DeepEquals(expected, actual), $"Col: {dataColumn.ColumnName}");
            
            actual = lastRowDetached.SilentlyGetValue(dataColumn.ColumnName);
            
            Assert.True(CoreDataRowContainer.DeepEquals( expected, actual), $"SilentlyGetValue Col: {dataColumn.ColumnName}");

            expected = lastRow[new ColumnHandle(dataColumn.ColumnHandle)];
            actual = lastRowDetached[new ColumnHandle(dataColumn.ColumnHandle)];
            
            Assert.True(CoreDataRowContainer.DeepEquals(expected, actual), $"SilentlyGetValue Col: {dataColumn.ColumnName}");
            
            Assert.AreEqual(lastRow.GetColumn(dataColumn.ColumnName).ColumnName, lastRowDetached.GetColumn(dataColumn.ColumnName).ColumnName, $"Col equals: {dataColumn.ColumnName}");

            var isNull = lastRow.IsNull(dataColumn.ColumnName);
            
            Assert.True(lastRowDetached.IsNull(dataColumn.ColumnName) == isNull, dataColumn.ColumnName);
            Assert.True(lastRowDetached.IsNull(new ColumnHandle(dataColumn.ColumnHandle))== isNull, dataColumn.ColumnName);
            Assert.True(lastRowDetached.IsNull(dataColumn) == isNull, dataColumn.ColumnName);
            Assert.True(lastRowDetached.IsNull((IDataTableReadOnlyColumn)dataColumn) == isNull, dataColumn.ColumnName);
            Assert.True(lastRowDetached.IsNull(clone.GetColumn(dataColumn.ColumnName)) == isNull, dataColumn.ColumnName);
            Assert.True(lastRowDetached.IsNull((IDataTableReadOnlyColumn)clone.GetColumn(dataColumn.ColumnName)) == isNull, dataColumn.ColumnName);
                
            Assert.AreEqual( lastRow.GetOriginalValue(table.GetColumn(Fields.Code)),  lastRowDetached.GetOriginalValue(table.GetColumn(Fields.Code)));
            Assert.AreEqual( lastRow.GetOriginalValue(clone.GetColumn(Fields.Code)), lastRowDetached.GetOriginalValue(clone.GetColumn(Fields.Code)));
            Assert.AreEqual( lastRow.GetOriginalValue(clone.GetColumn(Fields.Code)), lastRowDetached.GetOriginalValue(new ColumnHandle((table.GetColumn(Fields.Code)).ColumnHandle)));
            Assert.AreEqual( lastRow.GetOriginalValue<string>(clone.GetColumn(Fields.Code)), lastRowDetached.GetOriginalValue<string>(clone.GetColumn(Fields.Code)));
            Assert.AreEqual( lastRow.GetOriginalValue<string>(table.GetColumn(Fields.Code)), lastRowDetached.GetOriginalValue<string>(table.GetColumn(Fields.Code)));
            Assert.AreEqual( lastRow.GetOriginalValue<string>(table.GetColumn(Fields.Code)), lastRowDetached.GetOriginalValue<string>(new ColumnHandle((table.GetColumn(Fields.Code)).ColumnHandle)));
                
            Assert.Throws<DataDetachedException>(() =>  lastRowDetached[dataColumn] = null , $"Col: {dataColumn.ColumnName}");
            Assert.Throws<DataDetachedException>(() =>  lastRowDetached[dataColumn.ColumnName] = null, $"Col: {dataColumn.ColumnName}");
            Assert.Throws<DataDetachedException>(() =>  lastRowDetached[new ColumnHandle(dataColumn.ColumnHandle)] = null, $"Col: {dataColumn.ColumnName}");
        }
            
        Assert.DoesNotThrow(() => lastRowDetached.SilentlySetValue(Fields.id, -1));
        Assert.DoesNotThrow(() => lastRowDetached.SilentlySetValue(Guid.NewGuid().ToString(), -1));
            
        Assert.AreEqual(lastRow.Field<int>(Fields.id), lastRowDetached.Field<int>(Fields.id));
        Assert.AreEqual(lastRow.Field<int?>(Fields.id), lastRowDetached.Field<int?>(Fields.id));
        Assert.AreEqual(lastRow.Field<string>(Fields.id), lastRowDetached.Field<string>(Fields.id));
            
        Assert.Throws<DataDetachedException>(() => lastRowDetached.Set(table.GetColumn(Fields.id), 1));
        Assert.Throws<DataDetachedException>(() =>  lastRowDetached.Set(table.GetColumn(Fields.id), new int?(1)));
        Assert.Throws<DataDetachedException>(() =>  lastRowDetached.Set(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle), 1));
            
        Assert.Throws<DataDetachedException>(() => lastRowDetached.SetColumnError(table.GetColumn(Fields.id), "Error"));
        Assert.Throws<DataDetachedException>(() => lastRowDetached.SetColumnWarning(table.GetColumn(Fields.id), "Warn"));
        Assert.Throws<DataDetachedException>(() => lastRowDetached.SetColumnInfo(table.GetColumn(Fields.id), "Info"));
        Assert.Throws<DataDetachedException>(() =>  lastRowDetached.SetColumnError(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle), "Error"));
        Assert.Throws<DataDetachedException>(() =>  lastRowDetached.SetColumnWarning(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle), "Warn"));
        Assert.Throws<DataDetachedException>(() =>  lastRowDetached.SetColumnInfo(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle), "Info"));
            
        Assert.AreEqual(lastRow.Field<int>(table.GetColumn(Fields.id)), lastRowDetached.Field<int>(table.GetColumn(Fields.id)));
        Assert.AreEqual(lastRow.Field<int?>(table.GetColumn(Fields.id)), lastRowDetached.Field<int?>(table.GetColumn(Fields.id)));
        Assert.AreEqual(lastRow.Field<string>(table.GetColumn(Fields.id)), lastRowDetached.Field<string>(table.GetColumn(Fields.id)));
            
        Assert.AreEqual(lastRow.Field<int>(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)), lastRowDetached.Field<int>(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)));
        Assert.AreEqual(lastRow.Field<int?>(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)), lastRowDetached.Field<int?>(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)));
        Assert.AreEqual(lastRow.Field<string>(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)), lastRowDetached.Field<string>(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)));

        Assert.AreEqual(lastRow.GetRowAge(), lastRowDetached.GetRowAge());
            
        Assert.AreEqual(lastRow.GetRowAge(), lastRowDetached.GetColumnAge(Fields.id));
        Assert.AreEqual(lastRow.GetRowAge(), lastRowDetached.GetColumnAge(table.GetColumn(Fields.id)));
        Assert.AreEqual(lastRow.GetRowAge(), lastRowDetached.GetColumnAge(new ColumnHandle(table.GetColumn(Fields.id).ColumnHandle)));
           
     
            
        Assert.AreEqual(0, lastRowDetached.GetXProperty<int>(Guid.NewGuid().ToString()));
        Assert.AreEqual(string.Empty, lastRowDetached.GetXProperty<string>(Guid.NewGuid().ToString()));
        Assert.AreEqual("Value", lastRowDetached.GetXProperty<string>("Test"));

        Assert.True(lastRow.GetXProperties().SequenceEqual(lastRowDetached.GetXProperties()));

        table.Dispose();
            
        Assert.AreEqual(RowState.Disposed, dataRow.RowRecordState);
    }

    [Test]
    public void TestDetachUpsert()
    {
        var table = GetTestTable();

        var list = table.Rows.ToData();

        var rows = list.Skip(5).Take(10).ToData();

        var rowContainers = rows.Select(r => r.ToContainer()).ToData();

        rowContainers.Reverse();

        table.LoadRows(rowContainers, true);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];

            Assert.AreNotEqual(RowState.Detached, row.RowRecordState);
        }
    }

    [Test]
    public void TestDefault()
    {
        var table = GetTestTable();

        var row = table.Rows.First();

        Assert.AreEqual("test4312", row.Field<string>("Default"));

        row.Set("Default", "123");

        Assert.AreEqual("123", row.Field<string>("Default"));

        row.SetNull("Default");

        Assert.True(row.IsNull("Default"));

        Assert.AreEqual("test4312", row.Field<string>("Default"));

        Assert.AreEqual("321", row.Field<string>("Default", "321"));
    }

    [Test]
    public void TestSelectExpression1()
    {
        var table = GetTestTable();

        var rows = table.Select<DataRow>("id = 5");

        Assert.IsNotEmpty(rows);

        Assert.True(rows.All(r => r.Field<int>("id") == 5));
    }

    [Test]
    public void TestSelectExpression2()
    {
        var table = GetTestTable();

        var rows = table.Select<DataRow>("name = 'Name 5'");

        Assert.IsNotEmpty(rows);

        Assert.True(rows.All(r => r.Field<string>("name") == "Name 5"));
    }

    [Test]
    public void TestSelectExpression3()
    {
        var table = GetTestTable();

        var expected = GetRowHandles(table.Rows
            .WhereFieldValueIn(Fields.id, 5, 6, 7));

        var rows = GetRowHandles(table.Select<DataRow>("id IN (5, 6, 7)"));

        Assert.AreEqual(expected, rows);
    }

    [Test]
    public void TestSelectExpression4()
    {
        var table = GetTestTable();

        var expected = GetRowHandles(table.Rows.WhereFieldValueStartsWith(Fields.Code, "Code", false));
        
        Assert.AreEqual(expected, GetRowHandles(table.Select<DataRow>("code LIKE 'Code %'")));
        Assert.IsEmpty(table.Select<DataRow>("code LIKE '123123 %'"));
        
        expected = GetRowHandles(table.Rows.WhereFieldValue(Fields.Code, "Code 0"));
        Assert.AreEqual(expected, GetRowHandles(table.Select<DataRow>("code LIKE 'Code 0%'")));
    }

    private int[] GetRowHandles(IEnumerable<DataRow> rows) => rows.Select(r => r.RowHandle).OrderBy(r => r).ToArray();

    [Test]
    public void TestSelectExpression5()
    {
        var table = GetTestTable();

        var exRows = table.Rows
            .WhereFieldValueIn(Fields.id, 5, 6, 7)
            .Union(table.Rows.WhereFieldValue(Fields.Code, "Code 1"));
        
        var expected = GetRowHandles(exRows);

        var rows = GetRowHandles(table.Select<DataRow>("id IN (5, 6, 7) OR code LIKE 'code 1%'"));

        Assert.AreEqual(expected, rows);
    }

    [Test]
    public void TestGetChangedFields()
    {
        var table = GetTestTable();

        var dataEdit = table.StartTransaction();

        var dataRow = table.ImportRow(table.NewRow()
            .Set(Fields.id, 7777)
            .Set(Fields.Sn, 888)
            .Set(Fields.Name, "Name " + 77777)
            .Set(Fields.Directories.CreateDt, DateTime.Now)
            .Set(Fields.Directories.LmDt, DateTime.Now)
            .Set(Fields.groupid, -1)
            .Set(Fields.Directories.LmId, -1)
            .Set(Fields.Directories.Guid, Guid.NewGuid())
            .Set(Fields.Directories.Code, "Code " + 77777 % 50)
            .Set("UserClass", new ComplexUserClass() { ID = 7777 })
            .Set("Point2D", new ComplexPoint2D(-1, -1))
            .Set("DataRowContainer", CreateTestRowContainer(-1))
            .Set(Fields.Directories.Data, new double[1])
            .Set(Fields.Directories.DataTypes, new Type[] {null})
            .Set(Fields.Directories.TimeRange, new Range<TimeSpan>(TimeSpan.Zero, TimeSpan.MaxValue))
        );

        dataEdit.Commit();

        var columns = table.GetColumns().ToData();

        var changedFields = dataRow.GetChangedFields().ToData();

        foreach (var column in columns)
        {
            if (column.FixType == DataColumnType.Common)
            {
                var firstOrDefault = changedFields.FirstOrDefault(c => c == column.ColumnName);

                if (firstOrDefault == null)
                {
                    //skip storage columns
                    var columnColumnName = column.ColumnName.Split(".").FirstOrDefault();
                    
                    if (columnColumnName != null && Enum.TryParse(typeof(TableStorageType), columnColumnName, out var _))
                    {
                        continue;
                    }
                }
                    
                Assert.NotNull(firstOrDefault, column.ColumnName);
            }
        }

        table.AcceptChanges();

        Assert.IsEmpty(dataRow.GetChangedFields());

        dataRow[Fields.Name] = "123123";

        var changes = dataRow.GetChangedFields().ToSet();
            
        Assert.True(changes.Contains(Fields.Name));
        Assert.False(changes.Contains(Fields.Code));

        Assert.IsNotEmpty(changes.Where(c => c == Fields.Name));
    }

    [Test]
    public void TestMaxColumnLenConstraint()
    {
        var table = GetTestTable();

        table.AddColumn("qwerty", TableStorageType.String, columnMaxLength: 10);
        table.AddColumn("ArrayTest", TableStorageType.Int32, TableStorageTypeModifier.Array, columnMaxLength: 10);
        
        var blablablablabla = "blablablablabla";
        var badArray = Enumerable.Range(0, 100).ToArray();
        
        var maxValueCapturedStr = new Data<string>();
        var maxValueCapturedArr = new Data<string>();
        
        table.MaxColumnLenConstraint.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                            
            if (args.ColumnName == "qwerty")
            {
                var value = args.GetValue<string>();

                if (value == blablablablabla)
                {
                    maxValueCapturedStr.Add(args.ColumnName);
                }
            }
            
            if (args.ColumnName == "ArrayTest")
            {
                var value = args.GetValue<int[]>();

                if (Tool.ArraysDeepEqual(value, badArray, (x, y) => x == y))
                {
                    maxValueCapturedArr.Add(args.ColumnName);
                }

            }
            
            args.RaiseError = false;
        });

        var row = table.GetRowBy(5);

        row.Set("qwerty", blablablablabla);
        Assert.AreEqual(10, row.Field<string>("qwerty").Length);
        row["qwerty"] = blablablablabla;
        Assert.AreEqual(10, row.Field<string>("qwerty").Length);
        row[table.GetColumn("qwerty")] = blablablablabla;
        Assert.AreEqual(10, row.Field<string>("qwerty").Length);
        row[(IDataTableColumn)table.GetColumn("qwerty")] = blablablablabla;
        Assert.AreEqual(10, row.Field<string>("qwerty").Length);
        row[(ICoreDataTableColumn)table.GetColumn("qwerty")] = blablablablabla;
        Assert.AreEqual(10, row.Field<string>("qwerty").Length);
        row[new ColumnHandle(table.GetColumn("qwerty").ColumnHandle)] = blablablablabla;
        Assert.AreEqual(10, row.Field<string>("qwerty").Length);
        
        row.Set("qwerty", "123");
        Assert.AreEqual(3, row.Field<string>("qwerty").Length);
        row.Set("qwerty", "4215");
        Assert.AreEqual(4, row.Field<string>("qwerty").Length);
        
        Assert.AreEqual(6, maxValueCapturedStr.Count);
        
        row.Set("ArrayTest", badArray);
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        row["ArrayTest"] = badArray;
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        row[table.GetColumn("ArrayTest")] = badArray;
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        row[(IDataTableColumn)table.GetColumn("ArrayTest")] = badArray;
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        row[(ICoreDataTableColumn)table.GetColumn("ArrayTest")] = badArray;
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        row[new ColumnHandle(table.GetColumn("ArrayTest").ColumnHandle)] = badArray;
        Assert.AreEqual(10, row.Field<int[]>("ArrayTest").Length);
        
        Assert.AreEqual(6, maxValueCapturedArr.Count);
        
        row.Set("ArrayTest", new int[3]{1,2,3});
        Assert.AreEqual(3, row.Field<int[]>("ArrayTest").Length);
        row.Set("ArrayTest", new int[4]{1,2,3,4});
        Assert.AreEqual(4, row.Field<int[]>("ArrayTest").Length);
    }

    [Test]
    public void TestMaxColumnLenConstraintDataCancel()
    {
        var table = new DataTable();

        table.AddColumn("qwerty", TableStorageType.String, columnMaxLength: 10);
        table.AddColumn("ArrayTest", TableStorageType.Int32, TableStorageTypeModifier.Array, columnMaxLength: 10);
        
        var badString = "blablablablabla";
        var badArray = Enumerable.Range(0, 100).ToArray();
        
        var newRow = table.NewRow();

        Assert.Throws<DataException>(() => newRow["qwerty"] = badString);

        Assert.Throws<DataChangeCancelException>(() => table.AddRow(_.MapObj(("qwerty", badString))));
        
        newRow = table.NewRow();

        Assert.Throws<DataException>(() => newRow["ArrayTest"] = badArray);

        Assert.Throws<DataChangeCancelException>(() => table.AddRow(_.MapObj(("ArrayTest", badArray))));
    }

    [Test]
    public void TestAddColumnsAfter()
    {
        var table = GetTestTable();
            
        table.AddColumn("test", TableStorageType.String);
        table.AddColumn("qwerty", TableStorageType.String, columnMaxLength: 10);

        for (int i1 = 0; i1 < 20; i1++)
        {
            var blablablablabla = "blablablablabla";

            var maxValueCaptured = new HashSet<string>();

            table.MaxColumnLenConstraint.Subscribe((args, c) =>
            {
                Assert.AreEqual(table, args.Table);
                            
                if (args.GetValue<string>() == blablablablabla && args.ColumnName == "qwerty" && args.Row.RowRecordState != RowState.Detached)
                {
                    maxValueCaptured.Add(args.ColumnName);
                }

                args.RaiseError = false;
            });

            var row = table.GetRowBy(5);

            Assert.AreEqual(row.Field<string>("TEST"), string.Empty);
            Assert.True(row.IsNull("TesT"));
            Assert.False(row.IsNotNull("Test"));

            var dataEdit = table.StartTransaction();

            for (int i = 10000; i > 9998; i--)
            {
                var id = i;

                table.ImportRow(table.NewRow()
                    .Set(Fields.id, i)
                    .Set(Fields.Name, "Name " + id)
                    .Set(Fields.Directories.CreateDt, DateTime.Now)
                    .Set(Fields.Directories.LmDt, DateTime.Now)
                    .Set(Fields.groupid, -i)
                    .Set(Fields.Directories.LmId, -i)
                    .Set(Fields.Directories.Guid, Guid.NewGuid())
                    .Set(Fields.Directories.Code, "Code " + id % 50)
                    .Set("TEST", "TEST")
                    .Set("qwerty", "qwerty111")
                    .Set("UserClass", new ComplexUserClass() { ID = id })
                );
            }

            dataEdit.Commit();

            var last = table.Rows.Last();

            Assert.NotNull(last.Field<string>("TEST"));
            Assert.False(last.IsNull("TesT"));
            Assert.True(last.IsNotNull("Test"));

            last["qwerty"] = blablablablabla;

            Assert.True(maxValueCaptured.Count > 0);
        }
    }

    [Test]
    public void TestAddColumnsAfter2()
    {
        var table = GetTestTable();

        table.AddColumn("name1");
        table.AddIndex("name1");

        var maxId = (int)table.Max(Fields.id);

        var dataEdit = table.StartTransaction();
        var row = table.NewRow();
        row[Fields.id] = ++maxId;
        row["UserClass"] =  new ComplexUserClass() { ID = row.Field<int>(Fields.id) };

        row["name1"] = "test1";
        table.AddRow(row);
        dataEdit.Commit();

        var firstRow1 = table.GetRow("name1", "test1");

        table.AddColumn("column1");
        table.AddIndex("column1");

        Assert.Null(firstRow1["column1"]);

        var beginEdit = table.StartTransaction();
        var anotherRow = table.NewRow();
        anotherRow[Fields.id] = ++maxId;
        anotherRow["column1"] = "test2";
        anotherRow["UserClass"] =  new ComplexUserClass() { ID = anotherRow.Field<int>(Fields.id) };
        table.AddRow(anotherRow);
        beginEdit.Commit();

        var findRow = table.GetRow("column1", "test2");
        Assert.NotNull(findRow);
        Assert.Null(findRow["name1"]);
    }

    [Test]
    public void TestAutoNotIndexIncrementAndDeleteRowOperation()
    {
        var table = GetTestTable();

        var max1 = (int)table.Max(Fields.Sn);

        Assert.AreNotEqual(0, max1);

        var row = table.GetRow(Fields.Sn, max1);

        Assert.AreEqual(max1, row.Field<int>(Fields.Sn));

        var dataEdit = table.StartTransaction();

        row.Delete();

        var max2 = (int)table.Max(Fields.Sn);

        Assert.AreNotEqual(max1, max2);

        dataEdit.Commit();

        var max3 = (int)table.Max(Fields.Sn);

        Assert.AreNotEqual(max1, max3);

        var row2 = table.GetRow(Fields.Sn, max3);

        row2.Delete();

        var max4 = (int)table.Max(Fields.Sn);

        Assert.AreNotEqual(max3, max4);
    }


    [Test]
    public void TestAutoMaxMinIndexedColumnDeleteRow()
    {
        var table = GetTestTable();

        var max1 = (int)table.Max(Fields.id);
        var min1 = (int)table.Min(Fields.id);
        
        var snMax = (int)table.GetAnyMax(Fields.Sn);

        var row1 = table.GetRowBy(max1);
        var row2 = table.GetRowBy(min1);

        row1.Delete();
        row2.Delete();

        var max2 = (int)table.Max(Fields.id);
        var min2 = (int)table.Min(Fields.id);

        Assert.AreNotEqual(max1, max2);
        Assert.AreNotEqual(min1, min2);
            
        var maxMax = (int)table.GetAnyMax(Fields.Sn);
        var maxAfterDelete = (int)table.Max(Fields.id);
            
        Assert.AreEqual(max1 - 1, maxAfterDelete);
        Assert.AreEqual(snMax, maxMax);
    }

    [Test]
    public void TestAutoIncrement()
    {
        var table = GetTestTable();

        var maxSn = (int)table.GetAnyMax(Fields.Sn);
        var maxId = (int)table.Max(Fields.id);

        maxId++;

        var dataEdit = table.StartTransaction();

        IDataRowAccessor lastRow = null;
        for (int i = 0; i < 30; i++)
        {
            lastRow = table.NewRow();

            lastRow.Set(Fields.id, maxId++);
                
            lastRow["UserClass"] =  new ComplexUserClass() { ID = lastRow.Field<int>(Fields.id) };

            table.AddRow(lastRow);
        }

        dataEdit.Commit();

        Assert.AreEqual(maxSn + 30, lastRow.Field<int>(Fields.Sn));
    }

    [Test]
    public void TestRowUnique()
    {
        var table = GetTestTable();

        var row = table.GetRowBy(15);

        row[Fields.id] = 666666;

        Assert.Null(table.GetRowBy(15));

        row[Fields.id] = 15;

        Assert.NotNull(table.GetRowBy(15));

        Assert.Throws<ConstraintException>(() => row[Fields.id] = 1);
    }

    [Test]
    public void TestRowError()
    {
        var table = GetTestTable();

        var row1 = table.GetRowBy(5);
        var row3 = table.GetRowBy(6);

        row1.SetRowFault("Fault 1");
        row1.SetRowError("Error 1");
        row1.SetRowWarning("Warning 1");
        row1.SetRowInfo("Info 1");

        row3.SetRowFault("Fault 2");
        row3.SetRowError("Error 2");
        row3.SetRowWarning("Warning 2");
        row3.SetRowInfo("Info 2");
            
        Assert.AreEqual("Error 1", row1.GetRowError());
        Assert.AreEqual("Fault 1", row1.GetRowFault());
        Assert.AreEqual("Warning 1", row1.GetRowWarning());
        Assert.AreEqual("Info 1", row1.GetRowInfo());

        Assert.AreEqual("Fault 2", row3.GetRowFault());
        Assert.AreEqual("Error 2", row3.GetRowError());
        Assert.AreEqual("Warning 2", row3.GetRowWarning());
        Assert.AreEqual("Info 2", row3.GetRowInfo());

        var row2 = table.GetRowBy(7);

        Assert.IsEmpty(row2.GetRowError());
        Assert.IsEmpty(row2.GetRowWarning());
        Assert.IsEmpty(row2.GetRowInfo());

        var tableCopy = table.Copy();

        var cloneTableRow1 = tableCopy.GetRowBy(5);

        Assert.AreEqual("Fault 1", cloneTableRow1.GetRowFault());
        Assert.AreEqual("Error 1", cloneTableRow1.GetRowError());
        Assert.AreEqual("Warning 1", cloneTableRow1.GetRowWarning());
        Assert.AreEqual("Info 1", cloneTableRow1.GetRowInfo());

        var cloneTableRow2 = tableCopy.GetRowBy(9);

        Assert.IsEmpty(cloneTableRow2.GetRowFault());
        Assert.IsEmpty(cloneTableRow2.GetRowError());
        Assert.IsEmpty(cloneTableRow2.GetRowWarning());
        Assert.IsEmpty(cloneTableRow2.GetRowInfo());

        var cloneTableRow3 = tableCopy.GetRowBy(6);

        Assert.AreEqual("Fault 2", cloneTableRow3.GetRowFault());
        Assert.AreEqual("Error 2", cloneTableRow3.GetRowError());
        Assert.AreEqual("Warning 2", cloneTableRow3.GetRowWarning());
        Assert.AreEqual("Info 2", cloneTableRow3.GetRowInfo());

        cloneTableRow3.SetRowFault(null);
        cloneTableRow3.SetRowError(null);
        cloneTableRow3.SetRowInfo(null);
        cloneTableRow3.SetRowWarning(null);

        Assert.IsEmpty(cloneTableRow3.GetRowFault());
        Assert.IsEmpty(cloneTableRow3.GetRowError());
        Assert.IsEmpty(cloneTableRow3.GetRowWarning());
        Assert.IsEmpty(cloneTableRow3.GetRowInfo());
    }


    [Test]
    public void TestRowCellChange()
    {
        var table = GetTestTable();

        var row1 = table.GetRowBy(5);

        table.AcceptChanges();

        Assert.False(row1.IsChanged(Fields.Name));
        Assert.False(row1.IsChanged(Fields.Sn));

        row1.Set(Fields.Name, "12321");
        row1.Set(Fields.Sn, 5);

        Assert.True(row1.IsChanged(Fields.Name));
        Assert.True(row1.IsChanged(Fields.Sn));
    }

    [Test]
    public void TestRowCellToString()
    {
        var testTable = m_sourceDataTable.Copy();

        var dataRow = testTable.GetRowBy(1);

        dataRow.Set(Fields.id, 5000000);
            
        var idStr = dataRow.ToString(Fields.id, "C", NumberFormatInfo.InvariantInfo);

        var expected = 5000000.ToString("C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        idStr = dataRow.ToString(testTable.GetColumn(Fields.id), "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        idStr = dataRow.ToString(m_sourceDataTable.GetColumn(Fields.id), "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);

        var dataRowContainer = dataRow.ToContainer();
            
        idStr = dataRowContainer.ToString(Fields.id, "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        idStr = dataRowContainer.ToString(dataRowContainer.GetColumn(Fields.id), "C", NumberFormatInfo.InvariantInfo);

        Assert.AreEqual(expected, idStr);

        dataRow.DetachRow();

        idStr = dataRow.ToString(Fields.id, "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        idStr = dataRow.ToString(testTable.GetColumn(Fields.id), "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        idStr = dataRow.ToString(m_sourceDataTable.GetColumn(Fields.id), "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        var readOnlyDataContainer = dataRow.ToContainer();
            
        idStr = readOnlyDataContainer.ToString(Fields.id, "C", NumberFormatInfo.InvariantInfo);
            
        Assert.AreEqual(expected, idStr);
            
        idStr = readOnlyDataContainer.ToString(readOnlyDataContainer.GetColumn(Fields.id), "C", NumberFormatInfo.InvariantInfo);

        Assert.AreEqual(expected, idStr);
    }


    [Test]
    public void TestRowColumnErrorByName()
    {
        var table = GetTestTable();

        var row5 = table.GetRowBy(5);

        row5.SetColumnError(Fields.Name, "Error 1");
        row5.SetColumnWarning(Fields.Name, "Warning 1");
        row5.SetColumnInfo(Fields.Name, "Info 1");

        var row3 = table.GetRowBy(3);

        row3.SetColumnError(Fields.Name, "Error 2");
        row3.SetColumnWarning(Fields.Name, "Warning 2");
        row3.SetColumnInfo(Fields.Name, "Info 2");

        Assert.AreEqual("Error 1", row5.GetCellError(Fields.Name));
        Assert.AreEqual("Warning 1", row5.GetCellWarning(Fields.Name));
        Assert.AreEqual("Info 1", row5.GetCellInfo(Fields.Name));

        Assert.IsEmpty(row5.GetCellError(Fields.Sn));
        Assert.IsEmpty(row5.GetCellWarning(Fields.Sn));
        Assert.IsEmpty(row5.GetCellInfo(Fields.Sn));

        Assert.AreEqual("Error 2", row3.GetCellError(Fields.Name));
        Assert.AreEqual("Warning 2", row3.GetCellWarning(Fields.Name));
        Assert.AreEqual("Info 2", row3.GetCellInfo(Fields.Name));

        Assert.IsEmpty(row3.GetCellError(Fields.Sn));
        Assert.IsEmpty(row3.GetCellWarning(Fields.Sn));
        Assert.IsEmpty(row3.GetCellInfo(Fields.Sn));

        var row6 = table.GetRowBy(6);

        Assert.IsEmpty(row6.GetCellError(Fields.Name));
        Assert.IsEmpty(row6.GetCellWarning(Fields.Name));
        Assert.IsEmpty(row6.GetCellInfo(Fields.Name));

        var tableCopy = table.Copy();

        var cloneRow5 = tableCopy.GetRowBy(5);

        Assert.AreEqual("Error 1", cloneRow5.GetCellError(Fields.Name));
        Assert.AreEqual("Warning 1", cloneRow5.GetCellWarning(Fields.Name));
        Assert.AreEqual("Info 1", cloneRow5.GetCellInfo(Fields.Name));

        Assert.IsEmpty(cloneRow5.GetCellError(Fields.Sn));
        Assert.IsEmpty(cloneRow5.GetCellWarning(Fields.Sn));
        Assert.IsEmpty(cloneRow5.GetCellInfo(Fields.Sn));

        var cloneRow4 = tableCopy.GetRowBy(4);

        Assert.IsEmpty(cloneRow4.GetCellError(Fields.Name));
        Assert.IsEmpty(cloneRow4.GetCellWarning(Fields.Name));
        Assert.IsEmpty(cloneRow4.GetCellInfo(Fields.Name));

        var cloneRow3 = tableCopy.GetRowBy(3);

        Assert.AreEqual("Error 2", cloneRow3.GetCellError(Fields.Name));
        Assert.AreEqual("Warning 2", cloneRow3.GetCellWarning(Fields.Name));
        Assert.AreEqual("Info 2", cloneRow3.GetCellInfo(Fields.Name));

        Assert.IsEmpty(cloneRow3.GetCellError(Fields.Sn));
        Assert.IsEmpty(cloneRow3.GetCellWarning(Fields.Sn));
        Assert.IsEmpty(cloneRow3.GetCellInfo(Fields.Sn));
    }

    [Test]
    public void TestRowColumnErrorByColumn()
    {
        var table = GetTestTable();

        var row1 = table.GetRowBy(3);

        var nameCol = table.GetColumn(Fields.Name);
        var snColumn = table.GetColumn(Fields.Sn);

        row1.SetColumnError(nameCol, "Error 1");
        row1.SetColumnWarning(nameCol, "Warning 1");
        row1.SetColumnInfo(nameCol, "Info 1");

        var row3 = table.GetRowBy(7);

        row3.SetColumnError(nameCol, "Error 2");
        row3.SetColumnWarning(nameCol, "Warning 2");
        row3.SetColumnInfo(nameCol, "Info 2");

        Assert.AreEqual("Error 1", row1.GetColumnError(nameCol));
        Assert.AreEqual("Warning 1", row1.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 1", row1.GetColumnInfo(nameCol));

        Assert.IsEmpty(row1.GetColumnError(snColumn));
        Assert.IsEmpty(row1.GetColumnWarning(snColumn));
        Assert.IsEmpty(row1.GetColumnInfo(snColumn));

        Assert.AreEqual("Error 2", row3.GetColumnError(nameCol));
        Assert.AreEqual("Warning 2", row3.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 2", row3.GetColumnInfo(nameCol));

        Assert.IsEmpty(row3.GetColumnError(snColumn));
        Assert.IsEmpty(row3.GetColumnWarning(snColumn));
        Assert.IsEmpty(row3.GetColumnInfo(snColumn));

        var row2 = table.GetRowBy(5);

        Assert.IsEmpty(row2.GetColumnError(nameCol));
        Assert.IsEmpty(row2.GetColumnWarning(nameCol));
        Assert.IsEmpty(row2.GetColumnInfo(nameCol));

        var clone = table.Copy(null);

        var cloneTableRow1 = clone.GetRowBy(3);

        Assert.AreEqual("Error 1", cloneTableRow1.GetColumnError(nameCol));
        Assert.AreEqual("Warning 1", cloneTableRow1.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 1", cloneTableRow1.GetColumnInfo(nameCol));

        Assert.IsEmpty(cloneTableRow1.GetColumnError(snColumn));
        Assert.IsEmpty(cloneTableRow1.GetColumnWarning(snColumn));
        Assert.IsEmpty(cloneTableRow1.GetColumnInfo(snColumn));

        var cloneTableRow2 = clone.GetRowBy(8);

        Assert.IsEmpty(cloneTableRow2.GetColumnError(nameCol));
        Assert.IsEmpty(cloneTableRow2.GetColumnWarning(nameCol));
        Assert.IsEmpty(cloneTableRow2.GetColumnInfo(nameCol));

        var cloneTableRow3 = clone.GetRowBy(7);

        Assert.AreEqual("Error 2", cloneTableRow3.GetColumnError(nameCol));
        Assert.AreEqual("Warning 2", cloneTableRow3.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 2", cloneTableRow3.GetColumnInfo(nameCol));

        Assert.IsEmpty(cloneTableRow3.GetColumnError(snColumn));
        Assert.IsEmpty(cloneTableRow3.GetColumnWarning(snColumn));
        Assert.IsEmpty(cloneTableRow3.GetColumnInfo(snColumn));
    }

    [Test]
    public void TestRowColumnErrorByColumnHandle()
    {
        var table = GetTestTable();

        var row5 = table.GetRowBy(5);

        var nameCol = table.GetColumnHandle(Fields.Name);
        var snColumn = table.GetColumnHandle(Fields.Sn);

        row5.SetColumnError(nameCol, "Error 1");
        row5.SetColumnWarning(nameCol, "Warning 1");
        row5.SetColumnInfo(nameCol, "Info 1");

        var row15 = table.GetRowBy(15);

        row15.SetColumnError(nameCol, "Error 2");
        row15.SetColumnWarning(nameCol, "Warning 2");
        row15.SetColumnInfo(nameCol, "Info 2");

        Assert.AreEqual("Error 1", row5.GetColumnError(nameCol));
        Assert.AreEqual("Warning 1", row5.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 1", row5.GetColumnInfo(nameCol));

        Assert.IsEmpty(row5.GetColumnError(snColumn));
        Assert.IsEmpty(row5.GetColumnWarning(snColumn));
        Assert.IsEmpty(row5.GetColumnInfo(snColumn));

        Assert.AreEqual("Error 2", row15.GetColumnError(nameCol));
        Assert.AreEqual("Warning 2", row15.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 2", row15.GetColumnInfo(nameCol));

        Assert.IsEmpty(row15.GetColumnError(snColumn));
        Assert.IsEmpty(row15.GetColumnWarning(snColumn));
        Assert.IsEmpty(row15.GetColumnInfo(snColumn));

        var row4 = table.GetRowBy(4);

        Assert.IsEmpty(row4.GetColumnError(nameCol));
        Assert.IsEmpty(row4.GetColumnWarning(nameCol));
        Assert.IsEmpty(row4.GetColumnInfo(nameCol));

        var tableCopy = table.Copy();

        var cloneRow5 = tableCopy.GetRowBy(5);

        Assert.AreEqual("Error 1", cloneRow5.GetColumnError(nameCol));
        Assert.AreEqual("Warning 1", cloneRow5.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 1", cloneRow5.GetColumnInfo(nameCol));

        Assert.IsEmpty(cloneRow5.GetColumnError(snColumn));
        Assert.IsEmpty(cloneRow5.GetColumnWarning(snColumn));
        Assert.IsEmpty(cloneRow5.GetColumnInfo(snColumn));

        var cloneRow8 = tableCopy.GetRowBy(8);

        Assert.IsEmpty(cloneRow8.GetColumnError(nameCol));
        Assert.IsEmpty(cloneRow8.GetColumnWarning(nameCol));
        Assert.IsEmpty(cloneRow8.GetColumnInfo(nameCol));

        var cloneRow15 = tableCopy.GetRowBy(15);

        Assert.AreEqual("Error 2", cloneRow15.GetColumnError(nameCol));
        Assert.AreEqual("Warning 2", cloneRow15.GetColumnWarning(nameCol));
        Assert.AreEqual("Info 2", cloneRow15.GetColumnInfo(nameCol));

        Assert.IsEmpty(cloneRow15.GetColumnError(snColumn));
        Assert.IsEmpty(cloneRow15.GetColumnWarning(snColumn));
        Assert.IsEmpty(cloneRow15.GetColumnInfo(snColumn));
    }

    [Test]
    public void TestRowAcceptChanges()
    {
        var table = GetTestTable();

        var row = table.GetRowBy(5);
        var changedRow = table.GetRowBy(3);

        var expected = changedRow.Field<string>(Fields.Name);

        changedRow.Set(Fields.Name, "3333");

        Assert.AreEqual("3333", changedRow.Field<string>(Fields.Name));

        Assert.True(row.RowRecordState == RowState.Unchanged);
        Assert.True(changedRow.RowRecordState == RowState.Modified);

        Assert.AreEqual(expected, changedRow.GetOriginalValue(Fields.Name));

        table.AcceptChanges();

        Assert.AreEqual("3333", changedRow.GetOriginalValue(Fields.Name));

        Assert.True(changedRow.RowRecordState == RowState.Unchanged);
        Assert.True(row.RowRecordState == RowState.Unchanged);

        var name = row.Field<string>(Fields.Name);

        var originalValue = row.GetOriginalValue(Fields.Name);

        Assert.AreEqual(name, originalValue);

        row.Set(Fields.Name, "1111").Set(Fields.Name, "2222");

        Assert.True(row.RowRecordState == RowState.Modified);

        Assert.AreEqual("2222", row.Field<string>(Fields.Name));

        Assert.AreEqual(name, row.GetOriginalValue(Fields.Name));

        table.AcceptChanges();

        Assert.True(changedRow.RowRecordState == RowState.Unchanged);
        Assert.AreEqual("3333", changedRow.GetOriginalValue(Fields.Name));

        Assert.AreEqual("2222", row.GetOriginalValue(Fields.Name));
        Assert.AreEqual("2222", row.Field<string>(Fields.Name));
    }

    [Test]
    public void TestTableRejectChanges()
    {
        var table = GetTestTable();

        Assert.False(table.HasChanges());

        table.GetRowBy(5).Set(Fields.Name, "1111").Set(Fields.Name, "2222").Set(Fields.Sn, 33333);
        table.GetRowBy(6).Set(Fields.Name, "1111").Set(Fields.Name, "2222").Set(Fields.Sn, 33333);
        table.GetRowBy(7).Set(Fields.Name, "1111").Set(Fields.Name, "2222").Set(Fields.Sn, 33333);

        Assert.True(table.HasChanges());

        table.RejectChanges();

        Assert.False(table.HasChanges());

        Assert.AreNotEqual("2222", table.GetRowBy(5).Field<string>(Fields.Name));
        Assert.AreNotEqual("2222", table.GetRowBy(6).Field<string>(Fields.Name));
        Assert.AreNotEqual("2222", table.GetRowBy(7).Field<string>(Fields.Name));

        Assert.AreNotEqual(33333, table.GetRowBy(5).Field<int>(Fields.Sn));
        Assert.AreNotEqual(33333, table.GetRowBy(6).Field<int>(Fields.Sn));
        Assert.AreNotEqual(33333, table.GetRowBy(7).Field<int>(Fields.Sn));
    }

    [Test]
    public void TestRowRejectChanges()
    {
        var table = GetTestTable();

        var row = table.GetRowBy(15);

        var name = row.Field<string>(Fields.Name);

        var originalValue = row.GetOriginalValue(Fields.Name);

        Assert.AreEqual(name, originalValue);

        row.Set(Fields.Name, "1111").Set(Fields.Name, "2222");

        Assert.AreEqual("2222", row.Field<string>(Fields.Name));

        Assert.AreEqual(name, row.GetOriginalValue(Fields.Name));

        table.RejectChanges();

        Assert.AreEqual(name, row.Field<string>(Fields.Name));
    }

    [Test]
    public void TestRemoveIndex()
    {
        var table = GetTestTable();

        Assert.True(table.HasIndex(Fields.id));

        table.RemoveIndex(Fields.id);

        Assert.False(table.HasIndex(Fields.id));

        Assert.Null(table.GetRowBy(-5));

        var row = table.GetRow(Fields.id, 5);

        Assert.NotNull(row);

        Assert.AreEqual(row.Field<int>(Fields.id), 5);
    }

    [Test]
    public void TestRemoveAllColumnsForward()
    {
        TestRemoveColumns(true);
    }

    [Test]
    public void TestRemoveAllColumnsBackward()
    {
        TestRemoveColumns(false);
    }

    private void TestRemoveColumns(bool forward)
    {
        int RemoveCols(List<CoreDataColumnContainer> dataColumns, DataTable table1, int colCount, DataRow dataRow)
        {
            foreach (var column in dataColumns)
            {
                if (table1.CanRemoveColumn(column.ColumnName))
                {
                    Assert.True(column.HasXProperty(column.ColumnName));
                    
                    var tableCol = table1.GetColumn(column.ColumnName);

                    table1.RemoveColumn(tableCol.ColumnName);

                    colCount--;

                    Assert.AreEqual(colCount, table1.ColumnCount);

                    Assert.False(dataRow.IsExistsField(column.ColumnName), column.ColumnName);

                    if (dataRow.HasXProperty(column.ColumnName))
                    {
                        continue;
                    }

                    Assert.Throws<MissingMetadataException>(() =>
                    {
                        var o = dataRow[tableCol.ColumnName];
                    }, column.ColumnName);

                    //todo restore
                    /*Assert.Throws<DataDetachedException>(() =>
                    { 
                        tableCol.Caption = "";
                    }, column.ColumnName);*/
                }
            }

            return colCount;
        }

        var table = new DataTable();

        table.AddColumn(Fields.id);
        table.AddColumn(Fields.Code);
        table.AddColumn(Fields.groupid, builtin: true);
        table.AddColumn(Fields.parentid);
        table.AddColumn(Fields.Sn);
        table.AddColumn(Fields.Directories.CreateDt);

        table.AddIndex(Fields.Code);
        table.AddIndex(Fields.id);
        
        table.AddMultiColumnIndex(new string[] {Fields.id, Fields.Code});
        
        Assert.False(table.CanRemoveColumn(Fields.id));
        Assert.False(table.CanRemoveColumn(Fields.Code));
        Assert.False(table.CanRemoveColumn(Fields.groupid));
        
        table.RemoveMultiColumnIndex(new string[] {Fields.id, Fields.Code});
        
        Assert.True(table.CanRemoveColumn(Fields.id));
        Assert.True(table.CanRemoveColumn(Fields.Code));
        Assert.False(table.CanRemoveColumn(Fields.groupid));
        
        table.AddRow(_.MapObj((Fields.id, 1), (Fields.Code, 1), (Fields.groupid, 1)));

        var row = table.Rows.First();

        foreach (var column in table.GetColumns())
        {
            column.SetXProperty(column.ColumnName, column.Ordinal);
        }

        var columns = table.GetColumns().Select(c => new CoreDataColumnContainer(c)).ToList();

        var columnCount = table.ColumnCount;

        if (forward == false)
        {
            columns.Reverse();
            
            columnCount = RemoveCols(columns, table, columnCount, row);
        }
        else
        {
            columnCount = RemoveCols(columns, table, columnCount, row);
        }
        
        //groupid is buildit and cannot be removed
        Assert.AreEqual(1, columnCount);

        var dataColumn = table.GetColumn(Fields.groupid);
        
        Assert.AreEqual(0, dataColumn.ColumnHandle);
    }

    [Test]
    public void TestMinMax()
    {
        var table = GetTestTable();

        var minId = (int)table.Min(Fields.id);
        var maxId = (int)table.Max(Fields.id);

        Assert.Greater(maxId, minId);

        var idCol = table.GetColumn(Fields.id);

        Assert.True(table.HasIndex(Fields.id));
        Assert.True(table.HasIndex(idCol.ColumnHandle));

        var minId1 = (int)table.Min(idCol);
        var maxId1 = (int)table.Max(idCol);

        Assert.Greater(maxId1, minId1);

        Assert.AreEqual(minId, minId1);
        Assert.AreEqual(maxId, maxId1);

        var mingroupid = (int)table.Min(Fields.groupid);
        var maxgroupid = (int)table.Max(Fields.groupid);

        Assert.Greater(maxgroupid, mingroupid);

        var minCreateDt = (DateTime)table.Min(Fields.Directories.CreateDt);
        var maxCreateDt = (DateTime)table.Max(Fields.Directories.CreateDt);

        Assert.Greater(maxCreateDt, minCreateDt);

        var minSn = (int)table.Min(Fields.Sn, new Tuple<string, IComparable>(Fields.groupid, mingroupid));
        var maxSn = (int)table.Max(Fields.Sn, new Tuple<string, IComparable>(Fields.groupid, maxgroupid));

        Assert.Greater(minSn, 0);
        Assert.Greater(maxSn, 0);
    }

    [Test]
    public void TestGetFieldsByName()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        var id = (int)dataRow[Fields.id];

        var id1 = dataRow.Field<int?>(Fields.id);
        var id3 = dataRow.FieldNotNull<int>(Fields.id);
        var id2 = dataRow.Field<int>(Fields.id);
        var id4 = dataRow.FieldNotNull<long>(Fields.id);
        var id5 = dataRow.FieldNotNull<double>("Expr2");

        Assert.AreEqual(id, id1);
        Assert.AreEqual(id, id3);
        Assert.AreEqual(id, id2);
        Assert.AreEqual((double)(id + id), id5);

        var idColumn = table.GetColumn(Fields.id);

        var id1ByCol = dataRow.Field<int?>(idColumn);
        var id3ByCol = dataRow.FieldNotNull<int>(idColumn);
        var id2ByCol = dataRow.Field<int>(idColumn);

        Assert.AreEqual(id, id1ByCol);
        Assert.AreEqual(id, id3ByCol);
        Assert.AreEqual(id, id2ByCol);

        var idHandle = new ColumnHandle(idColumn.Ordinal);

        var id1ByColH = dataRow.Field<int?>(idHandle);
        var id3ByColH = dataRow.FieldNotNull<int>(idHandle);
        var id2ByColH = dataRow.Field<int>(idHandle);

        Assert.AreEqual(id, id1ByColH);
        Assert.AreEqual(id, id3ByColH);
        Assert.AreEqual(id, id2ByColH);

        Assert.Throws<ConstraintException>(() => dataRow.SetNull(Fields.id));
        Assert.Throws<ConstraintException>(() => dataRow.SetNull(idColumn));
        Assert.Throws<ConstraintException>(() => dataRow.SetNull(idHandle));

        dataRow.SetNull(Fields.groupid);

        id1 = dataRow.Field<int?>(Fields.groupid);
        id3 = dataRow.FieldNotNull<int>(Fields.groupid);
        id2 = dataRow.Field<int>(Fields.groupid);

        Assert.AreEqual(null, id1);
        Assert.AreEqual(null, dataRow[Fields.groupid]);
        Assert.AreEqual(0, id3);
        Assert.AreEqual(0, id2);
    }

    [Test]
    public void TestGetFieldsByHandle()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        var idHandle = table.GetColumnHandle(Fields.id);
        var groupIdHandle = table.GetColumnHandle(Fields.groupid); 

        var id = (int)dataRow[idHandle];

        var id1 = dataRow.Field<int?>(idHandle);
        var id3 = dataRow.FieldNotNull<int>(idHandle);
        var id2 = dataRow.Field<int>(idHandle);

        Assert.AreEqual(id, id1);
        Assert.AreEqual(id, id3);
        Assert.AreEqual(id, id2);
         
        Assert.Throws<ConstraintException>(() => dataRow.SetNull(idHandle));

        dataRow.SetNull(groupIdHandle);

        id1 = dataRow.Field<int?>(groupIdHandle);
        id3 = dataRow.FieldNotNull<int>(groupIdHandle);
        id2 = dataRow.Field<int>(groupIdHandle);

        Assert.AreEqual(null, id1);
        Assert.AreEqual(null, dataRow[groupIdHandle]);
        Assert.AreEqual(0, id3);
        Assert.AreEqual(0, id2);
    }

    [Test]
    public void TestGetFieldsByDataColumn()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        var idColumn = table.GetColumn(Fields.id);
        var groupIdColumn = table.GetColumn(Fields.groupid);

        var id = (int)dataRow[idColumn];

        var id1 = dataRow.Field<int?>(idColumn);
        var id3 = dataRow.FieldNotNull<int>(idColumn);
        var id2 = dataRow.Field<int>(idColumn);

        Assert.AreEqual(id, id1);
        Assert.AreEqual(id, id3);
        Assert.AreEqual(id, id2);

        Assert.Throws<ConstraintException>(() => dataRow.SetNull(idColumn));

        dataRow.SetNull(groupIdColumn);

        id1 = dataRow.Field<int?>(groupIdColumn);
        id3 = dataRow.FieldNotNull<int>(groupIdColumn);
        id2 = dataRow.Field<int>(groupIdColumn);

        Assert.AreEqual(null, id1);
        Assert.AreEqual(null, dataRow[groupIdColumn]);
        Assert.AreEqual(0, id3);
        Assert.AreEqual(0, id2);
    }

    [Test]
    public void TestDefaultValues()
    {
        var table = GetTestTable();

        var row = table.Rows.First();

        row.SetNull(Fields.Sn);

        Assert.IsNull(row.Field<int?>(Fields.Sn));

        Assert.IsNotNull(row.Field<int?>(Fields.Sn, 0));

        row.SetNull(Fields.Name);

        Assert.True(row.Field<string>(Fields.Name) == string.Empty);

        Assert.Null(row.Field<string>(Fields.Name, null));

        Assert.AreEqual(row.Field<string>(Fields.Name, "12345"), "12345");
    }

    [Test]
    public void TestAddEmptyRow()
    {
        var table = GetTestTable();

        var count = table.RowCount;

        var beginEdit = table.StartTransaction();

        var rowAccessor = table.NewRow().Set(Fields.id, 555555).Set("UserClass", new ComplexUserClass() { ID = 555555 }).Set(Fields.Name, "Test");
            
        table.AddRow(rowAccessor);
        table.AddRow(table.NewRow().Set(Fields.id, 444444).Set("UserClass", new ComplexUserClass() { ID = 444444 }).Set(Fields.Name, "Test2"));
        table.AddRow(table.NewRow().Set(Fields.id, 333333).Set("UserClass", new ComplexUserClass() { ID = 333333 }).Set(Fields.Name, "Test3"));

        beginEdit.Commit();

        Assert.AreEqual(count + 3, table.RowCount);

        Assert.NotNull(table.GetRowBy(555555));
        Assert.NotNull(table.GetRowBy(444444));
        Assert.NotNull(table.GetRowBy(333333));
    }

    [Test]
    public void TestSetFieldsByName()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        dataRow[Fields.groupid] = 5;

        Assert.AreEqual(dataRow[Fields.groupid], 5);

        dataRow.Set(Fields.groupid, 6);

        Assert.AreEqual(dataRow.FieldNotNull<int>(Fields.groupid), 6);

        dataRow.SetNull(Fields.groupid);

        Assert.True(dataRow.IsNull(Fields.groupid));
        Assert.Null(dataRow[Fields.groupid]);
        Assert.Null(dataRow.Field<int?>(Fields.groupid));

        Assert.AreEqual(default(int), dataRow.FieldNotNull<int>(Fields.groupid));

        dataRow.Set(Fields.Name, "123123");

        Assert.AreEqual(dataRow.Field<string>(Fields.Name), "123123");

        dataRow.Set(Fields.groupid, new int?(5));

        Assert.AreEqual(dataRow.Field<int>(Fields.groupid), 5);

        Assert.Throws<InvalidCastException>(() => dataRow.Set(Fields.groupid, "12123_werwer"));
    }

    [Test]
    public void TestSetFieldsByColumn()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        var groupIdColumn = table.GetColumn(Fields.groupid);
        var nameColumn = table.GetColumn(Fields.Name);

        dataRow[groupIdColumn] = 5;

        Assert.AreEqual(dataRow[groupIdColumn], 5);

        dataRow.Set(groupIdColumn, 6);

        Assert.AreEqual(dataRow.FieldNotNull<int>(groupIdColumn), 6);

        dataRow.SetNull(groupIdColumn);

        Assert.True(dataRow.IsNull(groupIdColumn));
        Assert.Null(dataRow[groupIdColumn]);
        Assert.Null(dataRow.Field<int?>(groupIdColumn));

        Assert.AreEqual(default(int), dataRow.FieldNotNull<int>(groupIdColumn));

        dataRow.Set(nameColumn, "123123");

        Assert.AreEqual(dataRow.Field<string>(nameColumn), "123123");

        dataRow.Set(groupIdColumn, new int?(5));

        Assert.AreEqual(dataRow.Field<int>(groupIdColumn), 5);

        Assert.Throws<InvalidCastException>(() => dataRow.Set(groupIdColumn, "12123_werwer"));
    }

    [Test]
    public void TestSetFieldsByColumnHandle()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        var groupIdColumn = table.GetColumnHandle(Fields.groupid);
        var nameColumn = table.GetColumnHandle(Fields.Name);

        dataRow[groupIdColumn] = 5;

        Assert.AreEqual(dataRow[groupIdColumn], 5);

        dataRow.Set(groupIdColumn, 6);

        Assert.AreEqual(dataRow.FieldNotNull<int>(groupIdColumn), 6);

        dataRow.SetNull(groupIdColumn);

        Assert.True(dataRow.IsNull(groupIdColumn));
        Assert.Null(dataRow[groupIdColumn]);
        Assert.Null(dataRow.Field<int?>(groupIdColumn));

        Assert.AreEqual(default(int), dataRow.FieldNotNull<int>(groupIdColumn));

        dataRow.Set(nameColumn, "123123");

        Assert.AreEqual(dataRow.Field<string>(nameColumn), "123123");

        dataRow.Set(groupIdColumn, new int?(5));

        Assert.AreEqual(dataRow.Field<int>(groupIdColumn), 5);

        Assert.Throws<InvalidCastException>(() => dataRow.Set(groupIdColumn, "12123_werwer"));
    }

    [Test]
    public void TestLookingUpWithoutIndex()
    {
        var table = GetTestTable();

        var last = table.Rows.Last();

        foreach (var column in table.GetColumns())
        {
            if (column.HasIndex)
            {
                continue;
            }

            var comparable = last[column] as IComparable;

            if (comparable != null)
            {
                var one1 = table.GetRow(column.ColumnHandle, comparable);

                Assert.NotNull(one1, column.ColumnName);

                var one2 = table.GetRow(column.ColumnName, comparable);

                Assert.NotNull(one2, column.ColumnName);

                var dataTableRows = table.GetRows(column.ColumnHandle, comparable);

                var any = dataTableRows.Any(row => row.Equals(last));

                Assert.True(any);
            }
        }
    }


    [Test]
    public void TestLookingUpUsingIndAndStringIndex()
    {
        var table = GetTestTable();

        for (int id = 1; id < rowCount; id++)
        {
            var dataRow = table.GetRowBy(id);

            Assert.NotNull(dataRow);

            Assert.AreEqual(dataRow.Field<string>(Fields.Name), "Name " + id);
        }

        Assert.Null(table.GetRowBy(short.MinValue));
        Assert.Null(table.GetRowBy(ushort.MaxValue));

        table.AddIndex(Fields.Code);

        Assert.Greater(table.GetRows(Fields.Directories.Code, "Code 0").Count(), 1);

        for (int i = 1; i < 21; i++)
        {
            var id = i;

            var dataRow = table.ImportRow(table.NewRow().Set(Fields.id, rowCount + id).Set("UserClass", new ComplexUserClass() { ID = rowCount + id }).Set(Fields.Sn, i).Set(Fields.Name, "Name " + id).Set(Fields.Code, "A"));
        }

        Assert.AreEqual(20, table.GetRows(Fields.Directories.Code, "A").Count());
    }

    [Test]
    public void TestLookingUpWithoutIndexHandles()
    {
        var table = GetTestTable();

        var last = table.Rows.Last();

        foreach (var column in table.GetColumns())
        {
            if (column.HasIndex)
            {
                continue;
            }

            var comparable = last[column] as IComparable;

            if (comparable != null)
            {
                var one1 = table.GetRowHandle(column.ColumnName, comparable);

                Assert.NotNull(one1);

                var dataTableRows = table.FindManyHandles(column.ColumnHandle, comparable);

                var any = dataTableRows.Any(row => table.GetRowByHandle(row).Equals(last));

                Assert.True(any);
            }
        }
    }


    [Test]
    public void TestLookingUpUsingIndAndStringIndexHandles()
    {
        var table =  GetTestTable();

        for (int id = 1; id < rowCount; id++)
        {
            var row = table.GetRowByHandle(table.GetRowHandle(id));

            Assert.NotNull(row);

            Assert.AreEqual(row.Field<string>(Fields.Name), "Name " + id);
        }

        Assert.True(table.GetRowHandle(short.MinValue) < 0);
        Assert.True(table.GetRowHandle(ushort.MaxValue) < 0);

        table.AddIndex(Fields.Code);

        Assert.Greater(table.FindManyHandles(table.GetColumn(Fields.Directories.Code).ColumnHandle, "Code 0").Count(), 1);

        for (int i = 1; i < 21; i++)
        {
            var id = i;

            var dataRow = table.ImportRow(table.NewRow().Set(Fields.id, rowCount + id).Set("UserClass", new ComplexUserClass() { ID = rowCount + id }).Set(Fields.Code, "A"));
        }

        Assert.AreEqual(20, table.FindManyHandles(table.GetColumn(Fields.Directories.Code).ColumnHandle, "A").Count());
    }


    [Test]
    public void TestRowStates([Values(true, false)] bool lockEvents)
    {
        var table =  GetTestTable();

        var changedCount = 0;
        var changingCount = 0;
        var rowChangedCount = 0;
            
        table.SubscribeColumnChanged(Fields.Directories.LmId, (args, c) =>
        {
            changedCount++;
        });
        table.SubscribeColumnChanging<int>(Fields.Directories.LmId, (args, c) =>
        {
            changingCount++;
        });
            
        table.DataRowChanged.Subscribe((args, c) => rowChangedCount++);
        table.ColumnChanged.Subscribe((args, c) => changedCount++);
        table.ColumnChanging.Subscribe((args, c) => changingCount++);

        IDataLockEventState dataLockEventState = null;
            
        if (lockEvents)
        {
            dataLockEventState = table.LockEvents();
        }
            
        foreach (var row in table.Rows.Take(1))
        {
            Assert.False(row.IsAddedRow);
            Assert.False(row.IsModified);
            Assert.AreEqual(row.RowRecordState, RowState.Unchanged);
            
            row[Fields.Directories.LmId] = 1;

            Assert.False(row.IsAddedRow);
            Assert.False(row.IsModified);
            Assert.AreEqual(row.RowRecordState, RowState.Unchanged);
            Assert.AreEqual(1,  row[Fields.Directories.LmId]);

            row.SetField(Fields.Directories.LmId, 2);

            Assert.False(row.IsAddedRow);
            Assert.False(row.IsModified);
            Assert.AreEqual(row.RowRecordState, RowState.Unchanged);
            Assert.AreEqual(2,  row.Field<int>(Fields.Directories.LmId));

            row.SetField(Fields.Directories.LmId, new int?(3));

            Assert.False(row.IsAddedRow);
            Assert.False(row.IsModified);
            Assert.AreEqual(row.RowRecordState, RowState.Unchanged);
            Assert.AreEqual(3,  row.Field<int?>(Fields.Directories.LmId));
                
            row[Fields.Directories.LmId] = "4";

            Assert.False(row.IsAddedRow);
            Assert.False(row.IsModified);
            Assert.AreEqual(row.RowRecordState, RowState.Unchanged);
            Assert.AreEqual(4,  row.Field<int?>(Fields.Directories.LmId));
            Assert.AreEqual("4",  row.Field<string>(Fields.Directories.LmId));
            Assert.AreEqual(4,  row[Fields.Directories.LmId]);
                
            Assert.False(row.CanChangeTo(Fields.Directories.LmId, DateTime.Now, out var reason));
            Assert.IsNotEmpty(reason);
                
            Assert.False(row.CanChangeTo(Fields.id, null, out reason));
            Assert.IsNotEmpty(reason);
        }

        table.GetColumn(Fields.groupid).IsReadOnly = true;
            
        Assert.False(table.GetRow(1).CanChangeTo(Fields.groupid, null, out var reason1));
        Assert.IsNotEmpty(reason1);
            
        Assert.AreEqual(0, changedCount);
        Assert.AreEqual(0, changingCount);
            
        if (lockEvents)
        {
            dataLockEventState.UnlockEvents();
        }
            
        Assert.AreEqual(0, changedCount);
        Assert.AreEqual(0, changingCount);
    }

    [Test]
    public void TestDeleteFirst()
    {
        var table = GetTestTable();

        Assert.AreEqual(rowCount, table.RowCount);

        var deletedRow = table.Rows.First();

        TestDeleteAndAdd(deletedRow, table, rowCount - 1);
    }

    [Test]
    public void TestDeleteAll()
    {
        var table = GetTestTable();

        foreach (var row in table.Rows.ToData())
        {
            row.Delete();
        }

        Assert.Null(table.Rows.FirstOrDefault());
    }

    [Test]
    public void TestDeleteLast([Values(true, false)] bool acceptChanges)
    {
        var table = GetTestTable();

        if (acceptChanges)
        {
            table.AcceptChanges();
        }

        Assert.AreEqual(rowCount, table.RowCount);

        var deletedRow = table.Rows.Last();

        TestDeleteAndAdd(deletedRow, table, rowCount - 1);
    }

    [Test]
    public void TestDeleteSome()
    {
        var table = GetTestTable();

        table.AddIndex(Fields.Code);

        Assert.AreEqual(rowCount, table.RowCount);

        var deletedRows = table.Rows.Skip(50).Take(50).ToArray();

        for (int index = 0; index < deletedRows.Length; index++)
        {
            var deletedRow = deletedRows[index];

            deletedRow.Delete();

            Assert.AreEqual(table.RowCount, rowCount - index - 1);

            Assert.AreEqual(table.Rows.Count(), table.RowCount);

            Assert.False(deletedRow.IsAddedRow);
            Assert.True(deletedRow.HasChanges);
            Assert.True(deletedRow.IsDeletedRow);
            Assert.AreEqual(deletedRow.RowRecordState, RowState.Deleted);

            Assert.AreEqual(table.AllRows.Count(), rowCount);

            Assert.IsNotEmpty(deletedRow.Field<string>(Fields.Name));

            Assert.Null(table.GetRowBy(deletedRow.Field<int>(Fields.id)));
            Assert.Null(table.GetRow(Fields.Name, deletedRow.Field<string>(Fields.Name)));

            Assert.IsEmpty(table.GetRows(Fields.id, deletedRow.Field<int>(Fields.id)));
            Assert.IsEmpty(table.GetRows(Fields.Name, deletedRow.Field<string>(Fields.Name)));
        }

        table.AcceptChanges();

        Assert.AreEqual(rowCount - deletedRows.Length, table.AllRows.Count());

        for (int index = 0; index < deletedRows.Length; index++)
        {
            var detachedRow = deletedRows[index];

            Assert.True(detachedRow.RowRecordState == RowState.Detached);

            Assert.Throws<InvalidOperationException>(() => detachedRow.Field<string>(Fields.Name));
        }
    }

    [Test]
    public void TestEvents([Values(true, false)] bool copyWithEvents)
    {
        var table = GetTestTable();


        var addingRaised = false;
        var addedRaised = false;
        var nameColumnChangingRaised = false;
        var nameColumnChangedRaised = false;
        var rowDeletingRaised = false;
        var rowDeletedRaised = false;
        var dataRowChangedRaised = false;

        var dataChangingCanceled = false;
        var dataRowDeleteingCanceled = false;
            
        var dataChangingCanceledInt = false;

        table.RowAdding.Subscribe((args, c) =>
        {
            addingRaised = true;
        });

        table.RowAdded.Subscribe((args, c) =>
        {
            addedRaised = true;
        });

        table.ColumnChanging.Subscribe((args, c) =>
        {
            Assert.False(args.IsCancel);
                        
            nameColumnChangingRaised = true;
                        
            if (args.ColumnName == Fields.Name)
            {
                if ((string)args.NewValue == "3333")
                {
                    Assert.AreNotEqual(args.OldValue, args.NewValue);

                    dataChangingCanceled = true;

                    args.IsCancel = true;
                }
            }
                        
            if (args.ColumnName == Fields.Sn)
            {
                if ((int)args.NewValue == 3333)
                {
                    Assert.AreNotEqual(args.OldValue, args.NewValue);

                    dataChangingCanceledInt = true;

                    args.IsCancel = true;
                }
            }

            if (args.ColumnName == Fields.id)
            {
                if ((int)args.NewValue < 0)
                {
                    args.ErrorMessage = "Cannot be negative";
                    args.IsCancel = true;
                }
            }
        });

        table.ColumnChanged.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.IsNotEmpty(args.ChangedColumnNames);
            Assert.NotNull(args.Row);

            foreach (var columnName in args.ChangedColumnNames)
            {
                if(columnName == "Expr1")
                {
                    
                }
                
                var oldValue = args.GetOldValue(columnName);

                var newValue = args.GetNewValue(columnName);
                
                Assert.AreNotEqual(oldValue, newValue, $"Column changed: {columnName}");
            }
                    
            nameColumnChangedRaised = true;
        });

        table.RowDeleted.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                        
            Assert.IsNotEmpty(args.DeletedRows);
                        
            rowDeletedRaised = true;
        });
            
        table.RowDeleting.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                        
            Assert.False(args.IsCancel);
                        
            rowDeletingRaised = true;

            if (args.Rows.Any())
            {
                if (args.Rows.First().Field<int?>(Fields.id) == 7777)
                {
                    dataRowDeleteingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.DataRowChanged.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.NotNull(args.Row);

            dataRowChangedRaised = true;
        });

        table.SubscribeColumnChanging<int>(Fields.Sn, (args, c) =>
        {
            Assert.False(args.NewValueIsNull);

            if (args.NewValue == 4444)
            {
                Assert.AreNotEqual(args.OldValue, args.NewValue);

                dataChangingCanceledInt = true;

                args.IsCancel = true;
            }

            if (args.NewValue < 0)
            {
                args.ErrorMessage = "Cannot be negative";
                args.IsCancel = true;
            }
        });

        if (copyWithEvents)
        {
            var t = GetTestTable();

            table.AttachEventHandlersTo(t);
                
            table = t;
        }

        var xPropertyChanging = false;
        var xPropertyChanged = false;

        table.RowXPropertyChanging.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);
            Assert.NotNull(args.Row);
            Assert.False(args.IsCancel);
            Assert.AreNotEqual(args.GetOldValue<string>(), args.GetNewValue<string>());
                        
            xPropertyChanging = xPropertyChanging | args.PropertyCode == "TestP";

            if (args.PropertyCode == "TestP")
            {
                args.IsCancel = true;
            }
        });

        table.RowXPropertyChanged.Subscribe((args, c) =>
        {
            Assert.AreEqual(table, args.Table);

            Assert.IsNotEmpty(args.ChangedPropertyCodes);

            foreach (var propertyCode in args.ChangedPropertyCodes)
            {
                Assert.AreNotEqual(args.GetOldValue<string>(propertyCode), args.GetNewValue<string>(propertyCode));
            }
                        
            Assert.NotNull(args.Row);
                        
            xPropertyChanged = args.IsPropertyChanged("TestQ");
        });
            

        table.ImportRow(table.NewRow().Set(Fields.id, 7777).Set("UserClass", new ComplexUserClass() { ID = 7777 }).Set(Fields.Sn, 1).Set(Fields.Name, "7777"));
        table.ImportRow(table.NewRow().Set(Fields.id, 77771).Set("UserClass", new ComplexUserClass() { ID = 77771 }).Set(Fields.Sn, 2).Set(Fields.Name, "77771"));

        var dataRow = table.GetRowBy(7777);
            
        Assert.Greater(new CoreDataRowDebugView(dataRow).Items.Length, 0);

        var name = dataRow[Fields.Name];

        var changedName = name + "_";

        dataRow[Fields.Name] = changedName;

        Assert.AreEqual(changedName, dataRow.Field<string>(Fields.Name));

        dataRow.SetXProperty(Fields.groupid + "1", 77);
        
        Assert.AreEqual(77, dataRow.Field<int>(Fields.groupid + "1"));
        Assert.AreEqual(77, dataRow.FieldNotNull<long>(Fields.groupid + "1"));
        Assert.AreEqual(77d, dataRow.FieldNotNull<double>(Fields.groupid + "1"));

        dataRow[Fields.Name] = "3333";

        dataRow.Set(Fields.Sn, 3333);
        Assert.AreNotEqual(3333, dataRow.Field<int>(Fields.Sn));
            
        dataRow.Set(Fields.Sn, new int?(3333));
        Assert.AreNotEqual(3333, dataRow.Field<int>(Fields.Sn));
            
        dataRow.Set(Fields.Sn, 4444);
        Assert.AreNotEqual(4444, dataRow.Field<int>(Fields.Sn));
            
        dataRow.Set(Fields.Sn, new int?(4444));
        Assert.AreNotEqual(4444, dataRow.Field<int>(Fields.Sn));
            
        dataRow.Set(Fields.groupid, -1);
            
        Assert.Throws<DataChangeCancelException>(() =>
        {
            dataRow.Set(Fields.Sn, -3333);
        });
        Assert.AreNotEqual(-3333, dataRow.Field<int>(Fields.Sn));
            
        Assert.Throws<DataChangeCancelException>(() =>
        {
            dataRow.Set(Fields.Sn, new int?(-3333));
        });
        Assert.AreNotEqual(-3333, dataRow.Field<int?>(Fields.Sn));
            
        Assert.Throws<DataChangeCancelException>(() =>
        {
            dataRow[Fields.Sn] = -3333;
        });
        Assert.AreNotEqual(-3333, dataRow.Field<int?>(Fields.Sn));
            
        Assert.Throws<DataChangeCancelException>(() =>
        {
            dataRow[Fields.id] = -1;
        });
        Assert.AreNotEqual(-1, dataRow.Field<int?>(Fields.id));

        Assert.AreNotEqual("3333", dataRow.Field<string>(Fields.Name));

        dataRow.Delete();

        Assert.False(dataRow.IsDeletedRow);

        var dataRow1 = table.GetRowBy(77771);

        dataRow1.SetXProperty("TestP", "test1");
        Assert.IsEmpty(dataRow1.GetXProperty<string>("TestP"));

        dataRow1.SetXProperty("TestQ", "test1");

        Assert.AreEqual("test1", dataRow1.GetXProperty<string>("TestQ"));

        Assert.NotNull(dataRow1);
            
        table.AcceptChanges();

        dataRow1.Delete();

        Assert.True(dataRow1.IsDeletedRow);

        Assert.True(addingRaised);
        Assert.True(addedRaised);
        Assert.True(nameColumnChangingRaised);
        Assert.True(nameColumnChangedRaised);
        Assert.True(rowDeletedRaised);
        Assert.True(rowDeletedRaised);
        Assert.True(rowDeletingRaised);
        Assert.True(dataRowChangedRaised);
        Assert.True(dataChangingCanceled);
        Assert.True(dataRowDeleteingCanceled);
        Assert.True(xPropertyChanged);
        Assert.True(xPropertyChanging);
            
        Assert.True(dataChangingCanceledInt);

        table.Dispose();
    }

    [Test]
    public void TestTypedColumnChangingEvents()
    {
        var table = GetTestTable();

        var dataRow = table.GetRow(0);

        dataRow.SetField(Fields.Sn, -1);

        int handlerCallCount = 0;

        Action<IDataColumnChangingTypedEventArgs<int>, string> e1 = (arg, c) =>
        {
            Assert.False(arg.NewValueIsNull);
            Assert.False(arg.PrevValueIsNull);
                
            Assert.True(5 == arg.NewValue);
            Assert.True(-1 == arg.OldValue);
                
            handlerCallCount++;
        };
            
        table.SubscribeColumnChanging(Fields.Sn, e1);
            
        dataRow[Fields.Sn] = 5;
            
        Assert.AreEqual(1, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Sn, e1);
            
        Action<IDataColumnChangingTypedEventArgs<int>, string> e2 = (arg, c) =>
        {
            Assert.True(arg.NewValueIsNull);
            Assert.False(arg.PrevValueIsNull);
                
            Assert.True(0 == arg.NewValue);
            Assert.True(5 == arg.OldValue);
                
            handlerCallCount++;
        };
            
        table.SubscribeColumnChanging(Fields.Sn, e2);
            
        dataRow.SetNull(Fields.Sn);
            
        Assert.True(dataRow.IsNull(Fields.Sn));
            
        Assert.AreEqual(2, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Sn, e2);
            
        Action<IDataColumnChangingTypedEventArgs<int>, string> e3 = (arg, c) =>
        {
            Assert.False(arg.NewValueIsNull);
            Assert.True(arg.PrevValueIsNull);
                
            Assert.True(5 == arg.NewValue);
            Assert.True(0 == arg.OldValue);
                
            handlerCallCount++;
        };
            
        table.SubscribeColumnChanging(Fields.Sn, e3);
            
        dataRow.Set(Fields.Sn, new long?(5));
            
        Assert.AreEqual(3, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Sn, e3);

        Action<IDataColumnChangingTypedEventArgs<int>, string> e6 = (arg, c) =>
        {
            Assert.False(arg.NewValueIsNull);
            Assert.True(arg.PrevValueIsNull);
                
            Assert.True(5 == arg.NewValue);
            Assert.True(0 == arg.OldValue);
                
            handlerCallCount++;
        };

        dataRow.Set(Fields.Sn, (string)null);
            
        Assert.True(dataRow.IsNull(Fields.Sn));
            
        table.SubscribeColumnChanging(Fields.Sn, e6);
            
        dataRow.Set(Fields.Sn, 5);
            
        Assert.AreEqual(4, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Sn, e6);
            
        Action<IDataColumnChangingTypedEventArgs<int>, string> e7 = (arg, c) =>
        {
            Assert.False(arg.NewValueIsNull);
            Assert.True(arg.PrevValueIsNull);

            ((IDataColumnChangingEventArgs)arg).NewValue = "2";
                
            Assert.AreEqual(2, arg.NewValue);
                
            ((IDataColumnChangingEventArgs)arg).NewValue = 3.0;
                
            Assert.AreEqual(3, arg.NewValue);
                
            arg.NewValue = 6;
                
            arg.NewValueIsNull = false;
                
            handlerCallCount++;
        };

        dataRow.Set(Fields.Sn, (string)null);
            
        Assert.True(dataRow.IsNull(Fields.Sn));
            
        table.SubscribeColumnChanging(Fields.Sn, e7);
            
        dataRow[Fields.Sn] =  15;
            
        Assert.AreEqual(6, dataRow[Fields.Sn]);
            
        Assert.AreEqual(5, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Sn, e7);
            
        Action<IDataColumnChangingTypedEventArgs<int>, string> e8 = (arg, c) =>
        {
            arg.NewValueIsNull = true;
                
            handlerCallCount++;
        };

        dataRow.Set(Fields.Sn, (string)null);
            
        Assert.True(dataRow.IsNull(Fields.Sn));
            
        table.SubscribeColumnChanging(Fields.Sn, e8);
            
        dataRow[Fields.Sn] =  15;
            
        dataRow[Fields.Sn] =  "15";
            
        Assert.IsNull(dataRow.Field<int?>(Fields.Sn));
            
        Assert.True(dataRow.IsNull(Fields.Sn));
            
        Assert.AreEqual(7, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Sn, e8);
            
        Action<IDataColumnChangingTypedEventArgs<string>, string> e9 = (arg, c) =>
        {
            Assert.NotNull(arg.Row);
            Assert.NotNull(arg.Table);
            Assert.AreEqual(Fields.Code, arg.ColumnName);
                
            arg.NewValueIsNull = true;
                
            handlerCallCount++;
        };

        dataRow.Set(Fields.Code, (string)null);
            
        Assert.True(dataRow.IsNull(Fields.Code));
            
        table.SubscribeColumnChanging(Fields.Code, e9);
            
        dataRow[Fields.Code] =  15;
            
        dataRow[Fields.Code] =  "15";
            
        Assert.IsNull(dataRow.Field<int?>(Fields.Code));
        Assert.IsEmpty(dataRow.Field<string>(Fields.Code));
            
        Assert.True(dataRow.IsNull(Fields.Code));
            
        Assert.AreEqual(9, handlerCallCount);
            
        table.UnsubscribeColumnChanging(Fields.Code, e9);
    }

    [Test]
    public void TestTypedCancelEdit([Values(true, false)] bool onCopy)
    {
        (DataTable t, DataRow r) GetRow(DataTable t)
        {
            if (onCopy)
            {
                var copy1 = t.Copy();
                    
                return (copy1, copy1.GetRow(0));
            }
                
            return (t, t.GetRow(0));
        }

        var table = GetTestTable();
            
        Action<IDataColumnChangingTypedEventArgs<int>, string> e0 = (arg, c) =>
        {
        };
            
        table.SubscribeColumnChanging(Fields.Sn, e0);

        var copy = table.Copy();

        table.ClearRows();

        table.ImportRow(copy.GetRow(0));
            
        table.GetRow(0).SetField(Fields.Sn, 5);

        (DataTable t, DataRow r) tv = (null, null);
            
        tv = GetRow(table);
            
        table.AttachEventHandlersTo(tv.t);

        Action<IDataColumnChangingTypedEventArgs<int>, string> e1 = (arg, c) =>
        {
            arg.IsCancel = true;
        };
            
        tv.t.SubscribeColumnChanging(Fields.Sn, e1);
            
        tv.r.SetNull(Fields.Sn);
            
        Assert.False(tv.r.IsNull(Fields.Sn));

        tv.r.Set(Fields.Sn, 100);
            
        Assert.AreNotEqual(100,  tv.r[Fields.Sn]);
            
        tv.r.Set(Fields.Sn, new long?(100l));
            
        Assert.AreNotEqual(100l,  tv.r.Field<long?>(Fields.Sn));
            
        tv.r[Fields.Sn]  = 100l;
            
        Assert.AreNotEqual(100l,  tv.r.Field<long?>(Fields.Sn));
            
        tv.r.Set(Fields.Sn, new int?(100));
            
        Assert.AreNotEqual(100,  tv.r.Field<int?>(Fields.Sn));
            
        tv.r.Set(Fields.Sn, "100");
            
        Assert.AreNotEqual("100",  tv.r.ToString(Fields.Sn));
            
        Assert.AreEqual(5,  tv.r.Field<int?>(Fields.Sn));
            
        tv.t.UnsubscribeColumnChanging(Fields.Sn, e1);
            
        tv = GetRow(table);
            
        Action<IDataColumnChangingTypedEventArgs<int>, string> e2 = (arg, c) =>
        {
            arg.ErrorMessage = "Cannot setup";
        };
            
        tv.t.SubscribeColumnChanging(Fields.Sn, e2);

        Assert.Throws<DataChangeCancelException>(() => tv.r.SetNull(Fields.Sn));
            
        Assert.False(tv.r.IsNull(Fields.Sn));

        Assert.Throws<DataChangeCancelException>(() => tv.r.Set(Fields.Sn, 100));
            
        Assert.AreNotEqual(100, tv.r[Fields.Sn]);
            
        Assert.Throws<DataChangeCancelException>(() => tv.r.Set(Fields.Sn, new long?(100l)));
            
        Assert.AreNotEqual(100, tv.r.Field<long?>(Fields.Sn));
            
        Assert.Throws<DataChangeCancelException>(() => tv.r.Set(Fields.Sn, new int?(100)));
            
        Assert.AreNotEqual(100, tv.r.Field<int?>(Fields.Sn));
            
        Assert.Throws<DataChangeCancelException>(() => tv.r.Set(Fields.Sn, "100"));
            
        Assert.AreNotEqual("100", tv.r.ToString(Fields.Sn));
            
        Assert.Throws<DataChangeCancelException>(() => tv.r[Fields.Sn]  = 100l);
            
        Assert.AreNotEqual(100l, tv.r.Field<long?>(Fields.Sn));
            
        Assert.AreEqual(5, tv.r.Field<int?>(Fields.Sn));
            
        tv.t.UnsubscribeColumnChanging(Fields.Sn, e2);
    }

    [Test]
    public void TestSetModifiedEvents()
    {
        var table = GetTestTable();

        var nameColumnChangingRaised = false;
        var nameColumnChangedRaised = false;
        var dataRowChangedRaised = false;

        var dataChangingCanceled = false;

        table.ColumnChanging.Subscribe((args, c) =>
        {
            nameColumnChangingRaised = true;

            if (args.ColumnName == Fields.Name)
            {
                if ((string)args.NewValue == "3333")
                {
                    dataChangingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.ColumnChanged.Subscribe((args, c) =>
        {
            nameColumnChangedRaised = true;
        });

        table.DataRowChanged.Subscribe((args, c) =>
        {
            dataRowChangedRaised = true;
        });

        var dataRow = table.GetRowBy(3);

        var name = dataRow[Fields.Name];

        var changedName = name + "_";

        dataRow.SilentlySetValue(Fields.Name, changedName);

        Assert.AreEqual(changedName, dataRow.Field<string>(Fields.Name));

        dataRow.SilentlySetValue(dataRow.GetColumn(Fields.Name), "3333");

        Assert.AreEqual("3333", dataRow.Field<string>(Fields.Name));
            
        dataRow.SilentlySetValue(table.GetColumn(Fields.Name), "4444");

        Assert.AreEqual("4444", dataRow.Field<string>(Fields.Name));
            
        dataRow.SilentlySetValue(dataRow.GetColumn(Fields.Name), "5555");

        Assert.AreEqual("5555", dataRow.Field<string>(Fields.Name));
            
        dataRow.SilentlySetValue(new ColumnHandle(table.GetColumn(Fields.Sn).ColumnHandle), 66666);

        Assert.AreEqual(66666, dataRow.FieldNotNull<int>(table.GetColumn(Fields.Sn)));

        Assert.False(nameColumnChangingRaised);
        Assert.False(nameColumnChangedRaised);
        Assert.False(dataRowChangedRaised);
        Assert.False(dataChangingCanceled);

        table.Dispose();
        table.Dispose();
    }

    [Test]
    public void TestFieldRequestMissingFieldStruct()
    {
        var table = GetTestTable();

        var newGuid = Guid.NewGuid();
       
        var dataRow = table.Rows.First();

        Assert.Throws<MissingMetadataException>(() => dataRow.FieldNotNull<Guid>(newGuid.ToString()));
        Assert.Throws<MissingMetadataException>(() =>
        {
            var v = dataRow[newGuid.ToString()];
        });
        Assert.Throws<MissingMetadataException>(() => dataRow.Field<string>(newGuid.ToString()));
        
        Assert.Throws<MissingMetadataException>(() =>  dataRow[newGuid.ToString()] = newGuid);
        Assert.Throws<MissingMetadataException>(() =>  dataRow.Set(newGuid.ToString(), newGuid));
        Assert.Throws<MissingMetadataException>(() =>  dataRow.Set(newGuid.ToString(), newGuid.ToString()));
        
        dataRow.SetXProperty(newGuid.ToString(), string.Empty);

        dataRow[newGuid.ToString()] = newGuid;
        dataRow.Set(newGuid.ToString(), newGuid);
        dataRow.Set(newGuid.ToString(), newGuid.ToString());
        
        Assert.AreEqual(newGuid, dataRow.FieldNotNull<Guid>(newGuid.ToString()));
        Assert.AreEqual(newGuid.ToString(), dataRow[newGuid.ToString()]);
        Assert.AreEqual(newGuid.ToString(), dataRow.Field<string>(newGuid.ToString()));
        
        Assert.AreEqual(newGuid, dataRow.GetXProperty<Guid>(newGuid.ToString()));
    }

    [Test]
    public void TestEventsAggregate()
    {
        var table = GetTestTable();

        var lockEventState = table.LockEvents();

        var addingRaised = false;
        var addedRaised = false;
        var nameColumnChangingRaised = false;
        var nameColumnChangedRaised = false;
        var rowDeletingRaised = false;
        var rowDeletedRaised = false;
        var dataRowChangedRaised = false;

        var dataChangingCanceled = false;
        var dataRowDeleteingCanceled = false;

        var xPropertyChanged = false;
        var xPropertyChanging = false;

        table.RowAdding.Subscribe((args, c) =>
        {
            addingRaised = true;
        });

        table.RowAdded.Subscribe((args, c) =>
        {
            addedRaised = true;
        });

        table.ColumnChanging.Subscribe((args, c) =>
        {
            nameColumnChangingRaised = true;

            if (args.ColumnName == Fields.Name)
            {
                if ((string)args.NewValue == "3333")
                {
                    dataChangingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.ColumnChanged.Subscribe((args, c) =>
        {
            nameColumnChangedRaised = true;
        });

        table.RowDeleted.Subscribe((args, c) =>
        {
            rowDeletedRaised = true;
        });

        table.RowDeleting.Subscribe((args, c) =>
        {
            rowDeletingRaised = true;

            if (args.Rows.Any())
            {
                if (args.Rows.First().Field<int?>(Fields.id) == 7777)
                {
                    dataRowDeleteingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.DataRowChanged.Subscribe((args, c) =>
        {
            dataRowChangedRaised = true;
        });

        table.RowXPropertyChanged.Subscribe((args, c) => xPropertyChanged = args.IsPropertyChanged("TestQ"));

        table.RowXPropertyChanging.Subscribe((args, c) => xPropertyChanging = args.PropertyCode == "TestQ");

        table.ImportRow(table.NewRow().Set(Fields.id, 7777).Set("UserClass", new ComplexUserClass() { ID = 7777 }).Set(Fields.Sn, 1).Set(Fields.Name, "7777"));
        table.ImportRow(table.NewRow().Set(Fields.id, 77771).Set("UserClass", new ComplexUserClass() { ID = 77771 }).Set(Fields.Sn, 2).Set(Fields.Name, "77771"));

        var dataRow = table.GetRowBy(7777);

        var name = dataRow[Fields.Name];

        var changedName = name + "_";

        dataRow[Fields.Name] = changedName;
        dataRow[Fields.Sn] = 5;

        Assert.AreEqual(changedName, dataRow.Field<string>(Fields.Name));

        dataRow.SetXProperty(Fields.groupid + "1", 77);
        
        Assert.AreEqual(77, dataRow.Field<int>(Fields.groupid + "1"));

        dataRow[Fields.Name] = "3333";

        Assert.AreEqual("3333", dataRow.Field<string>(Fields.Name));

        dataRow.Delete();

        Assert.False(dataRow.IsDeletedRow);

        var dataRow1 = table.GetRowBy(77771);

        Assert.NotNull(dataRow1);

        dataRow1.SetXProperty("TestQ", "test");

        table.AcceptChanges();
            
        dataRow1.Delete();
            
        lockEventState.UnlockEvents();

        Assert.True(dataRow1.IsDeletedRow);
        Assert.True(addedRaised);
        Assert.True(addingRaised);

        Assert.False(nameColumnChangingRaised);
        Assert.True(rowDeletingRaised);
        Assert.False(dataChangingCanceled);
        Assert.True(dataRowDeleteingCanceled);

        Assert.True(nameColumnChangedRaised);
        Assert.True(rowDeletedRaised);
        Assert.True(dataRowChangedRaised);

        Assert.True(xPropertyChanged);
        Assert.False(xPropertyChanging);

        table.Dispose();
    }

    [Test]
    public void TestSetModifiedEventsAggregate()
    {
        var table = GetTestTable();

        var beginEdit = table.StartTransaction();

        var nameColumnChangingRaised = false;
        var nameColumnChangedRaised = false;
        var dataRowChangedRaised = false;

        var dataChangingCanceled = false;

        table.ColumnChanging.Subscribe((args, c) =>
        {
            nameColumnChangingRaised = true;

            if (args.ColumnName == Fields.Name)
            {
                if ((string)args.NewValue == "3333")
                {
                    dataChangingCanceled = true;

                    args.IsCancel = true;
                }
            }
        });

        table.ColumnChanged.Subscribe((args, c) =>
        {
            nameColumnChangedRaised = true;
        });

        table.DataRowChanged.Subscribe((args, c) =>
        {
            dataRowChangedRaised = true;
        });

        var dataRow = table.GetRowBy(3);

        var name = dataRow[Fields.Name];

        var changedName = name + "_";

        dataRow.SilentlySetValue(Fields.Name, changedName);

        Assert.AreEqual(changedName, dataRow.Field<string>(Fields.Name));

        dataRow.SilentlySetValue(Fields.Name, "3333");

        Assert.AreEqual("3333", dataRow.Field<string>(Fields.Name));

        beginEdit.Commit();

        Assert.False(nameColumnChangingRaised);
        Assert.False(nameColumnChangedRaised);
        Assert.False(dataRowChangedRaised);
        Assert.False(dataChangingCanceled);

        table.Dispose();
        table.Dispose();
    }

    [Test]
    public void TestCustomEvents()
    {
        var table = GetTestTable();

        int changingCount = 0;
        int changedCount = 0;

        var first = table.Rows.First();

        Action<IDataColumnChangingTypedEventArgs<string>, string> changing1 =  (arg, c) => { changingCount++; };
        Action<IDataColumnChangingTypedEventArgs<string>, string> changing2 =  (arg, c) => { changingCount++; };
        Action<IDataColumnChangingTypedEventArgs<string>, string> changing3 =  (arg, c) => { changingCount++; };

        table.SubscribeColumnChanging(Fields.Name, changing1);
        table.SubscribeColumnChanging(Fields.Name, changing2);
        table.SubscribeColumnChanging(Fields.Name, changing3);

        Action<IDataColumnChangedEventArgs, string> changed1 = (arg, c) => { changedCount++;};
        Action<IDataColumnChangedEventArgs, string> changed2 = (arg, c) => { changedCount++;};
        Action<IDataColumnChangedEventArgs, string> changed3 = (arg, c) => { changedCount++;};

        table.SubscribeColumnChanged(Fields.Name, changed1);
        table.SubscribeColumnChanged(Fields.Name, changed2);
        table.SubscribeColumnChanged(Fields.Name, changed3);

        first[Fields.Name] = "321";
        first.Set(Fields.Name, "321");

        table.UnsubscribeColumnChanged(Fields.Name, changed1);
        table.UnsubscribeColumnChanged(Fields.Name, changed2);
        table.UnsubscribeColumnChanged(Fields.Name, changed3);

        table.UnsubscribeColumnChanging(Fields.Name, changing1);
        table.UnsubscribeColumnChanging(Fields.Name, changing2);
        table.UnsubscribeColumnChanging(Fields.Name, changing3);

        first[Fields.Name] = "321";
        first.Set(Fields.Name, "321");

        Assert.AreEqual(6, changingCount);
        Assert.AreEqual(3, changedCount);

        table.Dispose();
    }

    [Test]
    public void TestAges()
    {
        var table = GetTestTable();

        var tableAge = table.DataAge;

        table.ImportRow(table.NewRow().Set(Fields.id, 5555).Set("UserClass", new ComplexUserClass() { ID = 5555 }).Set(Fields.Sn, 1).Set(Fields.Name, "7777"));
        table.ImportRow(table.NewRow().Set(Fields.id, 77771).Set("UserClass", new ComplexUserClass() { ID = 77771 }).Set(Fields.Sn, 2).Set(Fields.Name, "77771"));

        var dataRow = table.ImportRow(table.NewRow().Set(Fields.id, 5555).Set("UserClass", new ComplexUserClass() { ID = 5555 }).Set(Fields.Sn, 1).Set(Fields.Name, "555"));
        var rowAge = dataRow.GetRowAge();
        var idAge = dataRow.GetColumnAge(Fields.id);

        Assert.True(table.DataAge > tableAge);

        rowAge = dataRow.GetRowAge();
        tableAge = table.DataAge;
        idAge = dataRow.GetColumnAge(Fields.id);

        dataRow.Set(Fields.id, 5556);

        Assert.True(dataRow.GetColumnAge(Fields.id) > idAge);
        Assert.True(dataRow.GetRowAge() > rowAge);
        Assert.AreEqual(table.DataAge, tableAge + 1);

        rowAge = dataRow.GetRowAge();
        tableAge = table.DataAge;
        idAge = dataRow.GetColumnAge(Fields.id);

        dataRow.Set(Fields.Name, "333");

        Assert.AreEqual(idAge, dataRow.GetColumnAge(Fields.id));
        Assert.AreEqual(tableAge + 1, table.DataAge);
        Assert.True(dataRow.GetRowAge() > rowAge);

        rowAge = dataRow.GetRowAge();
        tableAge = table.DataAge;
        idAge = dataRow.GetColumnAge(Fields.id);

        dataRow.SetXProperty("TestP", "123");

        Assert.AreEqual(idAge, dataRow.GetColumnAge(Fields.id));
        Assert.AreEqual(tableAge + 1, table.DataAge);
        Assert.True(dataRow.GetRowAge() > rowAge);
    }

    [Test]
    public void TestEmpyRowAddCellValueSet()
    {
        var table = GetTestTable();

        int id = (int)table.Max(Fields.id);

        var initialId = id;

        Func<INewRowCellValueRequestingArgs, string, bool> handler = (args, c) =>
        {
            Assert.AreEqual(table, args.Table);
                
            args.Value = ++id;
                
            Assert.IsNotEmpty(args.ColumnName);

            return false;
        };

        table.SubscribeCellValueRequesting(Fields.id, handler);

        var beginEdit = table.StartTransaction();

        for (int i = 0; i < 20; i++)
        {
            Assert.NotNull(table.NewRow().Field<int?>(Fields.id));
        }

        table.UnsubscribeCellValueRequesting(Fields.id, handler);

        for (int i = 0; i < 20; i++)
        {
            var addEmptyRow = table.NewRow();

            Assert.Null(addEmptyRow.Field<int?>(Fields.id));

            addEmptyRow[Fields.id] = id + i + 1;
            addEmptyRow["UserClass"] =  new ComplexUserClass() { ID = addEmptyRow.Field<int>(Fields.id) };

            table.AddRow(addEmptyRow);
        }

        beginEdit.Commit();

        Assert.AreEqual(initialId + 20, id);
    }

    [Test]
    public void TestSetModified()
    {
        var table = GetTestTable();

        var dataRow = table.Rows.Last();

        dataRow.Delete();

        Assert.True(table.HasChanges());

        Assert.True(dataRow.IsDeletedRow);

        dataRow.SetModified();

        Assert.True(dataRow.IsModified);

        var beginEdit = table.StartTransaction();

        var row = table.AddRow(table.NewRow().Set(Fields.id, ushort.MaxValue).Set("UserClass", new ComplexUserClass() { ID = ushort.MaxValue }));

        beginEdit.Commit();

        Assert.True(row.IsAddedRow);

        row.SetModified();

        Assert.True(row.IsModified);

        var first = table.Rows.First();

        Assert.True(first.IsModified == false);

        first.SetModified();

        Assert.True(first.IsModified);
    }

    [Test]
    public void TestColumnClear()
    {
        var table = GetTestTable();

        var age = table.DataAge;

        table.ClearColumns();

        //sn is built in
        Assert.AreEqual(2, table.ColumnCount); //id and groupid (buildin)
        Assert.AreEqual(0, table.RowCount);
        
        table.SetPrimaryKeyColumn(null);
        
        table.ClearColumns();
        
        Assert.AreEqual(1, table.ColumnCount); //groupid (buildin)

        Assert.Greater(table.DataAge, age);
    }

    [Test]
    public void TestLookingUpHandle()
    {
        var table = GetTestTable();

        Assert.AreEqual(-1, table.GetRowHandle(int.MinValue));

        var ints = table.FindManyHandles(table.GetColumn(Fields.id).ColumnHandle, int.MaxValue).ToData();

        Assert.IsEmpty(ints);

        Assert.GreaterOrEqual(table.GetRowHandle(3), 0);
    }

    [Test]
    public void TestRemoveColumn()
    {
        var table = GetTestTable();

        var snCol = table.GetColumn(Fields.Sn);

        var list = table.Rows.Select(r => r.Field<int>(snCol)).ToData();

        Assert.IsNotEmpty(list);

        var columnCount = table.ColumnCount;

        table.RemoveColumn(snCol.ColumnHandle);

        Assert.AreEqual(columnCount - 1, table.ColumnCount);

        Assert.Null(table.TryGetColumn(Fields.Sn));

        Assert.Throws<MissingMetadataException>(
            () =>
            {
                table.GetColumn(Fields.Sn);
            });

        Assert.True(snCol.IsDetached);
    }

    [Test]
    public void TestRowEquals()
    {
        var t1 = GetTestTable();

        var r1 = t1.GetRowBy(5);
        var r11 = t1.GetRowBy(5);
           
        Assert.AreEqual(r1, r11);
           
        var t2 = GetTestTable().Copy();

        var r2 = t2.GetRowBy(5);
        var r21 = t2.GetRowBy(5);
           
        Assert.AreEqual(r2, r21);

        t2.AddColumn("TestCol", TableStorageType.Decimal);

        var _555 = t2.GetRowBy(5);

        _555["TestCol"] = 156;

        var r12 = t1.GetRowBy(5);
        var r22 = t2.GetRowBy(5);
            
        Assert.False(r12.Equals(r22));
    }

    [Test]
    public void TestRowColToString()
    {
        var t1 = GetTestTable();

        var _555 = t1.GetRowBy(5);

        Assert.NotNull(_555.ToString("Name"));

        _555.SetNull("Name");
        _555.SetNull(Fields.Sn);

        Assert.Null(_555.Field<int?>(Fields.Sn));

        Assert.NotNull(_555.Field<string>("Name"));

        Assert.NotNull(_555.ToString("Name"));
    }

    [Test]
    public void TestGetUniqueIndexColumnHandle()
    {
        var t1 = GetTestTable();

        var index = t1.TryGetUniqueIndex();

        Assert.NotNull(index);
    }

    [Test]
    public void TestAddIndexOnExpressionFails()
    {
        var t1 = GetTestTable();

        t1.AddColumn("TestExpr", TableStorageType.String, dataExpression: "name + ' ' + id");

        var dataColumn = t1.GetColumn("TestExpr");

        Assert.Throws<ReadOnlyAccessViolationException>(() => { t1.AddIndex(dataColumn, unique: true); });
        Assert.Throws<ReadOnlyAccessViolationException>(() => { t1.AddIndex(dataColumn.ColumnName, unique: true); });

        t1.Dispose();
    }

    [Test]
    public void TestColumnLogChanges()
    {
        var testTable = GetTestTable();
        
        Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
        Assert.False(testTable.GetIsLoggingChanges());

        var dateTime = new DateTime(2000,1, 1);
        
        testTable.StartTrackingChangeTimes(dateTime);

        var newCode1 = Guid.NewGuid().ToString();
        var newCode2 = Guid.NewGuid().ToString();

        using (testTable.StartLoggingChanges("Test 1"))
        {
            Assert.True(testTable.GetIsLoggingChanges());
            
            Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
            
            var firstRow = testTable.Rows.First();

            firstRow[Fields.Code] = newCode1;

            var dataLogEntries = testTable.GetLoggedChanges();
            
            Assert.AreEqual(1, dataLogEntries.Count);
            Assert.AreEqual(newCode1, dataLogEntries[0].Value);
            Assert.True(dataLogEntries[0].UtcTimestamp > dateTime);
            
            testTable.ClearLoggedChanges();
            
            testTable.RejectChanges();
            
            Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
            
            var lastRow = testTable.Rows.Last();

            firstRow[Fields.Code] = newCode1;
            lastRow[Fields.Code] = newCode2;

            var test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(2, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[0].Value);
            Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[0].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[1].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[1].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[1].ColumnOrXProperty);
            
            firstRow[Fields.Code] = null;
            lastRow[Fields.Code] = null;

            using (testTable.StartLoggingChanges("Test 1"))
            {
                firstRow[Fields.Code] = newCode1;
                lastRow[Fields.Code] = newCode2;

                test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
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

            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

            using (testTable.StartLoggingChanges("Test 2"))
            {
                firstRow[Fields.Code] = null;
                lastRow[Fields.Code] = null;
            
                firstRow[Fields.Code] = newCode1;
                lastRow[Fields.Code] = newCode2;
                
                test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 2")).ToArray();
            
                Assert.AreEqual(4, test1Changes.Length);
            
                Assert.AreEqual(newCode1, test1Changes[2].Value);
                Assert.AreEqual(Fields.Code, test1Changes[2].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[2].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[3].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[3].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[3].ColumnOrXProperty);
            }
            
            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

            var dataEditTransaction = testTable.StartTransaction();
            
            firstRow[Fields.Code] = Guid.NewGuid().ToString();
            lastRow[Fields.Code]  = Guid.NewGuid().ToString();

            dataEditTransaction.Rollback();
            
            Assert.AreEqual(newCode1, firstRow[Fields.Code]);
            Assert.AreEqual(newCode2, lastRow[Fields.Code]);
            
            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

            using (testTable.StartLoggingChanges("Test 3"))
            {
                var t1 = testTable.StartTransaction();

                var t1g = Guid.NewGuid().ToString();

                firstRow[Fields.Code] = t1g;

                var t2 = testTable.StartTransaction();

                var t2g = Guid.NewGuid().ToString();
                
                lastRow[Fields.Code] = t2g;

                t2.Rollback();
                
                Assert.AreEqual(t1g, firstRow[Fields.Code]);
                Assert.AreEqual(newCode2, lastRow[Fields.Code]);
                
                test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 3")).ToArray();
            
                Assert.AreEqual(1, test1Changes.Length);
            
                Assert.AreEqual(t1g, test1Changes[0].Value);
                Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[0].RowHandle);

                t1.Rollback();
                
                Assert.AreEqual(newCode1, firstRow[Fields.Code]);
                Assert.AreEqual(newCode2, lastRow[Fields.Code]);
                
                var t3 = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 3")).ToArray();
                
                Assert.AreEqual(0, t3.Length);
            }
            
            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);
        }
        
        testTable.RejectChanges();
        
        Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
        
        testTable.StopTrackingChangeTimes();
    }
    
    [Test]
    public void TestXPropLogChanges()
    {
        var testTable = GetTestTable();
        
        Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
        Assert.False(testTable.GetIsLoggingChanges());

        var dateTime = new DateTime(2000,1, 1);
        
        testTable.StartTrackingChangeTimes(dateTime);

        var newCode1 = Guid.NewGuid().ToString();
        var newCode2 = Guid.NewGuid().ToString();

        using (testTable.StartLoggingChanges("Test 1"))
        {
            Assert.True(testTable.GetIsLoggingChanges());
            
            Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
            
            var firstRow = testTable.Rows.First();

            firstRow.SetXProperty(Fields.Code, newCode1);

            var dataLogEntries = testTable.GetLoggedChanges();
            
            Assert.AreEqual(1, dataLogEntries.Count);
            Assert.AreEqual(newCode1, dataLogEntries[0].Value);
            Assert.True(dataLogEntries[0].UtcTimestamp > dateTime);
            
            testTable.ClearLoggedChanges();
            
            testTable.RejectChanges();
            
            Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
            
            var lastRow = testTable.Rows.Last();

            firstRow.SetXProperty(Fields.Code, newCode1);
            lastRow.SetXProperty(Fields.Code,  newCode2);

            var test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(2, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[0].Value);
            Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[0].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[1].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[1].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[1].ColumnOrXProperty);
            
            firstRow.SetXProperty<object>(Fields.Code, null);
            lastRow.SetXProperty<object>(Fields.Code,  null);

            using (testTable.StartLoggingChanges("Test 1"))
            {
                firstRow.SetXProperty(Fields.Code, newCode1);
                lastRow.SetXProperty(Fields.Code,  newCode2);

                test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
                Assert.AreEqual(6, test1Changes.Length);
            
                Assert.AreEqual(newCode1, test1Changes[4].Value);
                Assert.AreEqual(Fields.Code, test1Changes[4].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[4].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[5].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[5].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[5].ColumnOrXProperty);
            }
            
            firstRow.SetXProperty<object>(Fields.Code, null);
            lastRow.SetXProperty<object>(Fields.Code,  null);
            
            firstRow.SetXProperty(Fields.Code, newCode1);
            lastRow.SetXProperty(Fields.Code,  newCode2);

            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

            using (testTable.StartLoggingChanges("Test 2"))
            {
                firstRow.SetXProperty<object>(Fields.Code, null);
                lastRow.SetXProperty<object>(Fields.Code,  null);
            
                firstRow.SetXProperty(Fields.Code, newCode1);
                lastRow.SetXProperty(Fields.Code,  newCode2);
                
                test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 2")).ToArray();
            
                Assert.AreEqual(4, test1Changes.Length);
            
                Assert.AreEqual(newCode1, test1Changes[2].Value);
                Assert.AreEqual(Fields.Code, test1Changes[2].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[2].RowHandle);
                Assert.AreEqual(newCode2, test1Changes[3].Value);
                Assert.AreEqual(lastRow.RowHandle, test1Changes[3].RowHandle);
                Assert.AreEqual(Fields.Code, test1Changes[3].ColumnOrXProperty);
            }
            
            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

            var dataEditTransaction = testTable.StartTransaction();
            
            firstRow.SetXProperty(Fields.Code, Guid.NewGuid().ToString());
            lastRow.SetXProperty(Fields.Code, Guid.NewGuid().ToString());

            dataEditTransaction.Rollback();
            
            Assert.AreEqual(newCode1, firstRow.GetXProperty<string>(Fields.Code));
            Assert.AreEqual(newCode2, lastRow.GetXProperty<string>(Fields.Code));
            
            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);

            using (testTable.StartLoggingChanges("Test 3"))
            {
                var t1 = testTable.StartTransaction();

                var t1g = Guid.NewGuid().ToString();

                firstRow.SetXProperty(Fields.Code, t1g);

                var t2 = testTable.StartTransaction();

                var t2g = Guid.NewGuid().ToString();
                
                lastRow.SetXProperty(Fields.Code, t2g);

                t2.Rollback();
                
                Assert.AreEqual(t1g, firstRow.GetXProperty<string>(Fields.Code));
                Assert.AreEqual(newCode2, lastRow.GetXProperty<string>(Fields.Code));
                
                test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 3")).ToArray();
            
                Assert.AreEqual(1, test1Changes.Length);
            
                Assert.AreEqual(t1g, test1Changes[0].Value);
                Assert.AreEqual(Fields.Code, test1Changes[0].ColumnOrXProperty);
                Assert.AreEqual(firstRow.RowHandle, test1Changes[0].RowHandle);

                t1.Rollback();
                
                Assert.AreEqual(newCode1, firstRow.GetXProperty<string>(Fields.Code));
                Assert.AreEqual(newCode2, lastRow.GetXProperty<string>(Fields.Code));
                
                var t3 = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 3")).ToArray();
                
                Assert.AreEqual(0, t3.Length);
            }
            
            test1Changes = testTable.GetLoggedChanges().Where(c => c.Context.Equals("Test 1")).ToArray();
            
            Assert.AreEqual(10, test1Changes.Length);
            
            Assert.AreEqual(newCode1, test1Changes[8].Value);
            Assert.AreEqual(Fields.Code, test1Changes[8].ColumnOrXProperty);
            Assert.AreEqual(firstRow.RowHandle, test1Changes[8].RowHandle);
            Assert.AreEqual(newCode2, test1Changes[9].Value);
            Assert.AreEqual(lastRow.RowHandle, test1Changes[9].RowHandle);
            Assert.AreEqual(Fields.Code, test1Changes[9].ColumnOrXProperty);
        }
        
        testTable.RejectChanges();
        
        Assert.AreEqual(0, testTable.GetLoggedChanges().Count);
        
        testTable.StopTrackingChangeTimes();
    }

    [Test]
    public void TestColumnToStringHandles()
    {
        var sourceTable = GetTestTable();

        var targetTable = new DataTable();

        var ls = targetTable.BeginLoad();

        foreach (var column in sourceTable.GetColumns().Reverse())
        {
            targetTable.AddColumn(column);
        }
        
        ls.EndLoad();

        var newColumns = targetTable.GetColumns().ToArray();
        var originalColumns = sourceTable.GetColumns().ToArray();

        var colNames = newColumns.Select(col => col.ColumnName).ToArray();
        var colHandles = newColumns.Select(col => col.Ordinal).ToArray();
        
        var colNamesOriginal = originalColumns.Select(col => col.ColumnName).ToArray();

        Assert.False(colNames.SequenceEqual(colNamesOriginal));

        targetTable.MergeMetaOnly(sourceTable);

        var colNamesUpdated = targetTable.GetColumns().Select(col => col.ColumnName).ToArray();

        Assert.AreEqual(colNamesOriginal, colNamesUpdated);

        var colHandlesUpdated = targetTable.GetColumns().Select(col => col.Ordinal).ToArray();

        Assert.AreEqual(colHandles.OrderBy(col => col), colHandlesUpdated);
    }

    [Test]
    public void TestRowDeleteAndAccept()
    {
        var t1 = GetTestTable();

        var row = t1.GetRowBy(3);

        row.SetXProperty("Test", "1");

        var rowRowHandle = row.RowHandle;

        row.Delete();

        Assert.AreEqual(RowState.Deleted, row.RowRecordState);

        t1.AcceptChanges();

        Assert.AreEqual(RowState.Detached, row.RowRecordState);

        var newRow = t1.AddRow(t1.NewRow().Set(Fields.id, ushort.MaxValue - 1).Set("UserClass", new ComplexUserClass() { ID = ushort.MaxValue - 1 }));

        Assert.AreEqual(RowState.Added, newRow.RowRecordState);

        Assert.AreEqual(rowRowHandle, newRow.RowHandle);

        Assert.True(row.Equals(newRow));

        Assert.IsEmpty(row.GetXProperty<string>("Test"));
    }

    [Test]
    public void TestLoadDetachedRows()
    {
        var t1 = GetTestTable();
            
        var deleted = t1.Rows.ToData();

        t1.DeleteRows(m_sourceDataTable.RowsHandles.ToData());

        t1.AcceptChanges();

        var clone = m_sourceDataTable.Clone();

        var cloneRowCount = clone.RowCount;

        clone.LoadRows(deleted);

        Assert.AreEqual(cloneRowCount, clone.RowCount);
    }

    [Test]
    public void TestAddRowCancel()
    {
        var t1 = GetTestTable();

        t1.RowAdding.Subscribe((args, c) => args.IsCancel = true);

        var beginEdit = t1.StartTransaction();

        var newRow = t1.AddRow(t1.NewRow());

        Assert.Null(newRow);

        beginEdit.Commit();
    }


    [Test]
    public void TestRowAddedEvent()
    {
        var t1 = GetTestTable();

        var addedRowId = new HashSet<int>();

        t1.RowAdded.Subscribe((args, c) =>
        {
            Assert.AreEqual(false, args.IsMultipleRow);
            Assert.AreEqual(t1, args.Table);
                
            addedRowId.Add(args.Row.Field<int>(Fields.id));
        });

        var newRow = t1.AddRow(t1.NewRow().Set(Fields.id, ushort.MaxValue).Set("UserClass", new ComplexUserClass() { ID = ushort.MaxValue }));

        Assert.True(addedRowId.Contains(newRow.Field<int>(Fields.id)));
    }


    [Test]
    public void TestAddRowAutoIncrementNull()
    {
        var t1 = GetTestTable();

        var row = t1.ImportRow(t1.NewRow().Set(Fields.id, ushort.MaxValue ).Set("UserClass", new ComplexUserClass() { ID = ushort.MaxValue }));

        Assert.NotNull(row);

        Assert.False(row.IsNull(Fields.Sn));

        Assert.Greater(row.Field<int>(Fields.Sn), 0);
    }


    [Test]
    public void TestAddRowWithoutBeginEdit()
    {
        var t1 = GetTestTable();

        Assert.Throws<ConstraintException>(() => t1.AddRow(t1.NewRow()));
    }

    [Test]
    public void TestImportModifiedRows()
    {
        var t1 = GetTestTable();
        var clone = t1.Clone();

        foreach (var row in t1.Rows)
        {
            row.SetModified();
        }

        clone.LoadRows(t1.Rows);

        Assert.True(clone.Rows.All(r => r.IsModified));
        Assert.True(clone.Rows.All(r => r.RowRecordState == RowState.Modified));
    }

    [Test]
    public void TestImportDeletedRows()
    {
        var t1 = GetTestTable();
        var clone = t1.Clone();

        foreach (var row in t1.AllRows)
        {
            row.Delete();
        }

        clone.LoadRows(t1.Rows);

        Assert.True(clone.Rows.All(r => r.IsDeletedRow));
        Assert.True(clone.Rows.All(r => r.RowRecordState == RowState.Deleted));
    }

    [Test]
    public void TestGetRowByHandle()
    {
        var t1 = GetTestTable();

        var row = t1.Rows.First();

        var rowRowHandle = row.RowHandle;

        row.Delete();

        var dataRow = t1.GetRowByHandle(rowRowHandle);

        Assert.True(row.Equals(dataRow));
    }

    [Test]
    public void TestRemoveAbsentColumn()
    {
        var t1 = GetTestTable();
        Assert.False(t1.RemoveColumn(string.Empty));
    }

    [Test]
    public void TestGetDefaultColumnNullValue()
    {
        var t1 = GetTestTable();

        Assert.AreEqual(string.Empty, t1.GetDefaultNullValue<string>(Fields.Name));
        Assert.AreEqual(null, t1.GetDefaultNullValue<int?>(Fields.id));

        var nameColumn = t1.GetColumn(Fields.Name);
        var idColumn = t1.GetColumn(Fields.id);

        Assert.AreEqual(string.Empty, t1.GetDefaultNullValue<string>(nameColumn));
        Assert.AreEqual(null, t1.GetDefaultNullValue<int?>(idColumn));
    }

    [Test]
    public void EditRowDeleteRejectTest()
    {
        var t1 = GetTestTable();

        for (int i = 0; i < 5; i++)
        {
            var someRow = t1.Rows.First();

            var dataEdit = someRow.StartTransaction();

            someRow.Delete();

            Assert.AreEqual(RowState.Deleted, someRow.RowRecordState);

            dataEdit.Rollback();

            Assert.AreEqual(RowState.Unchanged, someRow.RowRecordState);
        }
    }
        
    [Test]
    public void EditRowModifyRejectTest()
    {
        var t1 = GetTestTable();

        for (int i = 0; i < 5; i++)
        {
            var someRow = t1.Rows.First();

            var dataEdit = someRow.StartTransaction();

            var newGuid = Guid.NewGuid();

            var originalValue = someRow.Field<Guid>(Fields.Directories.Guid);

            someRow.Set(Fields.Directories.Guid, newGuid);

            Assert.AreEqual(RowState.Modified, someRow.RowRecordState);
                
            someRow.SetXProperty("Test", "test");
                
            Assert.AreEqual("test", someRow.GetXProperty<string>("Test"));

            dataEdit.Rollback();

            Assert.AreEqual(RowState.Unchanged, someRow.RowRecordState);
            Assert.AreEqual(originalValue, someRow.Field<Guid>(Fields.Directories.Guid));
            Assert.IsEmpty(someRow.GetXProperty<string>("Test"));
        }
    }

    [Test]
    public void AddDuplicateColumnRaisesException()
    {
        var dataTable = new DataTable();

        dataTable.AddColumn("Name");

        Assert.Throws<ArgumentException>(() => dataTable.AddColumn("name"));
    }


    [Test]
    public void EditMultiEditTest()
    {
        var t1 = GetTestTable();

        for (int i = 0; i < 5; i++)
        {
            var someRow = t1.Rows.First();

            var dataEdit = someRow.StartTransaction();

            var newGuid = Guid.NewGuid();

            var originalValue = someRow.Field<Guid>(Fields.Directories.Guid);

            someRow.Set(Fields.Directories.Guid, newGuid);
                
            someRow.SetXProperty("Test1", "test1");
                
            Assert.AreEqual("test1", someRow.GetXProperty<string>("Test1"));

            var edit2 = someRow.StartTransaction();
                
            someRow.SetXProperty("Test1", "test2");
                
            Assert.AreEqual("test2", someRow.GetXProperty<string>("Test1"));

            someRow.Set(Fields.Directories.Guid, Guid.NewGuid());

            edit2.Rollback();

            Assert.AreEqual(RowState.Modified, someRow.RowRecordState);
            Assert.AreEqual(newGuid, someRow.Field<Guid>(Fields.Directories.Guid));
            Assert.AreEqual("test1", someRow.GetXProperty<string>("Test1"));

            dataEdit.Rollback();

            Assert.AreEqual(RowState.Unchanged, someRow.RowRecordState);
            Assert.AreEqual(originalValue, someRow.Field<Guid>(Fields.Directories.Guid));
            Assert.IsEmpty(someRow.GetXProperty<string>("Test1"));
        }
    }
        
    [Test]
    public void EditMultiRowEditTest()
    {
        var t1 = GetTestTable();

        for (int i = 0; i < 5; i++)
        {
            var dataEdit = t1.StartTransaction();

            var firstRow = t1.Rows.First();
            var lastRow = t1.Rows.Last();

            var f1 = firstRow.Field<Guid>(Fields.Directories.Guid);
            var l1 = lastRow.Field<Guid>(Fields.Directories.Guid);

            try
            {
                var frEdit = firstRow.StartTransaction();

                firstRow.Set(Fields.Directories.Guid, Guid.NewGuid());
                    
                firstRow.SetColumnError(Fields.Directories.Guid, "COL_E1");
                firstRow.SetColumnWarning(Fields.Directories.Guid, "COL_W1");
                firstRow.SetColumnInfo(Fields.Directories.Guid, "COL_I1");
                    
                firstRow.SetRowError("Error1");
                firstRow.SetRowInfo("Info1");
                firstRow.SetRowWarning("Warning1");
                firstRow.SetRowFault("Fault1");

                frEdit.Commit();

                var lrEdit = lastRow.StartTransaction();

                lastRow.Set(Fields.Directories.Guid, Guid.NewGuid());
                    
                lastRow.SetColumnError(Fields.Directories.Guid, "COL_E2");
                lastRow.SetColumnWarning(Fields.Directories.Guid, "COL_W2");
                lastRow.SetColumnInfo(Fields.Directories.Guid, "COL_I2");
                    
                lastRow.SetRowError("Error2");
                lastRow.SetRowInfo("Info2");
                lastRow.SetRowWarning("Warning2");
                lastRow.SetRowFault("Fault2");

                lrEdit.Commit();

                throw null;
            }
            catch
            {
                dataEdit.Rollback();
            }

            Assert.AreEqual(RowState.Unchanged, firstRow.RowRecordState);
            Assert.AreEqual(RowState.Unchanged, lastRow.RowRecordState);

            Assert.AreEqual(f1, firstRow.Field<Guid>(Fields.Directories.Guid));
            Assert.AreEqual(l1, lastRow.Field<Guid>(Fields.Directories.Guid));
                
                
            Assert.IsEmpty(firstRow.GetCellError(Fields.Directories.Guid));
            Assert.IsEmpty(firstRow.GetCellWarning(Fields.Directories.Guid));
            Assert.IsEmpty(firstRow.GetCellInfo(Fields.Directories.Guid));
                
            Assert.IsEmpty(lastRow.GetCellError(Fields.Directories.Guid));
            Assert.IsEmpty(lastRow.GetCellWarning(Fields.Directories.Guid));
            Assert.IsEmpty(lastRow.GetCellInfo(Fields.Directories.Guid));
                
            Assert.IsEmpty(firstRow.GetRowError());
            Assert.IsEmpty(firstRow.GetRowWarning());
            Assert.IsEmpty(firstRow.GetRowInfo());
            Assert.IsEmpty(firstRow.GetRowFault());
                
            Assert.IsEmpty(lastRow.GetRowError());
            Assert.IsEmpty(lastRow.GetRowWarning());
            Assert.IsEmpty(lastRow.GetRowInfo());
            Assert.IsEmpty(lastRow.GetRowFault());
        }
    }
    
    [Test]
    public void TestArraysAndRanges()
    {
        var t1 = GetTestTable();

        var originalRow = m_sourceDataTable.GetRowBy(3);
        
        var testRow = t1.GetRowBy(3);

        Assert.True(Tool.ArraysDeepEqual(originalRow.Field<double[]>(Fields.Directories.Data), testRow.Field<double[]>(Fields.Directories.Data), (x, y) => x == y));
        Assert.True(Tool.ArraysDeepEqual(originalRow.Field<Type[]>(Fields.Directories.DataTypes), testRow.Field<Type[]>(Fields.Directories.DataTypes), (x, y) => x == y));
        Assert.AreEqual(originalRow.Field<Range<TimeSpan>>(Fields.Directories.TimeRange), testRow.Field<Range<TimeSpan>>(Fields.Directories.TimeRange));

        var r = new Range<TestClone>(new (1), new (2));

        var rc = (Range<TestClone>)r.Clone();
        
        Assert.True(r.CompareTo(rc) == 0);

        var ri = new Range<int>(1, 2);

        var ric = (Range<int>)ri.Clone();
        
        Assert.True(ri.CompareTo(ric) == 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Range<int>(0, -1));
    }

    [Test]
    public void TesRangeIndex()
    {
        var t1 = GetTestTable();

        var tableRowCount = t1.RowCount;

        for (int i = 1; i < tableRowCount + 1; i++)
        {
            var value = new Range<TimeSpan>(TimeSpan.FromHours(1 + i), TimeSpan.FromHours(1 + i) + TimeSpan.FromSeconds(1 + i));
            
            var dataRows = t1.GetRows(Fields.Directories.TimeRange,  value).ToData();

            Assert.AreEqual(1, dataRows.Count);

            var dataRow = t1.Rows.First(r => r.Field<Range<TimeSpan>>(Fields.Directories.TimeRange).ContainsRange(value));
            
            Assert.AreEqual(dataRow.RowHandle, dataRows[0].RowHandle);
        }
        
        var allRows = t1.GetRows(Fields.Directories.TimeRange,  new Range<TimeSpan>(TimeSpan.Zero, TimeSpan.FromHours(2 + tableRowCount))).ToData();

        Assert.AreEqual(tableRowCount, allRows.Count);

        var hd = allRows.Select(r => r.RowHandle).ToSet();

        Assert.True(Enumerable.Range(0, tableRowCount).All(i => hd.Contains(i)));
    }
    
    [Test]
    public void TesRangeIndexChange()
    {
        var t1 = GetTestTable().Copy();

        var dataRow = t1.Rows.First();

        var dataRowContainer = dataRow.ToContainer();

        var value = dataRowContainer.Field<Range<TimeSpan>>(Fields.Directories.TimeRange);

        var dataRows = t1.GetRows(Fields.Directories.TimeRange,  value).ToData();

        Assert.AreEqual(1, dataRows.Count);

        var search = t1.Rows.First(r => r.Field<Range<TimeSpan>>(Fields.Directories.TimeRange).ContainsRange(value));

        Assert.AreEqual(search.RowHandle, dataRows[0].RowHandle);
        
        var transaction = t1.StartTransaction();
        
        t1.DeleteRow(dataRow.RowHandle);
        
        dataRows = t1.GetRows(Fields.Directories.TimeRange,  value).ToData();

        Assert.AreEqual(0, dataRows.Count);

        search = t1.Rows.FirstOrDefault(r => r.Field<Range<TimeSpan>>(Fields.Directories.TimeRange).ContainsRange(value));

        Assert.Null(search);

        transaction.Rollback();
        
        dataRows = t1.GetRows(Fields.Directories.TimeRange,  value).ToData();

        Assert.AreEqual(1, dataRows.Count);

        search = t1.Rows.First(r => r.Field<Range<TimeSpan>>(Fields.Directories.TimeRange).ContainsRange(value));

        Assert.AreEqual(search.RowHandle, dataRows[0].RowHandle);
        
        dataRow = t1.Rows.First();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            dataRow[Fields.Directories.TimeRange] = new Range<TimeSpan>() {Minimum = TimeSpan.Zero, Maximum = TimeSpan.FromDays(-1) };
        });
        
        value = new Range<TimeSpan>(TimeSpan.Zero, TimeSpan.FromSeconds(1));

        dataRows = t1.GetRows(Fields.Directories.TimeRange,  value).ToData();

        Assert.AreEqual(0, dataRows.Count);
        
        dataRow.Set(Fields.Directories.TimeRange, value);
        
        dataRows = t1.GetRows(Fields.Directories.TimeRange,  value).ToData();

        Assert.AreEqual(1, dataRows.Count);

        search = t1.Rows.First(r => r.Field<Range<TimeSpan>>(Fields.Directories.TimeRange).ContainsRange(value));

        Assert.AreEqual(search.RowHandle, dataRows[0].RowHandle);
    }

    private class TestClone : ICloneable, IComparable, IComparable<TestClone>
    {
        public int Value;

        public TestClone(int val) => Value = val;

        public object Clone() => new TestClone(Value);

        public int CompareTo(object obj) => CompareTo((TestClone)obj);

        public int CompareTo(TestClone other) => Value.CompareTo(other.Value);
    }

    [Test]
    public void TestUserClass()
    {
        var t1 = GetTestTable();

        var row = t1.GetRowBy(3);

        var userClass = row.Field<ComplexUserClass>("UserClass");

        userClass.ID = int.MinValue;
            
        Assert.AreNotEqual(userClass, row.Field<ComplexUserClass>("UserClass"));

        row.SetField("UserClass", userClass);
            
        Assert.AreEqual(userClass, row.Field<ComplexUserClass>("UserClass"));
     
        row.SetField("UserClass", new XElement("ID", ushort.MaxValue));
        
        var userClass5 = row.Field<ComplexUserClass>("UserClass");

        Assert.AreEqual(userClass5, new ComplexUserClass() {ID = ushort.MaxValue});
        
        row.SetField("UserClass", new XElement("ID", short.MinValue).ToString());
        
        Assert.AreEqual(row.Field<ComplexUserClass>("UserClass"), new ComplexUserClass() {ID = short.MinValue});
    }

    [Test]
    public void TestDebugView()
    {
        var t1 = GetTestTable();
            
        var row = t1.Copy().GetRowBy(3);

        row.SetRowError("Error");
        row.SetRowWarning("Warning");
        row.SetRowInfo("Info");
        row.SetRowFault("Fault");
            
        row.SetColumnError(Fields.Code, "Error");
        row.SetColumnWarning(Fields.Code, "Warning");
        row.SetColumnInfo(Fields.Code, "Info");
            
        row.SetXProperty("TestProperty", "TestValue");

        Assert.IsNotEmpty(new CoreDataRowDebugView(row).Items);
        Assert.IsNotEmpty(new CoreDataRowDebugView(row.ToContainer()).Items);

        var itemArray = DataRow.GetItemArray(row);
            
        Assert.AreEqual(row.GetColumnCount(), itemArray.Length);

        for (int i = 0; i < itemArray.Length; i++)
        {
            Assert.AreEqual(row[new ColumnHandle(i)], itemArray[i], i.ToString());
        }
           
        var pairArray = DataRow.GetItemKeyStringValuePairArray(row);

        Assert.AreEqual(row.GetColumnCount(), pairArray.Length);
    }

    [Test]
    public void TestWeakEventClone()
    {
        var weakEvent = new DataEvent<int>(m_dispCollection);

        var ints = new Data<int>();

        weakEvent.Subscribe((arg, c) => ints.Add(arg));
            
        weakEvent.Raise(1);
        weakEvent.Raise(2);
        weakEvent.Raise(3);
            
        Assert.AreEqual(3, ints.Count);
            
        var clone = (DataEvent<int>)weakEvent.Clone(m_dispCollection);
            
        weakEvent.Dispose();
            
        weakEvent.Raise(4);
        weakEvent.Raise(5);
        weakEvent.Raise(6);
            
        Assert.AreEqual(3, ints.Count);

        clone.Raise(4);
        clone.Raise(5);
        clone.Raise(6);
            
        Assert.AreEqual(6, ints.Count);
            
        Assert.Throws<ArgumentNullException>(() => new DataEvent<object>(m_dispCollection).Raise(null));
    }

    [Test]
    public void TestTransactionEvents()
    {
        var dataTable = new DataTable();
        
        bool transactionCommit = false;
        bool transactionRollBack = false;
        
        dataTable.TransactionCommit.Subscribe((e, c) => transactionCommit = true);
        dataTable.TransactionRollback.Subscribe((e, c) => transactionRollBack = true );

        var transaction = dataTable.StartTransaction();
        
        transaction.Commit();
        
        Assert.True(transactionCommit);
        Assert.False(transactionRollBack);

        transactionCommit = false;
        
        transaction = dataTable.StartTransaction();
        
        transaction.Rollback();
        
        Assert.False(transactionCommit);
        Assert.True(transactionRollBack);
    }
    
    [Test]
    public void TestMetaChangeEvents()
    {
        var dataTable = new DataTable();
        
        bool xPropChanged = false;
        bool xPropChanging = false;
        
        dataTable.XPropertyChanged.Subscribe((e, c) => xPropChanged = true);
        dataTable.XPropertyChanging.Subscribe((e, c) =>
        {
            if (e.XPropertyName == "Cancel" && e.GetNewValue<bool>() && e.GetOldValue<bool>() == false)
            {
                e.Cancel = true;
            }
            else
            {
                xPropChanging = true;
            }
        });
        dataTable.SetXProperty("SomeProp", 123);
        
        Assert.True(xPropChanged);
        Assert.True(xPropChanging);

        xPropChanged = false;
        xPropChanging = false;
        
        dataTable.SetXProperty("Cancel", true);
        
        Assert.False(xPropChanged);
        Assert.False(xPropChanging);
    }

    [Test]
    public void TestRowMetaChangeEvents()
    {
        var dataTable = new DataTable();

        dataTable.AddColumn("test");
        
        var rowAccessor = dataTable.NewRow();

        rowAccessor.Set("test", "test");
        
        dataTable.Rows.Add(rowAccessor);

        bool fault = false;
        bool error = false;
        bool warn = false;
        bool info = false;
        
        dataTable.RowMetadataChangedEvent.Subscribe((e, c) =>
        {
            if (e.Key == "Error")
            {
                error = true;
            }

            if (e.Key == "Fault")
            {
                fault = true;
            }

            if (e.Key == "Warning")
            {
                warn = true;
            }

            if (e.Key == "Info")
            {
                info = true;
            }
        } );

        var row = dataTable.GetRowByHandle(0);

        row.SetRowError("1");
        
        Assert.True(error);
        Assert.False(fault);
        Assert.False(warn);
        Assert.False(info);
        
        error = false;
        
        row.SetRowFault("1");
        
        Assert.True(fault);
        Assert.False(error);
        Assert.False(warn);
        Assert.False(info);

        fault = false;
        
        row.SetRowWarning("1");
        
        Assert.True(warn);
        Assert.False(error);
        Assert.False(fault);
        Assert.False(info);

        warn = false;
        
        row.SetRowInfo("1");
        
        Assert.True(info);
        Assert.False(error);
        Assert.False(fault);
        Assert.False(warn);
    }

    [Test]
    public void TestEditRow() 
    {
        var testTable = GetTestTable();

        var dataRow = testTable.GetRow(0);

        testTable.SubscribeColumnChanging<string>(Fields.Code, (arg, c) =>
        {
            if (arg.NewValue == "Test")
            {
                arg.ErrorMessage = "Cannot use test in code";
            }
        });
            
        testTable.SubscribeColumnChanging<int>(Fields.groupid, (arg, c) =>
        {
            if (arg.NewValue < 0)
            {
                throw new InvalidOperationException("Cannot use negative groupid.");
            }
        });

        var newGuid = Guid.NewGuid().ToString();

        Assert.Throws<DataChangeCancelException>(() =>
            dataRow.EditRow<DataRow>(row =>
            {
                row[Fields.Name] = newGuid;

                Assert.AreEqual(newGuid, row[Fields.Name]);
                    
                row[Fields.Code] = "Test";
            }));
            
        Assert.AreNotEqual(newGuid, dataRow[Fields.Name]);
            
        newGuid = Guid.NewGuid().ToString();
            
        Assert.Throws<InvalidOperationException>(() =>
            dataRow.EditRow<DataRow>(row =>
            {
                row[Fields.Name] = newGuid;

                Assert.AreEqual(newGuid, row[Fields.Name]);
                    
                row[Fields.groupid] = -1;
            }));
            
        Assert.AreNotEqual(newGuid, dataRow[Fields.Name]);
            
        newGuid = Guid.NewGuid().ToString();
            
        Assert.DoesNotThrow(() =>
            dataRow.EditRow<DataRow>(row =>
            {
                row[Fields.Name] = newGuid;

                Assert.AreEqual(newGuid, row[Fields.Name]);
            }));
            
        Assert.AreEqual(newGuid, dataRow[Fields.Name]);
    }

 

    [Test]
    public void TestCustomTypeIndexing()
    {
        var testTable = GetTestTable();

        var dataRow = testTable.Rows.Where("UserClass").Equals(new ComplexUserClass() { ID = 5 }).First();
            
        var expected = testTable.Rows.Where(Fields.id).Equals(5).First();
            
        Assert.True(expected.Equals(dataRow));
          
        dataRow = testTable.GetRow("UserClass", new ComplexUserClass() { ID = 5 });
            
        Assert.True(expected.Equals(dataRow));

        var id = expected.Field<int>(Fields.id);

        var value = new ComplexPoint2D(id % 2, id % 3);
        
        var point2D = testTable
            .GetRows("Point2D", value)
            .OrderBy(Fields.id)
            .ToData();

        var point2Ds = testTable.Rows.SelectFieldValue<ComplexPoint2D>("Point2D").ToArray();

        var expectedRows = testTable.Rows
            .Where(r => r.Field<ComplexPoint2D>("Point2D").CompareTo(value) == 0)
            .OrderBy(Fields.id)
            .ToData();

        Assert.AreEqual(expectedRows.Count, point2D.Count);

        for (var index = 0; index < expectedRows.Count; index++)
        {
            Assert.True(expectedRows[index].Equals(point2D[index]));
        }
    }
    
    [Test]
    public void TestStringCaseIndex()
    {
        var testTable = GetTestTable();

        var dataRow = testTable.Rows.First();
        
        var code = dataRow.Field<string>(Fields.Code);

        var caseSensitive = testTable.GetColumn(Fields.Code).GetXProperty<bool?>(DataTable.StringIndexCaseSensitiveXProp);
        
        Assert.AreEqual(false, caseSensitive);

        var dataRows = testTable.Rows.WhereFieldValue(Fields.Code, code).OrderBy(Fields.id).ToData();

        var normalCodeRows = testTable.GetRows(Fields.Code, code).OrderBy(Fields.id).ToData();
        
        Assert.True(dataRows == normalCodeRows);

        var upperCodeRows = testTable.GetRows(Fields.Code, code.ToUpper()).OrderBy(Fields.id).ToData();
        
        Assert.True(dataRows == upperCodeRows);
        
        var lowerCodeRows = testTable.GetRows(Fields.Code, code.ToLower()).OrderBy(Fields.id).ToData();
        
        Assert.True(dataRows == lowerCodeRows);
    }

    [Test]
    public void TestRowXPropInfo()
    {
        var t = new DataTable();
        t.AddColumn("id", TableStorageType.Int32);
        
        t.Rows.Add(t.NewRow(_.MapObj(("id", 1))));
        t.Rows.Add(t.NewRow(_.MapObj(("id", 2))));

        var fr = t.Rows.First();
        var lr = t.Rows.Last();
        
        fr.SetXProperty("T", "T");
        lr.SetXProperty("V", 1);

        var tr = t.StartTransaction();

        fr.SetXPropertyAnnotation("T", "Testing", "Ask for help");
        
        Assert.AreEqual("Ask for help", fr.GetXPropertyAnnotation<string>("T", "Testing"));
        Assert.IsEmpty(lr.GetXPropertyAnnotation<string>("V", "Testing"));
        
        fr.SetRowInfo("Test");

        tr.Rollback();
        
        Assert.AreEqual(string.Empty, fr.GetRowInfo());
        
        Assert.IsEmpty(fr.GetXPropertyAnnotation<string>("T", "Testing"));
        
        tr = t.StartTransaction();
        
        lr.SetXPropertyAnnotation("V", "Testing", "Ask for help");
        
        Assert.AreEqual("Ask for help", lr.GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.AreEqual("V", lr.XPropertyAnnotations.First());
        Assert.IsEmpty(fr.GetXPropertyAnnotation<string>("T", "Testing"));

        Assert.AreEqual("Ask for help", lr.ToContainer().GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.IsEmpty(fr.ToContainer().GetXPropertyAnnotation<string>("T", "Testing"));
        
        tr.Commit();
        
        Assert.AreEqual("Ask for help", lr.GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.AreEqual("V", lr.XPropertyAnnotations.First());
        Assert.IsEmpty(fr.GetXPropertyAnnotation<string>("T", "Testing"));

        Assert.AreEqual("Ask for help", lr.ToContainer().GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.IsEmpty(fr.ToContainer().GetXPropertyAnnotation<string>("T", "Testing"));

        var container = lr.ToContainer();
        
        Assert.AreEqual("V", container.XPropertyAnnotations.First());

        container.SetXPropertyAnnotation("Test", "Another XProp", "value");
        
        Assert.AreEqual("Ask for help", container.GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.AreEqual("value", container.GetXPropertyAnnotation<string>("Test", "Another XProp"));
        
        Assert.AreEqual(2, container.XPropertyAnnotations.Count());
        
        Assert.AreEqual("Ask for help", container.Clone().GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.AreEqual("value", container.Clone().GetXPropertyAnnotation<string>("Test", "Another XProp"));

        var copy = t.Copy();
        
        lr = copy.Rows.Last();
        
        Assert.AreEqual("Ask for help", lr.GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.AreEqual("V", lr.XPropertyAnnotations.First());
        Assert.IsEmpty(fr.GetXPropertyAnnotation<string>("T", "Testing"));

        Assert.AreEqual("Ask for help", lr.ToContainer().GetXPropertyAnnotation<string>("V", "Testing"));
        Assert.IsEmpty(fr.ToContainer().GetXPropertyAnnotation<string>("T", "Testing"));
        
        Assert.Null(fr.GetXPropertyAnnotation<int?>("T", "MissingKey"));
        Assert.AreEqual(0,fr.GetXPropertyAnnotation<int>("T", "MissingKey"));
        Assert.IsEmpty(fr.GetXPropertyAnnotation<int[]>("T", "MissingKey"));
    }

    [Test]
    public void TestDataView()
    {
        var testTable = GetTestTable();

        var dataTableView = new DataTableView(testTable);
        
        Assert.AreEqual(testTable.RowCount, dataTableView.Count);
        
        Assert.AreEqual(dataTableView.Count, testTable.RowCount);
        
        Assert.True(dataTableView.Contains(testTable.Rows.First()));
        
        dataTableView.ApplySort(new DataTableColumnPropertyDescriptor(Fields.Name), ListSortDirection.Ascending);

        var dataRows = testTable.Rows.OrderBy(Fields.Name).ToData();

        for (int i = 0; i < dataTableView.Count; i++)
        {
            var row = (DataRow)dataTableView[i];
            
            Assert.True(row.Equals(dataRows[i]));
        }
        
        testTable.Rows.First().Delete();
        
        dataRows = testTable.Rows.OrderBy(Fields.Name).ToData();

        for (int i = 0; i < dataTableView.Count; i++)
        {
            var row = (DataRow)dataTableView[i];
            
            Assert.True(row.Equals(dataRows[i]));
        }
        
        dataTableView.ApplySort(new DataTableColumnPropertyDescriptor(Fields.Name), ListSortDirection.Descending);
        
        dataRows = testTable.Rows.OrderByDescending(Fields.Name).ToData();

        for (int i = 0; i < dataTableView.Count; i++)
        {
            var row = (DataRow)dataTableView[i];
            
            Assert.True(row.Equals(dataRows[i]));
        }
        
        testTable.Rows.Last().Delete();
        
        dataRows = testTable.Rows.OrderByDescending(Fields.Name).ToData();

        for (int i = 0; i < dataTableView.Count; i++)
        {
            var row = (DataRow)dataTableView[i];
            
            Assert.True(row.Equals(dataRows[i]));
        }
        
        dataTableView.BeginEdit();

        var rowObj =  (DataRow)dataTableView[0];

        var actual = Guid.NewGuid().ToString();
        
        rowObj[Fields.Name] = actual;
        
        dataRows = testTable.Rows.OrderByDescending(Fields.Name).ToData();

        for (int i = 0; i < dataTableView.Count; i++)
        {
            var row = (DataRow)dataTableView[i];
            
            Assert.True(row.Equals(dataRows[i]));
        }
            
        dataTableView.CancelEdit();

        rowObj = (DataRow)dataTableView[0];
        
        Assert.AreNotEqual(rowObj[Fields.Name], actual);
        
        dataRows = testTable.Rows.OrderByDescending(Fields.Name).ToData();

        for (int i = 0; i < dataTableView.Count; i++)
        {
            var row = (DataRow)dataTableView[i];
            
            Assert.True(row.Equals(dataRows[i]));
        }
    }

    private static void TestDeleteAndAdd(DataRow deletedRow, DataTable table, int expectedRowsCount)
    {
        deletedRow.Delete();

        Assert.AreEqual(table.RowCount, expectedRowsCount);

        Assert.AreEqual(table.Rows.Count(), table.RowCount);

        Assert.False(deletedRow.IsAddedRow);
        Assert.True(deletedRow.HasChanges);
        Assert.True(deletedRow.IsDeletedRow);
        Assert.AreEqual(deletedRow.RowRecordState, RowState.Deleted);

        Assert.AreEqual(table.AllRows.Count(), rowCount);

        Assert.IsNotEmpty(deletedRow.Field<string>(Fields.Name));

        Assert.Null(table.GetRowBy(deletedRow.Field<int>(Fields.id)));

        table.AcceptChanges();

        Assert.True(deletedRow.RowRecordState == RowState.Detached);

        Assert.Throws<DataDetachedException>(() => deletedRow.Field<string>(Fields.Name));

        Assert.AreEqual(expectedRowsCount, table.AllRows.Count());

        var row = table.NewRow().Set(Fields.id,-555).Set("UserClass", new ComplexUserClass() { ID = -555 }).Set("UserClass", new ComplexUserClass() { ID = -555 }).Set(Fields.Sn, 1).Set(Fields.Name, "Name " + -555).Set(Fields.Code, "Code " + -555 % 50);
            
        var newRow = table.ImportRow(row);

        Assert.True(newRow.IsAddedRow);

        Assert.AreEqual(rowCount, table.RowCount);

        Assert.AreEqual(table.RowCount, table.Rows.Count());

        Assert.AreEqual(table.AllRows.Count(), rowCount);

        var newRowFound = table.GetRowBy(-555);

        Assert.NotNull(newRowFound);

        Assert.AreEqual(newRowFound.Field<int>(Fields.id), newRow.Field<int>(Fields.id));
        Assert.AreEqual(newRowFound.Field<string>(Fields.Code), newRow.Field<string>(Fields.Code));
    }
}
}
