using System;
using System.Data;
using System.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class TestExtendedProperties
    {
        [Test]
        public void TestColumnSetXProp()
        {
            var table = GetTestTable();

            var nameColumn = table.GetColumn(Fields.Name);

            nameColumn.SetXProperty("IsCustom", true);

            Assert.True(nameColumn.GetXProperty<bool>("IsCustom"));
            Assert.IsNotEmpty(nameColumn.XProperties);
        }

        [Test]
        public void TestColumnXPropNullIfMissing()
        {
            var table = GetTestTable();
            
            var nameColumn = table.GetColumn(Fields.Name);
            
            Assert.Null(nameColumn.GetXProperty<Guid?>(Guid.NewGuid().ToString()));
        }
        
        [Test]
        public void TestColumnXPropCopied()
        {
            var table = GetTestTable();
            
            var nameColumn = table.GetColumn(Fields.Name);

            var xPropValue = new int[] { 1, 2, 3 };

            nameColumn.SetXProperty("Custom", xPropValue);

            var clone = table.Clone();

            var cloneNameColumn = clone.GetColumn(Fields.Name);

            var clonedXPropValue = cloneNameColumn.GetXProperty<int[]>("Custom");

            Assert.AreEqual(xPropValue, clonedXPropValue);
            Assert.False(ReferenceEquals(xPropValue, clonedXPropValue));
        }
        
        [Test]
        public void TestColumnXPropertiesDebug()
        {
            var testTable = GetTestTable();

            var dataColumn = testTable.GetColumn(Fields.Name);
            
            dataColumn.SetXProperty("Test", "Test");

            var extPropertiesDebug = testTable.DataColumnInfo.ExtPropertiesDebug;
        
            Assert.True(extPropertiesDebug.Length > 0);
        }

        [Test]
        public void TestRowContainerColumnXPropCopied()
        {
            var table = GetTestTable();
            
            var nameColumn = table.GetColumn(Fields.Name);

            var xPropValue = new int[] { 1, 2, 3 };

            nameColumn.SetXProperty("Custom", xPropValue);

            var container = table.Rows.First().ToContainer();

            var cloneNameColumn = container.GetColumn(Fields.Name);

            var clonedXPropValue = cloneNameColumn.GetXProperty<int[]>("Custom");

            Assert.AreEqual(xPropValue, clonedXPropValue);
            Assert.False(ReferenceEquals(xPropValue, clonedXPropValue));
            Assert.IsNotEmpty(cloneNameColumn.XProperties);
        }

        [Test]
        public void TestRowContainerCopyColumnXPropCopied()
        {
            var table = GetTestTable();
            
            var nameColumn = table.GetColumn(Fields.Name);

            var xPropValue = new int[] { 1, 2, 3 };

            nameColumn.SetXProperty("Custom", xPropValue);

            var container = table.Rows.First().ToContainer().Clone();

            var cloneNameColumn = container.GetColumn(Fields.Name);

            var clonedXPropValue = cloneNameColumn.GetXProperty<int[]>("Custom");

            Assert.AreEqual(xPropValue, clonedXPropValue);
            Assert.False(ReferenceEquals(xPropValue, clonedXPropValue));
            Assert.IsNotEmpty(cloneNameColumn.XProperties);
        }
        
        [Test]
        public void TestRemovingColumnClearsXProps()
        {
            var table = GetTestTable();
            
            var nameColumn = table.GetColumn(Fields.Name);

            var xPropValue = new int[] { 1, 2, 3 };

            nameColumn.SetXProperty("Custom", xPropValue);
            nameColumn.SetXProperty("CustomFlag", true);

            Assert.True(table.CanRemoveColumn(Fields.Name));
            table.RemoveColumn(Fields.Name);
        
            table.AddColumn(Fields.Name, TableStorageType.Single);

            var newNameColumn = table.GetColumn(Fields.Name);

            Assert.IsEmpty(newNameColumn.GetXProperty<int[]>("Custom"));
            Assert.IsNull(newNameColumn.GetXProperty<bool?>("CustomFlag"));
            
            Assert.IsEmpty(newNameColumn.XProperties);
        }

        [Test]
        public void TestRowXPropertiesComplexSet()
        {
            var row = GetTestTableRow();

            var value = new ComplexPoint2D(1, 2);
            
            row.SetXProperty("Test", value);

            var valFromRow = row.GetXProperty<ComplexPoint2D>("Test");

            Assert.AreEqual(value, valFromRow);
            Assert.True(ReferenceEquals(value, valFromRow)); // Because ComplexPoint2D IsReadonly/Immutable
        }
        
        [Test]
        public void TestRowXPropertiesComplexSetContainer()
        {
            var row = GetTestTableRow();

            var value = new ComplexPoint2D(1, 2);
            
            row.SetXProperty("Test", value);

            var valFromRow = row.ToContainer().GetXProperty<ComplexPoint2D>("Test");

            Assert.AreEqual(value, valFromRow);
            Assert.True(ReferenceEquals(value, valFromRow)); // Because ComplexPoint2D IsReadonly/Immutable
        }
        
        [Test]
        public void TestRowXPropertiesComplexSetContainerReject()
        {
            var row = GetTestTableRow();

            var value = new ComplexPoint2D(1, 2);
            row.SetXProperty("Test", value);

            var dataRowContainer = row.ToContainer();
            
            dataRowContainer.SetXProperty("Test", (ComplexPoint2D)null);

            var nullVal = dataRowContainer.GetXProperty<ComplexPoint2D>("Test");
            
            dataRowContainer.RejectChanges();

            var valAfterReject = dataRowContainer.GetXProperty<ComplexPoint2D>("Test");

            Assert.AreEqual(valAfterReject, value);
            Assert.Null(nullVal);
            Assert.True(ReferenceEquals(value, valAfterReject)); // Because ComplexPoint2D IsReadonly/Immutable
        }
        
        [Test]
        public void TestRowXPropertiesComplexSetContainerToJson()
        {
            var row = GetTestTableRow();

            var value = new ComplexPoint2D(1, 2);
            
            row.SetXProperty("Test", value);

            var fromJson = new DataRowContainer();

            fromJson.FromJson(row.ToContainer().ToJson());
            
            var valFromRow = fromJson.GetXProperty<ComplexPoint2D>("Test");

            Assert.AreEqual(value, valFromRow);
            Assert.False(ReferenceEquals(value, valFromRow)); // Serialization
        }
        
        [Test]
        public void TestRowXPropertiesComplexSetContainerToXml()
        {
            var row = GetTestTableRow();

            var value = new ComplexPoint2D(1, 2);
            
            row.SetXProperty("Test", value);

            var fromXml = new DataRowContainer();

            fromXml.FromXml(row.ToContainer().ToXml());
            
            var valFromRow = fromXml.GetXProperty<ComplexPoint2D>("Test");

            Assert.AreEqual(value, valFromRow);
            Assert.False(ReferenceEquals(value, valFromRow)); // Serialization
        }

        [Test]
        public void TestRowXPropertiesEmptyOrNull()
        {
            var row = GetTestTableRow();

            Assert.IsEmpty(row.GetXProperty<int[]>("Missing"));
            Assert.Null(row.GetXProperty<int?>("Missing"));
        }

        [Test]
        public void TestRowXPropertiesOriginalValue()
        {
            var testTable = GetTestTable();

            var row = testTable.Rows.First();

            row.SetXProperty("Test", true);
            row.SetXProperty("Test", false);
            
            var set = row.GetChangedXProperties().ToSet();
            
            Assert.Null(row.GetOriginalValue<bool?>("Test"));
            Assert.Null(row.GetXProperty<bool?>("Test", original: true));
            
            Assert.True(set.Contains("Test"));
            Assert.AreEqual(RowState.Added, row.RowRecordState);
        }
        
        [Test]
        public void TestRowXPropertiesOriginalValue2()
        {
            var testTable = GetTestTable();

            var row = testTable.Rows.First();

            row.SetXProperty("Test", true);
            
            testTable.AcceptChanges();
            
            row.SetXProperty("Test", false);
            var rowRowRecordState = row.RowRecordState;

            var set = row.GetChangedXProperties().ToSet();
            
            Assert.True(row.GetOriginalValue<bool?>("Test"));
            Assert.True(row.GetXProperty<bool>("Test", original: true));
            
            Assert.True(set.Contains("Test"));
            Assert.AreEqual(RowState.Modified, rowRowRecordState);
        }
        
        [Test]
        public void TestRowXPropertiesRejectChanges()
        {
            var testTable = GetTestTable();
            testTable.AcceptChanges();
            
            var row = testTable.Rows.First();

            row.SetXProperty("Test", true);
            
            testTable.RejectChanges();
            
            var rowRowRecordState = row.RowRecordState;

            var set = row.GetChangedXProperties().ToSet();
            
            Assert.Null(row.GetOriginalValue<bool?>("Test"));
            Assert.Null(row.GetXProperty<bool?>("Test", original: true));
            
            Assert.False(set.Contains("Test"));
            Assert.AreEqual(RowState.Unchanged, rowRowRecordState);
        }

        [Test]
        public void TestRowXPropertyInfoMissing()
        {
            var test = GetTestTable();

            test.AcceptChanges();

            var dataRow = test.Rows.First();

            var xPropertyInfo = dataRow.GetXPropertyAnnotation<string>("TEST", "Info");

            Assert.NotNull(xPropertyInfo);
        }

        [Test]
        public void TestRowXPropertyInfoSet()
        {
            var dataRow = GetTestTableRow();

            dataRow.SetXPropertyAnnotation("Test", "Key", 1);

            Assert.AreEqual(1, dataRow.GetXPropertyAnnotation<int>("Test", "Key"));
        }
        
        [Test]
        public void TestRowXPropertyInfoSetComplex()
        {
            var dataRow = GetTestTableRow();

            var complexPoint2D = new ComplexPoint2D(1, 2);
            
            dataRow.SetXPropertyAnnotation("Test", "Key", complexPoint2D);

            Assert.AreEqual(complexPoint2D, dataRow.GetXPropertyAnnotation<ComplexPoint2D>("Test", "Key"));
            
            Assert.True(ReferenceEquals(complexPoint2D, dataRow.GetXPropertyAnnotation<ComplexPoint2D>("Test", "Key")));
        }
        
        [Test]
        public void TestRowXPropExpressionFunc()
        {
            var table = new DataTable();

            table.Columns.Add(new DataColumnContainer() { Type = TableStorageType.Int32, ColumnName = "R" });

            var r1 = table.Rows.Add(table.NewRow(_.MapObj(("R", 1)))).First();
            var r2 = table.Rows.Add(table.NewRow(_.MapObj(("R", 2)))).First();

            r1.SetXProperty("Test", 1);
            r2.SetXProperty("Tag", 2);

            /*Assert.AreEqual(1, table.Select<DataRow>("cInt(RowXProp('Test')) = 1").ToArray().Length);
            Assert.AreEqual(0, table.Select<DataRow>("cInt(RowXProp('Test')) = 2").ToArray().Length);
            Assert.AreEqual(1, table.Select<DataRow>("cInt(RowXProp('Tag')) = 2").ToArray().Length);
            Assert.AreEqual(0, table.Select<DataRow>("cInt(RowXProp('Tag')) = 1").ToArray().Length);
            Assert.AreEqual(1, table.Select<DataRow>("RowXProp('Test') = 1").ToArray().Length);*/
            Assert.AreEqual(1, table.Select<DataRow>("R != 1").ToArray().Length);
            Assert.AreEqual(1, table.Select<DataRow>("R != 2").ToArray().Length);
            Assert.AreEqual(1, table.Select<DataRow>("RowXProp('Test') != 1").ToArray().Length);
            Assert.AreEqual(1, table.Select<DataRow>("not RowXProp('Test') = 1").ToArray().Length);
        }

        private DataRow GetTestTableRow()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(Fields.Name);

            dataTable.AddRow(_.MapObj((Fields.Name, "test")));

            return dataTable.Rows.First();
        }

        private DataTable GetTestTable()
        {
            var dataTable = new DataTable();

            dataTable.AddColumn(Fields.Name);

            dataTable.AddRow(_.MapObj((Fields.Name, "test")));

            return dataTable;
        }
    }
}