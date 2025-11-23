using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    
    [TestFixture]
    public class CopyTableTests
    {
        private DataTable m_sourceDataTable;
        private const int rowCount = 20;
        private const int initialCapacity = 101;
        
        private static DateTime m_timeNow = DateTime.Now;
        private static Guid m_guid = Guid.NewGuid();
        private static string s_tableName = "t_nmt";

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = s_tableName;

            FillTable(table);

            m_sourceDataTable = table;
        }

        [Test]
        public void TestCopy( [Values(TableTestMode.Copy, 
                TableTestMode.CloneMerge,
                TableTestMode.ReverseFullMerge,
                TableTestMode.Json,
                TableTestMode.Xml,
                TableTestMode.XmlSchemaMerge,
                TableTestMode.JsonSchemaMerge,
                TableTestMode.JsonDataset,
                TableTestMode.JsonDatasetMerge,
                TableTestMode.JsonDatasetSerializeDataOnly,
                TableTestMode.XmlDataset,
                TableTestMode.XmlDatasetMerge,
                TableTestMode.XmlDatasetSerializeDataOnly
                )] TableTestMode testMode, 
            [Values(true, false)] bool reverseColumns,
            [Values(true, false)] bool mutateTable)
        {
            Func<string, DataTable> createNewTableFunc = (string tableName) =>
            {
                var tbl = new DataTable();

                tbl.Name = tableName;

                FillTable(tbl, reverseColumns, mutateTable);

                return tbl;
            };
            
            var testTable = GetTestTable(testMode, createNewTableFunc);
            
            var sourceDataTable = new DataTable();

            sourceDataTable.Name = s_tableName;

            FillTable(sourceDataTable, reverseColumns, mutateTable);

            var equalsExt = sourceDataTable.EqualsExt(testTable);
            
            Assert.True(equalsExt.value, equalsExt.ToString());
        }

        public static DataTable GetTestTable(TableTestMode tableTestMode, Func<string, DataTable> createTable)
        {
            switch (tableTestMode)
            {
                case TableTestMode.Copy:
                {
                    return createTable(s_tableName).Copy();
                }
                case TableTestMode.CloneMerge:
                {
                    var source = createTable(s_tableName);
                    
                    var tbl = source.Clone();

                    tbl.FullMerge(source);

                    return tbl;
                }
                case TableTestMode.CopyMerge:
                {
                    var source = createTable(s_tableName);
                    
                    var tbl = source.Copy();

                    tbl.MergeDataOnly(source);

                    return tbl;
                }
                case TableTestMode.ReverseFullMerge:
                {
                    var source = createTable(s_tableName);
                    
                    var table = new DataTable();

                    table.Name = s_tableName;

                    FillTable(table, reverseColumns: true);

                    table.FullMerge(source);

                    return table;
                }
                case TableTestMode.Xml:
                {
                    var source = createTable(s_tableName);
                    
                    var xElement = source.ToXml(SerializationMode.Full);

                    var table = new DataTable();

                    table.LoadFromXml(xElement);

                    return table;
                }
                case TableTestMode.XmlSchemaMerge:
                {
                    var source = createTable(s_tableName);
                    
                    var xElement = source.ToXml(SerializationMode.SchemaOnly);

                    var table = new DataTable();

                    table.LoadMetadataFromXml(xElement);

                    table.MergeDataOnly(source);

                    return table;
                }
                case TableTestMode.Json:
                {
                    var source = createTable(s_tableName);
                    
                    var json = source.ToJson(SerializationMode.Full);

                    var table = new DataTable();

                    table.LoadFromJson(json);

                    return table;
                }
                case TableTestMode.JsonSchemaMerge:
                {
                    var source = createTable(s_tableName);
                    
                    var json = source.ToJson(SerializationMode.SchemaOnly);

                    var table = new DataTable();

                    table.LoadMetadataFromJson(json);

                    table.MergeDataOnly(source);

                    return table;
                }
                case TableTestMode.XmlDataset:
                {
                    var source = createTable(s_tableName);
                    
                    var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                    dataSet.AddTable(source.Copy());

                    var dsXElement = dataSet.ToXml(SerializationMode.Full);

                    var testDs = new DataTable();

                    testDs.LoadFromXml(dsXElement);

                    return (DataTable)testDs.GetTable(source.Name);
                }
                case TableTestMode.XmlDatasetMerge:
                {
                    var source = createTable(s_tableName);
                    
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
                    var source = createTable(s_tableName);
                    
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
                    var source = createTable(s_tableName);
                    
                    var dataSet = new DataTable() { TableName = "TestDataset", EnforceConstraints = true };

                    dataSet.AddTable(source.Copy());

                    var json = dataSet.ToJson(SerializationMode.Full);

                    var testDs = new DataTable();

                    testDs.LoadFromJson(json);

                    return (DataTable)testDs.GetTable(source.Name);
                }
                case TableTestMode.JsonDatasetMerge:
                {
                    var source = createTable(s_tableName);
                    
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
                    var source = createTable(s_tableName);
                    
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

        internal static void FillTable(DataTable table, bool reverseColumns = true, bool mutateTable = false)
        {
            DataTable.RegisterUserTypeStringMethods((s) => s.ToXml().ToString(),
                s => ComplexUserClass.LoadFromXml(XElement.Parse(s)));

            DataTable.RegisterUserType<ComplexPoint2D>();
            DataTable.RegisterUserType<ComplexUserClass>();

            var expressionAction = new Data<Action>()
            {
                () => table.AddColumn("Expr1", TableStorageType.String, dataExpression: "name + ' ' + id"),
                () => table.AddColumn("Expr2", TableStorageType.Int32, dataExpression: "id + id"),
            };

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
            
                () => table.AddColumn("Default", TableStorageType.String, defaultValue: "test4312"),
                () => table.AddColumn<ComplexUserClass>("UserClass"),
                () => table.AddColumn<ComplexPoint2D>("Point2D"),
                () => table.AddColumn<DataRowContainer>("DataRowContainer"),
                () => table.AddColumn(Fields.Directories.Data, TableStorageType.Double, TableStorageTypeModifier.Array),
                () => table.AddColumn(Fields.Directories.DataTypes, TableStorageType.Type, TableStorageTypeModifier.Array),
                () => table.AddColumn(Fields.Directories.TimeRange, TableStorageType.TimeSpan, TableStorageTypeModifier.Range),

                () => table.AddIndex("UserClass", true),
                () => table.AddIndex("Point2D"),

                //todo column x prop
              //  () => table.GetColumn(Fields.Code).SetXProperty(DataTable.StringIndexCaseSensitiveXProp, false),

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
            
            foreach (var action in expressionAction)
            {
                action();
            }

            table.AddIndex(Fields.Code);
            table.AddIndex(Fields.Directories.TimeRange);
            table.SetPrimaryKeyColumn(Fields.id);

            table.Capacity = 100;

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

                setNewRow(newRow, Fields.id, id, newRowFieldSet);
                setNewRow(newRow, Fields.Name, "Name " + id, newRowFieldSet);

                setNewRow(newRow, Fields.Directories.CreateDt, m_timeNow.AddMilliseconds(i), newRowFieldSet);
                setNewRow(newRow, Fields.Directories.LmDt, m_timeNow.AddSeconds(i), newRowFieldSet);
                setNewRow(newRow, Fields.Directories.LmId, -i, newRowFieldSet);
                setNewRow(newRow, Fields.groupid, -i, newRowFieldSet);
                setNewRow(newRow, Fields.Directories.Guid, m_guid, newRowFieldSet);
                setNewRow(newRow, Fields.Directories.Code, "Code " + id % 2, newRowFieldSet);
                setNewRow(newRow, "UserClass", new ComplexUserClass() { ID = id }, newRowFieldSet);
                setNewRow(newRow, "Point2D", new ComplexPoint2D(id % 2, id % 3), newRowFieldSet);
                setNewRow(newRow, "DataRowContainer", CreateTestRowContainer(id), newRowFieldSet);
                setNewRow(newRow, Fields.Directories.Data, new double[] { 1, 2, 3 }, newRowFieldSet);
                setNewRow(newRow, Fields.Directories.DataTypes, new Type[] { typeof(object), typeof(int), typeof(DataTable) }, newRowFieldSet);
                setNewRow(newRow, Fields.Directories.TimeRange, new Range<TimeSpan>(TimeSpan.FromHours(1 + i), TimeSpan.FromHours(1 + i) + TimeSpan.FromSeconds(1 + i)), newRowFieldSet);

                setNewRow(newRow, TableStorageType.BigInteger.ToString(), bigInteger, newRowFieldSet);
                setNewRow(newRow, TableStorageType.Type.ToString(), typeof(object), newRowFieldSet);
                setNewRow(newRow, TableStorageType.Json.ToString(), new JsonObject() { { "ID", "max" } }, newRowFieldSet);
                setNewRow(newRow, TableStorageType.Xml.ToString(), new XElement("Root", new XAttribute("Attr1", "test1"), new XAttribute("Attr2", "test2")), newRowFieldSet);
                setNewRow(newRow, TableStorageType.Boolean.ToString(), true, newRowFieldSet);
                setNewRow(newRow, TableStorageType.Uri.ToString(), new Uri("https://microsoft.com"), newRowFieldSet);
                setNewRow(newRow, TableStorageType.Byte + "." + TableStorageTypeModifier.Array, Encoding.UTF8.GetBytes("TableStorageType.ByteArray.ToString()"), newRowFieldSet);
                setNewRow(newRow, TableStorageType.Char + "." + TableStorageTypeModifier.Array, "false".ToCharArray(), newRowFieldSet);
                setNewRow(newRow, TableStorageType.Char.ToString(), 'f', newRowFieldSet);
                setNewRow(newRow, TableStorageType.Boolean.ToString(), "true", newRowFieldSet);
                
                newRow.SetXProperty("TEST", id.ToString());
                newRow.SetXProperty("Point2D", new ComplexPoint2D(id % 2, id % 3));
                newRow.SetRowInfo("TEST INFO");
                newRow.SetXPropertyAnnotation("TEST", "Info", "This is test property");
                
                var dataRow = table.AddRow(newRow);

                MutateRow(dataRow, setRow, id, i, bigInteger);
            }

            load.EndLoad();

            var dataRows = table.AllRows.ToArray();
            
            Assert.AreEqual(rowCount, dataRows.Length);

            if (mutateTable)
            {
                int maxId;

                var toRemove = MutateTableUnderTransactionsWithRollback(table);

                maxId = (int)table.Max(Fields.id);

                foreach (var row in toRemove)
                {
                    row.Delete();
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

                    dataTableRows.Add(table.AddRow(newRow));
                }

                table.AcceptChanges();

                foreach (var row in dataTableRows)
                {
                    row.Delete();
                }
            }
        }

        private static void MutateRow(DataRow dataRow, Action<DataRow, string, object, Set<string>> setRow, int id, int i, BigInteger bigInteger)
        {
            dataRow.SetRowInfo(string.Empty);

            var rowFieldSet = new Set<string>();

            setRow(dataRow, Fields.Name, "Name " + id, rowFieldSet);
            setRow(dataRow, Fields.Directories.CreateDt, m_timeNow.AddMilliseconds(i), rowFieldSet);
            setRow(dataRow, Fields.Directories.LmDt, m_timeNow.AddDays(i), rowFieldSet);
            setRow(dataRow, Fields.Directories.LmId, -i, rowFieldSet);
            setRow(dataRow, Fields.groupid, -i, rowFieldSet);
            setRow(dataRow, Fields.Directories.Guid, m_guid, rowFieldSet);
            setRow(dataRow, Fields.Directories.Code, "Code " + id % 2, rowFieldSet);
            setRow(dataRow, "UserClass", new ComplexUserClass() { ID = id }, rowFieldSet);
            setRow(dataRow, "Point2D", new ComplexPoint2D(id % 2, id % 3), rowFieldSet);

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

            //todo test boolean convert on set
            dataRow[TableStorageType.Boolean.ToString()] = "true";
            dataRow.Set(TableStorageType.Boolean.ToString(), "true");
            dataRow.Set(TableStorageType.Boolean.ToString(), 1);

            //todo test complex set and get of compex type
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

            //todo Silently set for service columns
            dataRow.SilentlySetValue(TableStorageType.Boolean.ToString(), true);
            dataRow.SilentlySetValue(TableStorageType.Uri.ToString(), new Uri("https://microsoft.com"));
            dataRow.SilentlySetValue(TableStorageType.Byte + "." + TableStorageTypeModifier.Array, Encoding.UTF8.GetBytes("TableStorageType.ByteArray.ToString()"));
            dataRow.SilentlySetValue(TableStorageType.Char + "." + TableStorageTypeModifier.Array, "false".ToCharArray());
            dataRow.SilentlySetValue(TableStorageType.Char.ToString(), 'f');
            dataRow.SilentlySetValue("UserClass", new ComplexUserClass() { ID = id });
            dataRow.SilentlySetValue("Point2D", new ComplexPoint2D(id % 2, id % 3));
            dataRow.SilentlySetValue("DataRowContainer", CreateTestRowContainer(id));
            dataRow.SilentlySetValue(TableStorageType.Boolean.ToString(), "true");
            dataRow.SilentlySetValue(TableStorageType.Boolean.ToString(), 1);
        }

        private static List<DataRow> MutateTableUnderTransactionsWithRollback(DataTable table)
        {
            var transaction = table.StartTransaction();

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
            
            return toRemove;
        }

        private class DataOwner : IDataOwner
        {
        }
        
        internal static DataRowContainer CreateTestRowContainer(int id)
        {
            var dataRowContainer = new DataRowContainer();

            var columnContainer = new DataColumnContainer() { ColumnName = Fields.id, Type = TableStorageType.Int32, IsReadOnly = true};
            
            var metadataProps = new CoreContainerMetadataProps("Test",
                new [] { columnContainer},
                new Map<string, CoreDataColumnContainer>() { {Fields.id, columnContainer} }, 
                Array.Empty<string>(), 
                0);
        
            var containerProps = new ContainerDataProps(-1, _.List((object)id));
            
            dataRowContainer.Init(metadataProps, containerProps);

            return dataRowContainer;
        }
    }
}