using System.Linq;
using Brudixy.Interfaces;
using Brudixy.Tests.TypedDs.UserTypes;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{

    public class TestUserTypeClassTypedTable
    {
        public TestUserTypeClassTypedTable()
        {
            DataTable.RegisterUserTypeStringMethods((UserClass t) => SerializeHelper.SerializeWithDcs(t),
                (s) => SerializeHelper.DeserializeWithDcs<UserClass>(s));
        }

        [Test]
        public void TestTupleGetKey()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowByPk(new (1, 1));

            Assert.NotNull(dataRow);
        }

        [Test]
        public void TestTupleContainer()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowByPk(new (1, 1));

            var rowContainer = dataRow.ToContainer();

            var expected = DataRowContainer.CompareDataRows(dataRow, rowContainer);

            Assert.True(expected.cmp == 0, expected.ToString());

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleKeyFind()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.Rows
                .Where("id").Equals(new UserClass(1, 1))
                .FirstOrDefault();

            Assert.NotNull(dataRow);
        }
        
        [Test]
        public void TestTupleMissingKey()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowByPk(new (0, 0));

            Assert.Null(dataRow);
        }
        
        [Test]
        public void TestTupleMissingKeyFind()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.Rows
                .Where("id").Equals(new UserClass(0, 0))
                .FirstOrDefault();

            Assert.Null(dataRow);
        }

        [Test]
        public void TestTupleContainerJson()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowByPk(new (1, 1));

            var rowContainer = dataRow.ToContainer();

            var json = rowContainer.ToJson();

            var jContainer = new TestClassTableRowContainer();

            jContainer.FromJson(json);

            var expected = DataRowContainer.CompareDataRows(dataRow, jContainer);

            Assert.True(expected.cmp == 0, expected.ToString());

            Assert.NotNull(dataRow);
        }

        [Test]
        public void TestTupleContainerXml()
        {
            var dataTable = CreateTupleTable();

            var dataRow = dataTable.GetRowByPk(new (1, 1));

            var rowContainer = dataRow.ToContainer();

            var xml = rowContainer.ToXml();

            var xContainer = new TestClassTableRowContainer();

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

            var jCopy = new TestClassTable();

            jCopy.LoadFromJson(js);

            var dataRow = jCopy.GetRowByPk(new (1, 1));

            Assert.NotNull(dataRow);
        }

        [Test]
        public void TestTupleXml()
        {
            var dataTable = CreateTupleTable();

            var xml = dataTable.ToXml(SerializationMode.Full);

            var xCopy = new TestClassTable();

            xCopy.LoadFromXml(xml);

            var dataRow = xCopy.GetRowByPk(new (1, 1));

            Assert.NotNull(dataRow);
        }

        [Test]
        public void TestTupleCopy()
        {
            var dataTable = CreateTupleTable();

            var xCopy = dataTable.Copy();

            var dataRow = xCopy.GetRowByPk(new (1, 1));

            Assert.NotNull(dataRow);
        }

        [Test]
        public void TestTupleMerge()
        {
            var dataTable = CreateTupleTable();

            var copy = new TestClassTable();

            copy.FullMerge(dataTable);

            var dataRow = copy.GetRowByPk(new (1, 1));

            Assert.NotNull(dataRow);
        }

        private static TestClassTable CreateTupleTable()
        {
            var dataTable = new TestClassTable();

            var container = dataTable.NewRow();

            container.id = new UserClass(1, 1);

            dataTable.AddRow(container);

            return dataTable;
        }
    }
}