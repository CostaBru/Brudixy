using System;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    class DataRelationTest
    {
        private static DataTable CreateDataSet()
        {
            var dataSet = new DataTable();
            var parents = new DataTable("Parents");
            var kids = new DataTable("Child");

            dataSet.AddTable(parents);
            dataSet.AddTable(kids);

            parents.AddColumn("Name", TableStorageType.String);
            parents.AddColumn("ChildName", TableStorageType.String);

            kids.AddColumn("Name", TableStorageType.String);
            kids.AddColumn("Age", TableStorageType.Int16);

            parents.AddIndex("Name");
            kids.AddIndex("Name");
            return dataSet;
        }

        [Test]
        public void InvalidConstraintException()
        {
            var dataSet = CreateDataSet();

            var parents = dataSet.GetTable("Parents");
            var kids = dataSet.GetTable("Child");
            
            Assert.Throws<InvalidOperationException>(() =>
            {
                // Parent Columns and Child Columns don't have type-matching columns.
                var dataRelation = new DataRelation((string)"Rel", parents.GetColumn("ChildName"), kids.GetColumn("Age"));
                dataSet.AddRelation(dataRelation);
            });
        }

        [Test]
        public void Foreign()
        {
            var dataSet = CreateDataSet();

            dataSet.EnforceConstraints = true;

            var kids = dataSet.GetTable("Child");
            var parents = dataSet.GetTable("Parents");
            
            dataSet.AddRelation("Rel", 
                kids.GetColumn("Name"),
                parents.GetColumn("ChildName"),
                RelationType.OneToMany,
                Rule.Cascade, 
                Rule.Cascade, 
                AcceptRejectRule.Cascade);
            
            var JackRow = kids
                .AddRow(kids.NewRow()
                    .Set("Name", "Jack")
                    .Set("Age", 16));
            
            var DickRow = kids
                .AddRow(kids.NewRow()
                    .Set("Name", "Dick")
                    .Set("Age", 56));
            
            
            var TeresaRow = parents
                .AddRow(parents.NewRow()
                    .Set("Name", "Teresa")
                    .Set("ChildName", "Jack"));
            
            var JohnRow = parents
                .AddRow(parents.NewRow()
                    .Set("Name", "John")
                    .Set("ChildName", "Jack"));
            
            var MaryRow = parents
                .AddRow(parents.NewRow()
                    .Set("Name", "Mary")
                    .Set("ChildName", "Dick"));

            Assert.AreEqual(2, kids.RowCount);

            kids.AcceptChanges();
            
            JackRow.Delete();

            Assert.AreEqual(1, parents.RowCount);
            
            Assert.Throws<Brudixy.Constraints.ConstraintException>(() =>
            {
                var fakeParent = parents
                    .AddRow(parents.NewRow()
                        .Set("Name", "Teresa")
                        .Set("ChildName", Guid.NewGuid().ToString()));
            });

            Assert.Throws<Brudixy.Constraints.ConstraintException>(() =>
            {
                var fakeParent = parents
                    .AddRow(parents.NewRow()
                        .Set("Name", "Teresa")
                        .Set("ChildName", "Jack"));
            });
          
            var MaganRow = parents
                .AddRow(parents.NewRow()
                    .Set("Name", "Magan")
                    .Set("ChildName", "Dick"));

            Assert.AreEqual(1, kids.RowCount);
        }

        [Test]
        public void DataSetRelations()
        {
            var dataSet = CreateDataSet();
            
            var parents = dataSet.GetTable("Parents");
            var kids = dataSet.GetTable("Child");

            DataRelation Relation;
            
            Assert.AreEqual(0, dataSet.Relations.Count());
            Assert.AreEqual(0, parents.ParentRelations.Count());
            Assert.AreEqual(0, parents.ChildRelations.Count());
            Assert.AreEqual(0, kids.ParentRelations.Count());
            Assert.AreEqual(0, kids.ChildRelations.Count());

            Relation = new DataRelation("Rel", parents.GetColumn("ChildName"), kids.GetColumn("Name"));
            dataSet.AddRelation(Relation);

            Assert.AreEqual(1, dataSet.Relations.Count());
            Assert.AreEqual(0, parents.ParentRelations.Count());
            Assert.AreEqual(1, parents.ChildRelations.Count());
            Assert.AreEqual(1, kids.ParentRelations.Count());
            Assert.AreEqual(0, kids.ChildRelations.Count());

            Relation = (DataRelation) dataSet.Relations.First();
            Assert.AreEqual(1, Relation.ParentColumnsCount);
            Assert.AreEqual(1, Relation.ChildColumnsCount);
           // Assert.AreEqual("Rel", Relation.ChildKeyConstraint.ConstraintName);
           // Assert.AreEqual("Constraint1", Relation.ParentKeyConstraint.ConstraintName);
        }

        [Test]
        public void ChildRows()
        {
            var dataSet = CreateDataSet();

            var parents = dataSet.GetTable("Parents");
            var kids = dataSet.GetTable("Child");
            
            dataSet.AddRelation("Rel" ,parents.GetColumn("ChildName"), kids.GetColumn("Name"));

            var teresaRow = parents.NewRow();
            teresaRow["Name"] = "Teresa";
            teresaRow["ChildName"] = "John";
            parents.AddRow(teresaRow);

            var meganRow = parents.NewRow();
            meganRow["Name"] = "Megan";
            meganRow["ChildName"] = "Dick";
            parents.AddRow(meganRow);

            var johnRow = kids.NewRow();
            johnRow["Name"] = "John";
            johnRow["Age"] = "15";
            kids.AddRow(johnRow);

            var dickRow = kids.NewRow();
            dickRow["Name"] = "Dick";
            dickRow["Age"] = "10";
            kids.AddRow(dickRow);

            var first = parents.GetRowBy("Megan");
            var row = first.GetChildRows("Rel").First();
            Assert.AreEqual("Dick", row["Name"]);
            Assert.AreEqual("10", row["Age"].ToString());

            row = row.GetParentRow("Rel");
            Assert.AreEqual("Megan", row["Name"]);
            Assert.AreEqual("Dick", row["ChildName"]);

            first = kids.GetRowBy("John");
            row = first.GetParentRows("Rel").First();
            Assert.AreEqual("Teresa", row["Name"]);
            Assert.AreEqual("John", row["ChildName"]);
        }
    }
}
