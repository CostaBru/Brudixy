using System;
using System.Linq;
using Brudixy.Interfaces;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class SeriealizationTests
    {
        private const int rowCount = 10;

        private const int initialCapacity = 101;

        private DataTable dataTable;

        [SetUp]
        public void Setup()
        {
            var table = new DataTable();

            table.Name = "t_nmt";

            table.AddColumn(Fields.id, TableStorageType.Int32, unique: true);
            table.AddColumn(Fields.Sn, TableStorageType.Int32, auto: true);
            table.AddColumn(Fields.Name, TableStorageType.String);
            table.AddColumn(Fields.Directories.CreateDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmDt, TableStorageType.DateTime);
            table.AddColumn(Fields.Directories.LmId, TableStorageType.Int32);
            table.AddColumn(Fields.groupid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Guid, TableStorageType.Guid);
            table.AddColumn(Fields.parentid, TableStorageType.Int32);
            table.AddColumn(Fields.Directories.Code, TableStorageType.String, columnMaxLength: 50);
            table.AddColumn(TableStorageType.DateTimeOffset.ToString(), TableStorageType.DateTimeOffset);
            table.AddColumn(TableStorageType.Decimal.ToString(), TableStorageType.Decimal);
            table.AddColumn(TableStorageType.Boolean.ToString(), TableStorageType.Boolean);
            table.AddColumn(TableStorageType.Double.ToString(), TableStorageType.Double);
            table.AddColumn(TableStorageType.Single.ToString(), TableStorageType.Single);
            table.AddColumn(TableStorageType.Byte.ToString(), TableStorageType.Byte, defaultValue: (byte)1);
            table.AddColumn(TableStorageType.TimeSpan.ToString(), TableStorageType.TimeSpan, displayName: "DURATION");
            table.AddColumn(TableStorageType.Uri.ToString(), TableStorageType.Uri);
            table.AddColumn(TableStorageType.Type.ToString(), TableStorageType.Type, readOnly: true);
            table.AddColumn("Expr1", TableStorageType.String, dataExpression: "Name + ' - ' + Code");

            table.AddIndex(Fields.Directories.Code);

            table.Capacity = initialCapacity;

            var dataEdit = table.StartTransaction();

            for (int i = rowCount; i > 0; i--)
            {
                var id = i;

                var value = new object[]
                            {
                                id, i, "Name " + id, DateTime.Now, DateTime.Now, -i, -i, Guid.NewGuid(), -id, "Code " + id % 50,
                                new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified), TimeSpan.FromHours(i % 12)),
                                (decimal)(150000.001m * (decimal)i), i % 2 == 0, (double)(0.000001d * (double)i), (float)(5.001d * (float)i), (byte)(i % 255),
                                TimeSpan.FromSeconds(i), new Uri("http://Uri" + i), typeof(int)
                            };

                table.ImportRow(RowState.Added, value);
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
        public void TestJson()
        {
          var str = @"{
  ""Name"": ""Table"",
  ""Attributes"": [
    {
      ""TableName"": ""db""
    },
    {
      ""V"": ""1.0""
    }
  ],
  ""Elements"": [
    {
      ""Name"": ""TableData"",
      ""Value"": {
        ""Name"": ""TableData"",
        ""Elements"": [
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""Session""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              },
              {
                ""Name"": ""Row"",
                ""Elements"": [
                  {
                    ""Name"": ""Id"",
                    ""Value"": ""134085188562391953""
                  },
                  {
                    ""Name"": ""Name"",
                    ""Value"": ""New Visit""
                  },
                  {
                    ""Name"": ""Age"",
                    ""Value"": """"
                  },
                  {
                    ""Name"": ""ClientId"",
                    ""Value"": """"
                  },
                  {
                    ""Name"": ""Year"",
                    ""Value"": """"
                  },
                  {
                    ""Name"": ""SessionType"",
                    ""Value"": ""GeneralQuestion""
                  },
                  {
                    ""Name"": ""UtcTimeStamp"",
                    ""Value"": ""2025-11-25T04:34:16.2391953Z""
                  },
                  {
                    ""Name"": ""Protected"",
                    ""Value"": ""false""
                  }
                ]
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""Conversations""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""LogItem""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""Documents""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""AnalysisResultItems""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""AnalysisResults""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""RecommendationItems""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          },
          {
            ""Name"": ""Table"",
            ""Attributes"": [
              {
                ""TableName"": ""Recommendations""
              },
              {
                ""V"": ""1.0""
              }
            ],
            ""Elements"": [
              {
                ""Name"": ""TableData"",
                ""Value"": {
                  ""Name"": ""TableData""
                }
              }
            ]
          }
        ]
      }
    }
  ]
}";
          var jElement = JElement.Parse(str);
        }


        [Test]
        public void Test2()
        {
            var table = dataTable.Copy();

            SetTestColumnExtProp(table);

            SetRowExtProperties(table);

            SetTableExtProperties(table);

            var element = table.ToXml();

            Assert.NotNull(element);

            var dataRows = table.AllRows.ToArray();
            var xElements = element.Elements().ToArray();

            Assert.AreEqual(dataRows.Length, xElements.Length);

            var columns = table.GetColumns().ToArray();

            for (int i = 0; i < dataRows.Length; i++)
            {
                var dataRow = dataRows[i];
                var xElement = xElements[i];

                foreach (var column in columns)
                {
                    var value = dataRow.Field<object>(column.ColumnName, null);

                    var serializedValue = xElement.Element(column.ColumnName).Value;

                    Assert.True(table.EqualSerializedValues(column, serializedValue, value), column.ColumnName.ToString() + " " + i);
                }
            }
        }


        private static void SetTableExtProperties(DataTable table)
        {
            table.SetXProperty("Width", "555");
            table.SetXProperty("Height", "333");

            foreach (var s in Enumerable.Range(1, 99).Select(i => "TestP" + i))
            {
                table.SetXProperty(s, s);
            }
        }

        private static void SetRowExtProperties(DataTable table)
        {
            var row = table.GetRowBy(1);

            row.SetXProperty("Width", "555");
            row.SetXProperty("Height", "333");

            var row1 = table.GetRowBy(2);

            row1.SetXProperty("Flag", "1");

            var row2 = table.GetRowBy(3);

            foreach (var s in Enumerable.Range(1, 99).Select(i => "TestP" + i))
            {
                row2.SetXProperty(s, s);
            }
        }

        public enum SMode
        {
            Xml,
            Json
        }

        [Test]
        public void TestColXProps([Values(SMode.Xml, SMode.Json)] SMode mode)
        {
            var table = dataTable.Copy();

            SetTestColumnExtProp(table);
            
            TestColumnExtProp(table);

            var test = new DataTable();

            if (mode == SMode.Xml)
            {
                var xElement = table.ToXml(SerializationMode.Full);
                
                test.LoadFromXml(xElement);
            }
            if (mode == SMode.Json)
            {
                var json = table.ToJson(SerializationMode.Full);
                
                test.LoadFromJson(json);
            }
            
            TestColumnExtProp(test);
        }

        [Test]
        public void TestVersions([Values(SMode.Xml, SMode.Json)] SMode mode)
        {
            if (mode == SMode.Xml)
            {
                var xElement = dataTable.ToXml(SerializationMode.Full);

                var version = Version.Parse(xElement.Attribute("V").Value);

                xElement.Attribute("V").Value = new Version(version.Major + 1, 1).ToString();

                Assert.Throws<NotSupportedException>(() => { new DataTable().LoadFromXml(xElement); });

                xElement.Attribute("V").Value = string.Empty;

                Assert.Throws<ArgumentException>(() => { new DataTable().LoadFromXml(xElement); });
                
                xElement.Attribute("V").Value = Guid.NewGuid().ToString();

                Assert.Throws<FormatException>(() => { new DataTable().LoadFromXml(xElement); });

                xElement.Attribute("V").Value = new Version(version.Major, version.Minor + 1).ToString();

                Assert.DoesNotThrow(() => new DataTable().LoadFromXml(xElement));
            }

            if (mode == SMode.Json)
            {
                var json = dataTable.ToJson(SerializationMode.Full);

                var version = Version.Parse(json.GetAttribute("V").ToString());

                json.SetAttribute("V",  new Version(version.Major + 1, 1).ToString());

                Assert.Throws<NotSupportedException>(() => { new DataTable().LoadFromJson(json); });

                json.SetAttribute("V", string.Empty);

                Assert.Throws<ArgumentException>(() => { new DataTable().LoadFromJson(json); });
    
                json.SetAttribute("V", Guid.NewGuid().ToString());

                Assert.Throws<FormatException>(() => { new DataTable().LoadFromJson(json); });

                json.SetAttribute("V",  new Version(version.Major, version.Minor + 1).ToString());

                Assert.DoesNotThrow(() => new DataTable().LoadFromJson(json));
            }
        }

        [Test]
        public void Test3()
        {
            var table = dataTable.Copy();

            SetTestColumnExtProp(table);

            SetRowExtProperties(table);

            SetTableExtProperties(table);

            var element = table.ToXml(SerializationMode.DataOnly);

            var toLoad = table.Clone();

            Assert.AreEqual(0, toLoad.RowCount);

            toLoad.LoadDataFromXml(element);

            var dataRows = toLoad.Rows.ToArray();
            var xElements = element.Elements().ToArray();
            
            Assert.AreEqual(dataRows.Length , xElements.Length);

            var columns = toLoad.GetColumns().ToArray();

            for (int i = 0; i < dataRows.Length; i++)
            {
                var dataRow = dataRows[i];
                var xElement = xElements[i];

                foreach (var column in columns)
                {
                    var value = dataRow.Field<object>(column.ColumnName, null);

                    var serializedValue = xElement.Element(column.ColumnName).Value;

                    if (column.ColumnName == "Double")
                    {
                    }

                    var equalSerializedValues = toLoad.EqualSerializedValues(column, serializedValue, value);

                    if (equalSerializedValues == false)
                    {
                    }
                    
                    Assert.True(equalSerializedValues, $"{column.ColumnName} {serializedValue} != {value}!");
                }
            }
        }

        private static void SetTestColumnExtProp(DataTable table)
        {
            var nameColumn = table.GetColumn(Fields.Name);
            
            nameColumn.SetXProperty("TestProperty", 123);
            
            var idColumn = table.GetColumn(Fields.id);
            
            idColumn.SetXProperty("Qwerty", true);

            var groupIdColumn = table.GetColumn(Fields.groupid);

            foreach (var s in Enumerable.Range(1, 99))
            {
                groupIdColumn.SetXProperty("TestP" + s, s);
            }
        }
        
        private static void TestColumnExtProp(DataTable table)
        {
            Assert.AreEqual(123, table.GetColumn(Fields.Name).GetXProperty<int>("TestProperty"));
            Assert.AreEqual(true,  table.GetColumn(Fields.id).GetXProperty<bool>("Qwerty"));

            var groupIdColumn = table.GetColumn(Fields.groupid);

            foreach (var s in Enumerable.Range(1, 99))
            {
                Assert.AreEqual(s, groupIdColumn.GetXProperty<int>("TestP" + s));
            }
        }

        [Test]
        public void Test5()
        {
            var table = dataTable;

            var element = table.ToXml(SerializationMode.Full);

            var fromXml = new DataTable();

            fromXml.LoadFromXml(element);

            var equalsExt = table.EqualsExt(fromXml);

            Assert.True(equalsExt.value, equalsExt.name + "." + equalsExt.type);

            Assert.AreEqual(table.RowCount, fromXml.RowCount);
            Assert.AreEqual(table.ColumnCount, fromXml.ColumnCount);

            foreach (var column in table.GetColumns())
            {
                var xmlCol = fromXml.GetColumn(column.ColumnName);

                Assert.AreEqual(column.Type, xmlCol.Type);
                Assert.AreEqual(column.MaxLength, xmlCol.MaxLength);
                Assert.AreEqual(column.IsUnique, xmlCol.IsUnique);
                Assert.AreEqual(column.Caption, xmlCol.Caption);
                Assert.AreEqual(column.IsAutomaticValue, xmlCol.IsAutomaticValue);
                Assert.AreEqual(column.FixType, xmlCol.FixType);
                Assert.AreEqual(column.Expression, xmlCol.Expression);
                Assert.AreEqual(column.IsReadOnly, xmlCol.IsReadOnly);
                Assert.AreEqual(column.DefaultValue, xmlCol.DefaultValue);

                TestColumnExtProperties(column, xmlCol);
            }

            var tableRows = table.Rows.ToArray();

            var xmlTableRows = fromXml.Rows.ToArray();

            for (int i = 0; i < xmlTableRows.Length; i++)
            {
                var xmlTableRow = xmlTableRows[i];
                var tableRow = tableRows[i];

                foreach (var column in table.GetColumns())
                {
                    var dataColumn = fromXml.GetColumn(column.ColumnName);

                    if (column.Type == TableStorageType.Double)
                    {
                        Assert.AreEqual((double)tableRow[column.ColumnName], (double)xmlTableRow[dataColumn.ColumnName], 0.0000000000000001d);
                    }
                    else if (column.Type == TableStorageType.Single)
                    {
                        Assert.AreEqual((float)tableRow[column.ColumnName], (float)xmlTableRow[dataColumn.ColumnName], 0.0000000000000001f);
                    }
                    else
                    {
                        Assert.AreEqual(tableRow[column.ColumnName], xmlTableRow[dataColumn.ColumnName]);
                    }
                }

                TestRowExtendedProperties(tableRow, xmlTableRow);
            }

            TestTableExtendedProperties(table, fromXml);
        }

        private static void TestColumnExtProperties(DataColumn lwDataColumn, DataColumn dataColumn)
        {
            var extProperties = lwDataColumn.XProperties.ToArray();

            var extProperiesFromXml = dataColumn.XProperties.ToArray();

            Assert.AreEqual(extProperties.Length, extProperiesFromXml.Length);

            if (extProperties.Length > 0)
            {
                Assert.True(extProperties.SequenceEqual(extProperiesFromXml));
            }

            foreach (var extProperty in extProperties)
            {
                var valueFromXml = dataColumn.GetXProperty<string>(extProperty);
                var value = lwDataColumn.GetXProperty<string>(extProperty);

                Assert.AreEqual(value, valueFromXml);
            }
        }

        private static void TestRowExtendedProperties(DataRow tableRow, DataRow xmlTableRow)
        {
            var extProperties = tableRow.GetXProperties().ToArray();

            var extProperiesFromXml = xmlTableRow.GetXProperties().ToArray();

            Assert.AreEqual(extProperties.Length, extProperiesFromXml.Length);

            if (extProperties.Length > 0)
            {
                Assert.True(extProperties.SequenceEqual(extProperiesFromXml));
            }

            foreach (var extProperty in extProperties)
            {
                var valueFromXml = xmlTableRow.GetXProperty<string>(extProperty);
                var value = tableRow.GetXProperty<string>(extProperty);

                Assert.AreEqual(value, valueFromXml);
            }
        }

        private static void TestTableExtendedProperties(DataTable table, DataTable xmlTable)
        {
            var extProperties = table.XProperties.ToArray();

            var extProperiesFromXml = xmlTable.XProperties.ToArray();

            Assert.AreEqual(extProperties.Length, extProperiesFromXml.Length);

            if (extProperties.Length > 0)
            {
                Assert.True(extProperties.SequenceEqual(extProperiesFromXml));
            }

            foreach (var extProperty in extProperties)
            {
                var valueFromXml = xmlTable.GetXProperty<string>(extProperty);
                var value = table.GetXProperty<string>(extProperty);

                Assert.AreEqual(value, valueFromXml);
            }
        }
    }
}
