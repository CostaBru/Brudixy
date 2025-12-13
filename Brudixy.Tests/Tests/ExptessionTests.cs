using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using Microsoft.VisualBasic.CompilerServices;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class ExpressionTests
    {
        private DataTable m_table;

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            CommonTest.FillTable(table);

            table.AddIndex(TableStorageType.Int32.ToString());

            m_table = table;
        }

        [Test]
        public void TestSelectByIndex()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            rows = table.Select<DataRow>("id = 5").ToArray();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.All(r => r.Field<int>("id") == 5));

            Assert.True(rows.First().CheckFilter("id = 5"));
            Assert.True(rows.First().CheckFilter("id = 6", _.MapObj(("id", 6))));

            Assert.True(rows.First().ToContainer().CheckFilter("id = 5"));
            Assert.True(rows.First().ToContainer().CheckFilter("id = 6", _.MapObj(("id", 6))));
            
        }

        [Test]
        public void TestNumericExpr()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            rows = table.Select<DataRow>("Int32 = 5").ToData();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.All(r => r.Field<int>("Int32") == 5));

            var filter =
                "Int16 > 5 and Int32 > 5 and Int64 > 5 and UInt16 > 5 and UInt32 > 5 and UInt64 > 5 and Byte > 5 and SByte > 5 and Char > 5";

            rows = table.Select<DataRow>(filter).ToData();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.All(r => r.Field<int>("Int32") > 5 && r.Field<Int16>("Int16") > 5));

            Assert.True(rows.First().CheckFilter(filter));
            Assert.True(rows.First().ToContainer().CheckFilter(filter));
        }

        [Test]
        public void TestConvert()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            rows = table.Select<DataRow>("Int32 = CONVERT('5', INT32)").ToData();

            Assert.IsNotEmpty(rows);

            Assert.AreEqual(5, rows.First().Field<int>("Int32"));

            rows = table.Select<DataRow>("Int32 = CONVERT(5.0, INT32)").ToData();

            Assert.IsNotEmpty(rows);

            Assert.AreEqual(5, rows.First().Field<int>("Int32"));
        }

        [Test]
        public void TestBinaryOp()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            var numerics = new TableStorageType[]
            {
                TableStorageType.Int32,
                TableStorageType.SByte, TableStorageType.Byte,
                TableStorageType.Decimal, TableStorageType.Double, TableStorageType.Single,
                TableStorageType.Int16 , TableStorageType.Int64,
                TableStorageType.UInt16, TableStorageType.UInt32, TableStorageType.UInt64,
            };

            foreach (var type in numerics)
            {
                var strings = new string[] { "+", "-", "/", "*" };

                foreach (var s in strings)
                {
                    rows = table.Select<DataRow>($"{type} > 0 and {type} < 128 and ({type} {s} 1) = ({type} {s} 1)")
                        .ToData();

                    Assert.IsNotEmpty(rows, "" + type + " " + s);

                    rows = table
                        .Select<DataRow>($"{type} > 0 and {type} < 128 and (({type} {s} 1) + 1) >= ({type} {s} 1)")
                        .ToData();

                    Assert.IsNotEmpty(rows, "" + type + " " + s);

                    rows = table
                        .Select<DataRow>($"{type} > 0 and {type} < 128 and (({type} {s} 1) + 1) > ({type} {s} 1)")
                        .ToData();

                    Assert.IsNotEmpty(rows, "" + type + " " + s);

                    rows = table
                        .Select<DataRow>($"{type} > 0 and {type} < 128 and ({type} {s} 1) < (({type} {s} 1) + 1)")
                        .ToData();

                    Assert.IsNotEmpty(rows, "" + type + " " + s);

                    rows = table
                        .Select<DataRow>($"{type} > 0 and {type} < 128 and ({type} {s} 1) <= (({type} {s} 1) + 1)")
                        .ToData();

                    Assert.IsNotEmpty(rows, "" + type + " " + s);
                }
            }

            var nameNameNameName = $"(Name + ' ' + Name) = 'Name 10 Name 10'";

            rows = table.Select<DataRow>(nameNameNameName).ToData();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.First().CheckFilter(nameNameNameName));
            Assert.True(rows.First().ToContainer().CheckFilter(nameNameNameName));
        }

        [Test]
        public void TestException()
        {
            var table = GetTestTable();

           // Assert.AreEqual(0, table.Select<DataRow>("[Id] = 'qwerty'").ToData().Length);
            Assert.AreEqual(1, table.Select<DataRow>("[Id] = '1'").ToData().Length);
            Assert.AreEqual(1, table.Select<DataRow>("[Id] = 1").ToData().Length);

            Assert.Throws<SyntaxErrorException>(() => table.Select<DataRow>("[Name"));
            Assert.Throws<SyntaxErrorException>(() => table.Select<DataRow>("Name IN"));
            Assert.Throws<SyntaxErrorException>(() => table.Select<DataRow>("[Id] ! 1"));

            Assert.Throws<SyntaxErrorException>(() => table.Rows.First().CheckFilter("[Name"));
            Assert.Throws<SyntaxErrorException>(() => table.Rows.First().CheckFilter("Name IN"));
            Assert.Throws<SyntaxErrorException>(() => table.Rows.First().CheckFilter("[Id] ! 1"));
        }

        [Test]
        public void TestLen()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            rows = table.Select<DataRow>("Len(Name) > 5").ToData();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.All(r => r.Field<string>("Name").Length > 5));

            rows = table.Select<DataRow>("Len([Byte.Array]) > 0").ToData();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.All(r => r.Field<byte[]>("Byte.Array").Length > 0));
        }

        [Test]
        public void TestIif()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            var iifLenNameTrueFalse = "IIF(Len(Name) > 5, True, False)";

            rows = table.Select<DataRow>(iifLenNameTrueFalse).ToData();

            Assert.IsNotEmpty(rows);

            Assert.True(rows.All(r => r.Field<string>("Name").Length > 5));

            Assert.True(rows.First().CheckFilter(iifLenNameTrueFalse));
            Assert.True(rows.First().ToContainer().CheckFilter(iifLenNameTrueFalse));
        }

        [Test]
        public void TestTrimSubstring()
        {
            IEnumerable<DataRow> rows;

            var table = GetTestTable();

            rows = table.Select<DataRow>("TRIM(SUBSTRING(Name, 0, 5)) = 'Name'").ToData();

            Assert.IsNotEmpty(rows);

            var dataRows = rows.Where(r => r.Field<string>("Name").Substring(0, 5).Trim() != "Name").ToData();

            Assert.AreEqual(0, dataRows.Count);
        }

        [Test]
        public void TestIsNull()
        {
            var table = GetTestTable();

            var rows = table.Select<DataRow>("IsNULL(NULL, NULL) is null").ToData();

            Assert.AreEqual(table.RowCount, rows.Count);

            rows = table.Select<DataRow>("IsNULL(NULL, 1) is not null ").ToData();

            Assert.AreEqual(table.RowCount, rows.Count);

            rows = table.Select<DataRow>("IsNULL(1, NULL) is not null ").ToData();

            Assert.AreEqual(table.RowCount, rows.Count);
        }

        [Test]
        public void TestABS()
        {
            var table = GetTestTable();

            var rows = table.Select<DataRow>("ABS(-1) = 1").ToData();

            Assert.AreEqual(table.RowCount, rows.Count);

            rows = table.Select<DataRow>("ABS(1) = 1").ToData();

            Assert.AreEqual(table.RowCount, rows.Count);
        }

        [Test]
        public void TestMissingColumn()
        {
            var table = GetTestTable();

            Assert.Throws<MissingMetadataException>(() => table.AddColumn("BadExp", dataExpression: "name1 + ' ' + id2"));

            var ls = table.BeginLoad();

            table.AddColumn("BadExp1", dataExpression: "name1 + ' ' + id2");

            Assert.Throws<MissingMetadataException>(() => ls.EndLoad());
        }

        private DataTable GetTestTable()
        {
            var d1 = m_table.GetColumn("Int32");

            var testTable = m_table.Copy();

            var d2 = testTable.GetColumn("Int32");

            return testTable;
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

            var idIn = "id IN (5, 6, 7)";

            var rows = table.Select<DataRow>(idIn).ToData();

            Assert.IsNotEmpty(rows);
            Assert.AreEqual(3, rows.Count);

            Assert.True(rows.First().CheckFilter(idIn));
            Assert.True(rows.First().ToContainer().CheckFilter(idIn));
        }

        [Test]
        public void TestSelectExpression4()
        {
            var table = GetTestTable();

            var codeLikeCode = "code LIKE 'code %'";

            var rows = table.Select<DataRow>(codeLikeCode);

            Assert.IsNotEmpty(rows);

            Assert.True(rows.First().CheckFilter(codeLikeCode));
            Assert.True(rows.First().ToContainer().CheckFilter(codeLikeCode));
        }

        [Test]
        public void TestSelectExpression5()
        {
            var table = GetTestTable();

            var idInOrCodeLikeCode = "id IN (5, 6, 7) OR code LIKE 'code %'";

            var rows = table.Select<DataRow>(idInOrCodeLikeCode);

            Assert.IsNotEmpty(rows);

            Assert.True(rows.First().CheckFilter(idInOrCodeLikeCode));
            Assert.True(rows.First().ToContainer().CheckFilter(idInOrCodeLikeCode));
        }

        [Test]
        public void TestXML()
        {
            var table = GetTestTable();

            var rows = table.Select<DataRow>("XAttribute(Xml, 'Attr1') = 'test1'").ToArray();

            Assert.IsNotEmpty(rows);

            rows = table.Select<DataRow>("cInt(XValue(XElement(Xml, 'Child2'))) = 4").ToArray();

            Assert.IsNotEmpty(rows);

            rows = table.Select<DataRow>("Contains(XValue(GetByIndex(XSelectElements(Xml, 'Child1'), 0)), '1')")
                .ToArray();

            Assert.IsNotEmpty(rows);

            rows = table.Select<DataRow>("Contains(XSelectAttributes(Xml, 'ChildAttr'), 'test_2')").ToArray();

            Assert.IsNotEmpty(rows);

            rows = table.Select<DataRow>("cInt(XValue(GetByIndex(XQueryElements(Xml, './Child2'), 0))) = 4").ToArray();

            Assert.IsNotEmpty(rows);

            rows = table.Select<DataRow>("Contains(XAttributeNames(Xml), 'Attr2')").ToArray();

            Assert.IsNotEmpty(rows);
        }

        [Test]
        public void TestRange()
        {
            var table = new DataTable();

            table.Columns.Add(new DataColumnContainer() { Type = TableStorageType.Int32, TypeModifier = TableStorageTypeModifier.Range, ColumnName = "R" });

            table.Rows.Add(table.NewRow(_.MapObj(("R", new Range<int>(1, 10)))));
            table.Rows.Add(table.NewRow(_.MapObj(("R", new Range<int>(100, 1000)))));

            Assert.AreEqual(1, table.Select<DataRow>("CONTAINS(R, 5)").ToArray().Length);
            Assert.AreEqual(0, table.Select<DataRow>("CONTAINS(R, 50)").ToArray().Length);
            Assert.AreEqual(1, table.Select<DataRow>("CONTAINS(R, 150)").ToArray().Length);

            Assert.AreEqual(1, table.Select<DataRow>("CONTAINS(R, RANGE(2,8))").ToArray().Length);

            Assert.AreEqual(2, table.Select<DataRow>("INTERSECTS(R, RANGE(9,101))").ToArray().Length);
            Assert.AreEqual(0, table.Select<DataRow>("INTERSECTS(R, RANGE(-1,0))").ToArray().Length);
            Assert.AreEqual(2, table.Select<DataRow>("INTERSECTS(R, RANGE(0,10000))").ToArray().Length);
        }

       
        
        [Test]
        public void TestExpressionColumnChangingSubscriptionCancelEdit()
        {
            var table = new DataTable();

            table.Columns
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Code" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Name" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Expr", Expression = "[Code] + ' ' + [Name]" });

            table.Rows.Add(table.NewRow(_.MapObj(("Code", "T"), ("Name", "N"))));

            bool wasCalled = false;
            bool wasCalledChanged = false;
            bool wasCalledChangedAll = false;
            bool wasCalledChangingAll = false;

            table.SubscribeColumnChanging<string>("Expr", (e, c) =>
            {
                wasCalled = true;

                e.IsCancel = e.NewValue == "T M";
            });
            
            table.ColumnChanging.Subscribe((e, c) =>
            {
                wasCalledChangingAll = wasCalledChangingAll || e.ColumnName == "Expr";

                if (wasCalledChangingAll)
                {
                    e.IsCancel = true;
                }
            });
            
            table.SubscribeColumnChanged("Expr", (e, c) =>
            {
                wasCalledChanged = wasCalledChanged || e.ChangedColumnNames.Contains("Expr");
            });
            
            table.ColumnChanged.Subscribe((e, c) =>
            {
                wasCalledChangedAll = wasCalledChangedAll || e.ChangedColumnNames.Contains("Expr");
            });
            
            var row = table.Rows.First();
            
            Assert.AreEqual("T N", row.Field<string>("Expr"));

            row["Name"] = "M";
            
            Assert.True(wasCalled);
            Assert.False(wasCalledChanged);
            Assert.False(wasCalledChangedAll);
            
            row["Name"] = "B";
            
            Assert.AreEqual("T N", row.Field<string>("Expr"));
            Assert.AreEqual("N", row.Field<string>("Name"));
            
            Assert.True(wasCalledChangingAll);
            Assert.False(wasCalledChanged);
            Assert.False(wasCalledChangedAll);
            
            Assert.AreEqual("T N", row.Field<string>("Expr"));
            Assert.AreEqual("N", row.Field<string>("Name"));
        }
        
        [Test]
        public void TestExpressionColumnChangedSubscription()
        {
            var table = new DataTable();

            table.Columns
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Code" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Name" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Expr", Expression = "[Code] + ' ' + [Name]" });

            table.Rows.Add(table.NewRow(_.MapObj(("Code", "T"), ("Name", "N"))));

            bool wasCalledChanged = false;
            bool wasCalledChangedAll = false;

            table.SubscribeColumnChanged("Expr", (e, c) =>
            {
                if (e.ChangedColumnNames.Contains("Expr"))
                {
                    wasCalledChanged = true;

                    Assert.AreEqual("T N", e.GetOldValue("Expr"));
                    Assert.AreEqual("T B", e.GetNewValue("Expr"));
                }
            });
            
            table.ColumnChanged.Subscribe((e, c) =>
            {
                if (e.ChangedColumnNames.Contains("Expr"))
                {
                    wasCalledChangedAll = true;
                    
                    Assert.AreEqual("T N", e.GetOldValue("Expr"));
                    Assert.AreEqual("T B", e.GetNewValue("Expr"));
                }
            });
            
            var row = table.Rows.First();
            
            Assert.AreEqual("T N", row.Field<string>("Expr"));

            row["Name"] = "B";
            
            Assert.True(wasCalledChanged);
            Assert.True(wasCalledChangedAll);
        }

        [Test]
        public void TestRowContainerExpressionColumnChangedSubscription()
        {
            var table = new DataTable();

            table.Columns
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Code" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Name" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Expr", Expression = "[Code] + ' ' + [Name]" });

            table.Rows.Add(table.NewRow(_.MapObj(("Code", "T"), ("Name", "N"))));
            
            var dataRowContainer = table.Rows.First().ToContainer();
            
            bool wasCalledChanged = false;
            bool wasCalledChanging = false;
            
            dataRowContainer.FieldValueChangingEvent.Subscribe((e, c) =>
            {
                wasCalledChanging = e.ColumnName == "Expr";

                if (wasCalledChanging)
                {
                    Assert.AreEqual("T N", e.OldValue);
                    //Assert.AreEqual("T B", e.NewValue);
                }
            });
            
            dataRowContainer.FieldValueChangedEvent.Subscribe((e, c) =>
            {
                wasCalledChanged = e.ChangedColumnNames.Contains("Expr");
                
                if (wasCalledChanged)
                {
                    Assert.AreEqual("T N", e.GetOldValue("Expr"));
                    Assert.AreEqual("T B", e.GetNewValue("Expr"));
                }
            });

            Assert.AreEqual("T N", dataRowContainer.Field<string>("Expr"));

            dataRowContainer["Name"] = "B";
            
            Assert.True(wasCalledChanged);
            Assert.True(wasCalledChanged);
        }
        
        [Test]
        public void TestRowContainerExpressionColumnChangingSubscriptionCancelEdit()
        {
            var table = new DataTable();

            table.Columns
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Code" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Name" })
                .Add(new DataColumnContainer() { Type = TableStorageType.String, ColumnName = "Expr", Expression = "[Code] + ' ' + [Name]" });

            table.Rows.Add(table.NewRow(_.MapObj(("Code", "T"), ("Name", "N"))));

            var dataRowContainer = table.Rows.First().ToContainer();

            bool wasCalledChangedAll = false;
            bool wasCalledChangingAll = false;
            
            dataRowContainer.FieldValueChangingEvent.Subscribe((e, c) =>
            {
                wasCalledChangingAll = wasCalledChangingAll || e.ColumnName == "Expr";

                if (wasCalledChangingAll)
                {
                    e.IsCancel = true;
                }
            });
            
            dataRowContainer.FieldValueChangedEvent.Subscribe((e, c) =>
            {
                wasCalledChangedAll = wasCalledChangedAll || e.ChangedColumnNames.Contains("Expr");
            });
            
            Assert.AreEqual("T N", dataRowContainer.Field<string>("Expr"));

            dataRowContainer["Name"] = "M";
            
            Assert.False(wasCalledChangedAll);
            
            dataRowContainer["Name"] = "B";
            
            Assert.AreEqual("T N", dataRowContainer.Field<string>("Expr"));
            Assert.AreEqual("N", dataRowContainer.Field<string>("Name"));
            
            Assert.True(wasCalledChangingAll);
            Assert.False(wasCalledChangedAll);
            
            Assert.AreEqual("T N", dataRowContainer.Field<string>("Expr"));
            Assert.AreEqual("N", dataRowContainer.Field<string>("Name"));
        }

        [Test]
        public void TestCustomFunc()
        {
            var table = GetTestTable();

            var dataRow = table.Select<DataRow>("ID = 5").First();

            dataRow[Fields.Code] = "ID = 5";

            var last = table.Rows.Last();

            last[Fields.Code] = "ID = " + int.MaxValue;

            FunctionRegistry.Registry.RegisterFunction("EVAL", (f) => new BoolEvalFunction());

            var rows = table.Select<DataRow>("EVAL([Code])");
            
            Assert.IsNotEmpty(rows);
            
            FunctionRegistry.Registry.DeregisterFunction("EVAL");

            Assert.Throws<EvaluateException>(() => table.Select<DataRow>("EVAL([Code])"));
        }
        
        private class BoolEvalFunction : Function
        {
            internal BoolEvalFunction() : 
                base(
                    name: "EVAL", 
                    result: typeof (bool), 
                    isValidateArguments: true, 
                    IsVariantArgumentList: false, 
                    argumentCount: 1, 
                    a1: typeof(string), 
                    a2: null, 
                    a3: null)
            {
            }

            protected override object EvalFunction(IExpressionDataSource expressionDataSource,
                Data<ExpressionNode> arguments, 
                object[] argumentValues, 
                int? row,
                IReadOnlyDictionary<string, object> testValues)
            {
                var expr = (string)argumentValues[0];
                
                var accessor = expressionDataSource.GetRowByHandle(row.Value);

                try
                {
                    var dataExpression = new DataExpression(new DataRowExpressionSource(accessor), expr);

                    return dataExpression.Invoke(row);
                }
                catch (Exception e)
                {
                    return false;
                }
            }
        }
    }
}