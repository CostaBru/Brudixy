using System;
using System.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class SelectRowsApiTest
    {
        [Test]
        public void TestNullIndexes()
        {
            var table = new DataTable();

            table.AddColumn("id", TableStorageType.Int32, unique: true);
            table.AddColumn("name", TableStorageType.String);
            table.AddColumn("parentid", TableStorageType.Int32);

            var beginEdit = table.BeginLoad();

            for (int i = 1; i < 51; i++)
            {
                table.ImportRow(table.NewRow().Set("id", i).Set("name", "Name " + i));
            }

            beginEdit.EndLoad();

            for (int i = 50; i > 2; i--)
            {
                var dataRow = table.GetRowBy(i);

                dataRow["parentid"] = i - 1;
            }

            var dataRows = table.GetRowsWhereNull("parentid").ToData();

            Assert.Greater(dataRows.Length, 0);

            table.AddIndex("parentid");

            var rowsSelectedByIndex = table.GetRowsWhereNull("parentid").ToData();

            Assert.Greater(rowsSelectedByIndex.Length, 0);
        }
        
        [Test]
        public void TestAllTypes()
        {
            var table1 = new DataTable();


            var values = Enum.GetValues(typeof(TableStorageType))
                .OfType<TableStorageType>()
                .Where(c => c != TableStorageType.UserType)
                .ToArray();

            var data = new Data<object>();

            for (int j = 0; j < 1; j++)
            {
                bool allowNull = false;

                foreach (var modifier in Enum.GetValues<TableStorageTypeModifier>())
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        var type = (TableStorageType)values.GetValue(i);

                        if (type == TableStorageType.Empty || type == TableStorageType.Object || type == TableStorageType.UserType)
                        {
                            continue;
                        }

                        if (modifier == TableStorageTypeModifier.Range)
                        {
                            if ((int)type > (int)TableStorageType.BigInteger)
                            {
                                continue;
                            }
                        }

                        if (modifier == TableStorageTypeModifier.Complex)
                        {
                            continue;
                        }

                        table1.AddColumn(type + "." + modifier + '.' + allowNull, type, modifier, allowNull: allowNull);

                        var defaultNotNull = DataTable.GetDefaultNotNull(type, modifier);

                        data.Add(defaultNotNull);
                    }
                }
            }

            var loadState = table1.BeginLoad();
            var objects = data.ToArray();

            for (int i = 0; i < 5; i++)
            {
                table1.ImportRowDirty(RowState.Added, objects);
            }

            loadState.EndLoad();

            var clone = table1.Clone();
            var copy = table1.Copy();

            var xElement = table1.ToXml(SerializationMode.Full);

            var s = xElement.ToString();

            var itemArray = DataRow.GetItemArray(copy.Rows.First());

            copy.Dispose();
            clone.Dispose();
        }
        
        [Test]
        public void TestRemoveFromBitArray()
        {
            var bitArray1 = new BitArr(10, false);

            bitArray1.Set(0, true);
            bitArray1.Set(9, true);
            bitArray1.Set(5, true);

            CoreDataColumnInfo.RemoveForBitArray(0, ref bitArray1);

            Assert.False(bitArray1.Get(0));
            Assert.False(bitArray1.Get(1));

            Assert.False(bitArray1.Get(9));
            Assert.False(bitArray1.Get(5));

            Assert.True(bitArray1.Get(5 - 1));
            Assert.True(bitArray1.Get(9 - 1));

            CoreDataColumnInfo.RemoveForBitArray(9 - 1, ref bitArray1);

            Assert.False(bitArray1.Get(0));
            Assert.False(bitArray1.Get(1));

            Assert.False(bitArray1.Get(9));
            Assert.False(bitArray1.Get(5));

            Assert.True(bitArray1.Get(5 - 1));

            Assert.False(bitArray1.Get(9 - 1));

            var bitArray2 = new BitArr(10, false);

            bitArray2.Set(0, true);
            bitArray2.Set(9, true);
            bitArray2.Set(5, true);

            CoreDataColumnInfo.RemoveForBitArray(0, ref bitArray2);
            CoreDataColumnInfo.RemoveForBitArray(9 - 1, ref bitArray2);
            CoreDataColumnInfo.RemoveForBitArray(5 - 1, ref bitArray2);

            for (int i = 0; i < bitArray2.Length; i++)
            {
                Assert.False(bitArray2.Get(i));
            }
        }
    }
}