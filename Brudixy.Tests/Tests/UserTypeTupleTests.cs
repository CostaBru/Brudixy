using System.Linq;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class UserTypeTupleTests
    {
        public UserTypeTupleTests()
        {
            DataTable.RegisterUserTypeStringMethods(((int, int) t) => SerializeHelper.SerializeWithDcs(t), (s) => SerializeHelper.DeserializeWithDcs<(int, int)>(s));
        }
        
        [Test]
        public void TestTuple()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowBySinglePk((1, 1));

            Assert.NotNull(dataRow);
        }

        [Test]
        public void TestTupleContainer()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowBySinglePk((1, 1));

            var rowContainer = dataRow.ToContainer();

            var expected = DataRowContainer.CompareDataRows(dataRow, rowContainer);
            
            Assert.True(expected.cmp == 0, expected.ToString());

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleContainerJson()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowBySinglePk((1, 1));

            var rowContainer = dataRow.ToContainer();

            var json = rowContainer.ToJson();

            var jContainer = new DataRowContainer();
            
            jContainer.FromJson(json);

            var expected = DataRowContainer.CompareDataRows(dataRow, jContainer);
            
            Assert.True(expected.cmp == 0, expected.ToString());

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleContainerXml()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowBySinglePk((1, 1));

            var rowContainer = dataRow.ToContainer();

            var xml = rowContainer.ToXml();

            var xContainer = new DataRowContainer();
            
            xContainer.FromXml(xml);

            var expected = DataRowContainer.CompareDataRows(dataRow, xContainer);
            
            Assert.True(expected.cmp == 0, expected.ToString());

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleJson()
        {
            var dataTable = CreateTupleTable();

            var js = dataTable.ToJson(SerializationMode.Full);

            var jCopy = new DataTable();
            
            jCopy.LoadFromJson(js);
            
            var dataRow = jCopy.GetRowBySinglePk((1, 1));

            Assert.NotNull(dataRow);
        }
          
        [Test]
        public void TestTupleXml()
        {
            var dataTable = CreateTupleTable();

            var xml = dataTable.ToXml(SerializationMode.Full);

            var xCopy = new DataTable();
            
            xCopy.LoadFromXml(xml);
            
            var dataRow = xCopy.GetRowBySinglePk((1, 1));

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleCopy()
        {
            var dataTable = CreateTupleTable();

            var xCopy = dataTable.Copy();
            
            var dataRow = xCopy.GetRowBySinglePk((1, 1));

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleMerge()
        {
            var dataTable = CreateTupleTable();

            var copy = new DataTable();
            
            copy.FullMerge(dataTable);
            
            var dataRow = copy.GetRowBySinglePk((1, 1));

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleKeyFind()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.Rows
                .Where("id").Equals((1, 1))
                .FirstOrDefault();

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleMissingKey()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowBySinglePk((0, 0));

            Assert.Null(dataRow);
        }
        
        [Test]
        public void TestTupleMissingKeyFind()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.Rows
                .Where("id").Equals((0, 0))
                .FirstOrDefault();

            Assert.Null(dataRow);
        }

        private static DataTable CreateTupleTable()
        {
            var dataTable = new DataTable();
            
            dataTable.AddColumn("id", typeof((int, int)));
            dataTable.SetPrimaryKeyColumn("id");

            var container = dataTable.NewRow();

            container["id"] = (1, 1);

            dataTable.AddRow(container);
            return dataTable;
        }
    }
}