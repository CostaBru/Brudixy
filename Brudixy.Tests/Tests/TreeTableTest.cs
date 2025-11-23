using System;
using Brudixy.Interfaces;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture(TableTestMode.Copy)]
    [TestFixture(TableTestMode.CloneMerge)]
    [TestFixture(TableTestMode.Direct)]
    [TestFixture(TableTestMode.Json)]
    [TestFixture(TableTestMode.Xml)]
    [TestFixture(TableTestMode.XmlSchemaMerge)]
    [TestFixture(TableTestMode.JsonSchemaMerge)]
    
    [TestFixture(TableTestMode.JsonDataset)]
    [TestFixture(TableTestMode.JsonDatasetMerge)]
    [TestFixture(TableTestMode.JsonDatasetSerializeDataOnly)]

    [TestFixture(TableTestMode.XmlDataset)]
    [TestFixture(TableTestMode.XmlDatasetMerge)]
    [TestFixture(TableTestMode.XmlDatasetSerializeDataOnly)]
    public class TreeTableTest
    {
        private readonly TableTestMode m_testMode;

        public TreeTableTest(TableTestMode testMode)
        {
            m_testMode = testMode;
        }
        
        public DataTable GetTestTable(Rule rule, AcceptRejectRule acceptRejectRule)
        {
            var treeTable = GetTreeTable(rule, acceptRejectRule);
            
            Func<string, DataTable> createNewTableFunc = (string tableName) =>
            {
                return GetTreeTable(rule, acceptRejectRule);
            };

            return CommonTest.GetTestTable(m_testMode, treeTable, createNewTableFunc);
        }
        
        private static DataTable GetTreeTable(Rule rule, AcceptRejectRule acceptRejectRule)
        {
            var tree = new DataTable();

            tree.EnforceConstraints = true;

            tree.AddColumn("id", TableStorageType.Int32, auto: true);
            tree.AddColumn("parentid", TableStorageType.Int32);

            tree.SetPrimaryKeyColumn("id");
            
            tree.AddIndex("parentid");

            tree.AddNestedRelation("tree", "id", "parentid", rule, rule, acceptRejectRule);

            var dataEdit = tree.StartTransaction();

            var r = tree.NewRow();

            var o = r["parentid"];

            var root = tree.AddRow(r);
            var n1 = tree.AddRow(tree.NewRow());
                
            n1.SetField("parentid", root["id"]);
            
            var n2 = tree.AddRow(tree.NewRow()).SetField("parentid", root["id"]);

            var n1t = tree.AddRow(tree.NewRow()).SetField("parentid", n1["id"]);
            var n1z = tree.AddRow(tree.NewRow()).SetField("parentid", n1["id"]);

            var n2t = tree.AddRow(tree.NewRow()).SetField("parentid", n2["id"]);
            var n2z = tree.AddRow(tree.NewRow()).SetField("parentid", n2["id"]);
            
            dataEdit.Commit();
            
            var kids = root.GetChildRows("tree");

            return tree;
        }
        
        [Test]
        public void TestNestedCascadeFKDelete()
        {
            var tree = GetTestTable(Rule.Cascade, AcceptRejectRule.Cascade);
            
            tree.AcceptChanges();

            var root = tree.GetRowBy(1);

            var kids = root.GetChildRows("tree").ToData();

            Assert.AreEqual(2, kids.Count);
            
            root.Delete();
            
            tree.AcceptChanges();

            Assert.AreEqual(0, tree.RowCount);
        }
    }
}