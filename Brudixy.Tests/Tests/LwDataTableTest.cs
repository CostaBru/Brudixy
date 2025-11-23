using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Brudixy.Interfaces;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class LwDataTableTest
    {
        private string m_connectionString = "Integrated Security=True;database=kontur_dev;server=8D062607H;Timeout=60;";

        private SqlConnection m_sqlConnect;

        [SetUp]
        public void Setup()
        {
            m_sqlConnect = new SqlConnection(m_connectionString);

        }

        [TearDown]
        public void TearDown()
        {
            m_sqlConnect.Dispose();
        }

        [Test]
        [Ignore("")]
        public void TestLoadIndexAfter()
        {
            var sqlCommand = new SqlCommand
            {
                CommandTimeout = 0, //пусть берется настройка с сервера
                Connection = m_sqlConnect
            };

            sqlCommand.CommandText = "select top 1 * from v_nmt where isdeleted is null";

            var table1 = new DataTable();

            SqlMapper.FillDataTable<DataTable>(m_sqlConnect, table1, sqlCommand);

            table1.AddIndex("id", unique: true);

            var row = table1.GetRowBy(555);
        }

        [Test]
        [Ignore("")]
        public void TestAllTypesMemory()
        {
            var dataTable = new DataTable();
            var sysDataTable = new System.Data.DataTable();

            var data = new List<object>();

            foreach (var modifier in Enum.GetValues<TableStorageTypeModifier>())
            {
                foreach (var type in Enum.GetValues<TableStorageType>())
                {
                    var columnType = DataTable.GetColumnType(type, modifier, false, null);

                    if (columnType == null)
                    {
                        continue;
                    }
                    
                    var columnName = type + "." + modifier;
                    
                    dataTable.AddColumn(columnName, type, modifier);

                    data.Add(DataTable.GetDefaultNotNull(type, modifier));
                    
                    sysDataTable.Columns.Add(columnName, columnType);
                }
            }

            dataTable.Capacity = 100000;
            sysDataTable.MinimumCapacity = 100000;

            sysDataTable.BeginInit();
            var loadState = dataTable.BeginLoad();
            
            for (int i = 0; i < 100000; i++)
            {
                dataTable.ImportRow(RowState.Added, data);
                sysDataTable.Rows.Add(data);
            }

            sysDataTable.EndInit();
            loadState.EndLoad();

         
        }


        [Test]
        [Ignore("")]
        public void TestFill()
        {
            var sqlCommand = new SqlCommand
            {
                CommandTimeout = 0, //пусть берется настройка с сервера
                Connection = m_sqlConnect
            };

            sqlCommand.CommandText = "select  top 100 * from v_nmt where isdeleted is null";

            var table = new DataTable();

            table.AddColumn("id", TableStorageType.Int32, unique: true);
            table.AddColumn("code", TableStorageType.String);
            table.AddColumn("name", TableStorageType.String);
            table.AddColumn("expr1", TableStorageType.String, dataExpression: "code + ' - ' + name");
            table.AddColumn("sn", TableStorageType.Int32);
            table.AddColumn("guid", TableStorageType.Guid);
            table.AddColumn("createdt", TableStorageType.DateTime);
            table.AddColumn("lmdt", TableStorageType.DateTime);
            table.AddColumn("lmid", TableStorageType.Int32);
            table.AddColumn("creatorid", TableStorageType.Int32);

            var dataEdit = table.StartTransaction();

            table.ImportRow(table.NewRow().Set("id", -1).Set("code", "-1").Set("name", "-1").Set("sn", -1).Set("giud", new Guid()).Set("createdt", DateTime.Now).Set("lmdt", DateTime.Now).Set("lmid", -1).Set("creatorid", -1));
            table.ImportRow(table.NewRow().Set("id", -2).Set("code", "-2").Set("name", "-2").Set("sn", -2).Set("giud", new Guid()).Set("createdt", DateTime.Now).Set("lmdt", DateTime.Now).Set("lmid", -2).Set("creatorid", -2));

            dataEdit.Commit();

            SqlMapper.FillDataTable<DataTable>(m_sqlConnect, table, sqlCommand);

            var row1 = table.GetRowBy(-1);
            var row2 = table.GetRowBy(-2);

            Assert.NotNull(row1);
            Assert.NotNull(row2);

            Assert.AreEqual(RowState.Added, row1.RowRecordState);
            Assert.AreEqual(RowState.Added, row2.RowRecordState);
        }
    }
}
