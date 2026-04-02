using System.IO.Compression;
using System.Text;
using Brudixy.Persistence;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{

    [TestFixture]
    [Ignore("Uses Konsarpoo persistence that relies on BinaryFormatter, which is removed/unsupported on modern .NET runtimes.")]
    public class TestPersistence
    {
        public TestPersistence()
        {
            using var persistenceTable = GetTable();

            if (persistenceTable.RowCount == 0)
            {
                for (int i = 0; i < 1000; i++)
                {
                    persistenceTable.Rows.Add(persistenceTable.NewRow(_.MapObj(("id", i), ("name", "Test" + i))));
                }
            }
        }

        private static PersistenceTable GetTable()
        {
            var persistenceTable = new PersistenceTable();
            persistenceTable.Name = "TestData";

            var key = Encoding.UTF8.GetBytes("TestKey");
            
            // Use cross-platform temp directory instead of hardcoded Windows path
            var tempPath = Path.Combine(Path.GetTempPath(), "TestData");
            Directory.CreateDirectory(tempPath);
            persistenceTable.OpenOrCreate(tempPath, key, CompressionLevel.Fastest);

            if (persistenceTable.ColumnCount == 0)
            {
                persistenceTable.AddColumn("id", TableStorageType.Int32);
                persistenceTable.AddColumn("name", TableStorageType.String);

                persistenceTable.AddIndex("id");
            }

            return persistenceTable;
        }


        [Test]
        [Ignore("BinaryFormatter")]
        public void LoadingData()
        {
            using var persistenceTable = GetTable();

            Assert.AreEqual(1000, persistenceTable.RowCount);

            for (int i = 0; i < 1000; i++)
            {
                var dataRow = persistenceTable.GetRow("id", i);

                Assert.NotNull(dataRow);

                Assert.AreEqual(dataRow["name"], "Test" + i);
            }
        }
    }
}