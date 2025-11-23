using System;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using CommandLine;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    
    [TestFixture(TableTestMode.Copy)]
    [TestFixture(TableTestMode.CloneMerge)]
    [TestFixture(TableTestMode.CopyMerge)]
    [TestFixture(TableTestMode.Direct)]
    [TestFixture(TableTestMode.Json)]
    [TestFixture(TableTestMode.Xml)]
    
    [TestFixture(TableTestMode.JsonDataset)]
    [TestFixture(TableTestMode.JsonDatasetMerge)]
    [TestFixture(TableTestMode.JsonDatasetSerializeDataOnly)]

    [TestFixture(TableTestMode.XmlDataset)]
    [TestFixture(TableTestMode.XmlDatasetMerge)]
    [TestFixture(TableTestMode.XmlDatasetSerializeDataOnly)]
    public class MultiColumnRelationTests
    {
        private readonly TableTestMode m_testMode;

        public MultiColumnRelationTests(TableTestMode testMode)
        {
            m_testMode = testMode;
        }
        
        DateTime m_dateTime = DateTime.Now.Date;

        [Test]
        public void TestCoreDataRowDebugView()
        {
            var ds = GetTestDataSet();

            var lastPerson = ds.GetTable("Person").Rows.Last();

            Assert.Greater(new CoreDataRowDebugView(lastPerson).Items.Length, 0);

            var lastPersonAccount = lastPerson.GetChildRows("FK_person_account").First();
            var lastPersonItems = lastPerson.GetChildRows("FK_person_items").ToData();

            Assert.Greater(new CoreDataRowDebugView(lastPersonAccount).Items.Length, 0);
            Assert.Greater(new CoreDataRowDebugView(lastPersonItems.First()).Items.Length, 0);
        }

        [Test]
        public void TestCascadeUpdateOnTransactionCommit()
        {
            var ds = GetTestDataSet();

            var lastPerson = ds.GetTable("Person").Rows.Last();
            var lastPersonAccount = lastPerson.GetChildRows("FK_person_account").First();
            var lastPersonItems = lastPerson.GetChildRows("FK_person_items").ToData();
            
            var rowEdit = lastPerson.StartTransaction();

            try
            {
                lastPerson.Set("FirstName", "Tom1");
                lastPerson.Set("LastName", "Jonson1");
                
                rowEdit.Commit();
            }
            catch
            {
                rowEdit.Rollback();
            }
            
            Assert.AreEqual("Tom1", lastPerson.Field<string>("FirstName"));
            Assert.AreEqual("Jonson1", lastPerson.Field<string>("LastName"));

            TestData(lastPerson, lastPersonAccount, lastPersonItems);

            var lastPersonAccount1 = lastPerson.GetChildRows("FK_person_account").First();
            var lastPersonItems2 = lastPerson.GetChildRows("FK_person_items").ToData();
            
            TestData(lastPerson, lastPersonAccount1, lastPersonItems2);
        }
        
        [Test]
        public void TestRollbackOnFail()
        {
            var ds = GetTestDataSet();

            var lastPerson = ds.GetTable("Person").Rows.Last();
            
            Assert.Greater(new CoreDataRowDebugView(lastPerson).Items.Length, 0);

            var accountTable = (DataTable)ds.GetTable("Account");

            var lastPersonAccount = lastPerson.GetChildRows("FK_person_account").First();
            var lastPersonItems = lastPerson.GetChildRows("FK_person_items").ToData();

            accountTable.SubscribeColumnChanging<DateTime>("Person_DOB", (e, c) =>
            {
                e.ErrorMessage = "Fail";
            });
            
            Assert.Throws<DataChangeCancelException>(() => lastPersonAccount.SetParentRow(ds.TryGetRelation("FK_person_account"), null));

            TestData(lastPerson, lastPersonAccount, lastPersonItems);
        }
        
        [Test]
        public void TestGetParentRows()
        {
            var ds = GetTestDataSet();

            var lastPerson = ds.GetTable("Person");
            
            var personObj = GetPersonObj(1, m_dateTime);

            var key = personObj.Select(c => (IComparable)c.Value).ToArray();

            var person = lastPerson.GetRowByMultiColPk(key);

            var childRow = person.GetChildRows<DataRow>("FK_person_items").First();

            var dataRows = childRow.GetParentRows("FK_person_items").ToData();

            Assert.AreEqual(person.Field<string>("FirstName"), dataRows[0].Field<string>("FirstName"));
        }

        private static void TestData(DataRow lastPerson, DataRow lastPersonAccount, Data<DataRow> lastPersonItems)
        {
            Assert.AreEqual(lastPerson["FirstName"], lastPersonAccount["Person_FirstName"]);
            Assert.AreEqual(lastPerson["LastName"], lastPersonAccount["Person_LastName"]);
            Assert.AreEqual(lastPerson["BirthPlace"], lastPersonAccount["Person_BirthPlace"]);
            Assert.AreEqual(lastPerson["DOB"], lastPersonAccount["Person_DOB"]);

            foreach (var personItem in lastPersonItems)
            {
                Assert.AreEqual(lastPerson["FirstName"], personItem["Person_FirstName"]);
                Assert.AreEqual(lastPerson["LastName"], personItem["Person_LastName"]);
                Assert.AreEqual(lastPerson["BirthPlace"], personItem["Person_BirthPlace"]);
                Assert.AreEqual(lastPerson["DOB"], personItem["Person_DOB"]);
            }
        }

        private DataTable GetTestDataSet()
        {
            var dataset = CreateTestDataset();

            switch (m_testMode)
            {
                case TableTestMode.Copy:
                    return dataset.Copy();
                case TableTestMode.Direct:
                    return dataset;
                case TableTestMode.Xml:
                case TableTestMode.XmlDataset:
                {
                    var xElement = dataset.ToXml(SerializationMode.Full);

                    var testDs = new DataTable();

                    testDs.LoadFromXml(xElement);

                    return testDs;
                }
                case TableTestMode.XmlDatasetMerge:
                {
                    var xElement = dataset.ToXml(SerializationMode.SchemaOnly);

                    var testDs = new DataTable();
                    
                    testDs.DatasetSchemaFromXElement(xElement);
                    
                    testDs.MergeData(dataset);

                    return testDs;
                }
                case TableTestMode.XmlDatasetSerializeDataOnly:
                {
                    var xElement = dataset.ToXml(SerializationMode.DataOnly);

                    var testDs = dataset.Clone();
                    
                    testDs.LoadDataFromXml(xElement);

                    return testDs;
                }
                case TableTestMode.Json:
                case TableTestMode.JsonDataset:
                {
                    var json = dataset.ToJson(SerializationMode.Full);

                    var testDs = new DataTable();

                    testDs.LoadFromJson(json);

                    return testDs;
                }
                case TableTestMode.JsonDatasetMerge:
                {
                    var json = dataset.ToJson(SerializationMode.SchemaOnly);

                    var testDs = new DataTable();
                    
                    testDs.LoadMetadataFromJson(json);
                    
                    testDs.MergeData(dataset);

                    return testDs;
                }
                case TableTestMode.JsonDatasetSerializeDataOnly:
                {
                    var json = dataset.ToJson(SerializationMode.DataOnly);

                    var testDs = dataset.Clone();
                    
                    testDs.LoadDataFromJson(json);

                    return testDs;
                }
                case TableTestMode.CloneMerge:
                {
                    var testDs = dataset.Clone();

                    testDs.FullMerge(dataset);

                    return testDs;
                }
                case TableTestMode.CopyMerge:
                {
                    var testDs = dataset.Copy();

                    foreach (var table in testDs.Tables.Where(t => t.PrimaryKey.Any() == false))
                    {
                        table.ClearRows();
                    }
                    
                    testDs.MergeData(dataset);
                    
                    testDs.AcceptChanges();

                    return testDs;
                }
            }
            
            return dataset;
        }

        private DataTable CreateTestDataset()
        {
            var person = new DataTable("Person");

            var personKey = new Data<CoreDataColumn>();

            var fn = person.AddColumn("FirstName");
            var ln = person.AddColumn("LastName");
            var bp = person.AddColumn("BirthPlace");
            var dob = person.AddColumn("DOB", TableStorageType.DateTime);
          
            personKey.Add(fn);
            personKey.Add(ln);
            personKey.Add((bp));
            personKey.Add(dob);

            person.SetPrimaryKeyColumns(personKey.Select(c => c.ColumnName).ToData());
            
            var account = new DataTable("Account");

            var accountPersonKey = new Data<CoreDataColumn>();

            var pfn = account.AddColumn("Person_FirstName");
            var pln = account.AddColumn("Person_LastName");
            var pbp = account.AddColumn("Person_BirthPlace");
            var pdob = account.AddColumn("Person_DOB", TableStorageType.DateTime);
            

            accountPersonKey.Add(pfn);
            accountPersonKey.Add(pln);
            accountPersonKey.Add(pbp);
            accountPersonKey.Add(pdob);

            account.AddColumn("amount", TableStorageType.Int32);
            
            account.SetPrimaryKeyColumns(accountPersonKey.Select(c => c.ColumnName).ToData());
            
            var personItems = new DataTable("PersonItems");

            var personItemsKey = new Data<CoreDataColumn>();

            var ifn = personItems.AddColumn("Person_FirstName");
            var iln = personItems.AddColumn("Person_LastName");
            var ibp = personItems.AddColumn("Person_BirthPlace");
            var idob = personItems.AddColumn("Person_DOB", TableStorageType.DateTime);

            personItemsKey.Add(ifn);
            personItemsKey.Add(iln);
            personItemsKey.Add(ibp);
            personItemsKey.Add(idob);

            personItems.AddColumn("item");

            var ds = new DataTable();

            ds.AddTable(person);
            ds.AddTable(account);
            ds.AddTable(personItems);

            var personAccountKey = personKey.Select((p, i) => (p, accountPersonKey[i])).ToData();
            var personInfoKey = personKey.Select((p, i) => (p, personItemsKey[i])).ToData();

            ds.AddRelation("FK_person_account", personAccountKey,
                RelationType.OneToOne,
                Rule.Cascade,
                Rule.Cascade,
                AcceptRejectRule.Cascade);

            ds.AddRelation("FK_person_items", personInfoKey,
                RelationType.OneToMany,
                Rule.Cascade,
                Rule.Cascade,
                AcceptRejectRule.Cascade);

            var personLoad = person.BeginLoad();
            var accountLoad = account.BeginLoad();
            var personItemsLoad = personItems.BeginLoad();

            for (int i = 1; i <= 10; i++)
            {
                var personObj = GetPersonObj(i, m_dateTime);

                person.AddRow(person.NewRow(personObj));

                var personObjKey = GetPersonObjKey(personObj);

                personObjKey["amount"] = i;

                account.AddRow(account.NewRow(personObjKey));

                var personObjItemKey1 = GetPersonObjKey(personObj);

                personObjItemKey1["item"] = "item " + i;

                personItems.AddRow(personItems.NewRow(personObjItemKey1));

                var personObjItemKey2 = GetPersonObjKey(personObj);

                personObjItemKey2["item"] = "item " + i * 1000;

                personItems.AddRow(personItems.NewRow(personObjItemKey2));
            }

            personItemsLoad.EndLoad();
            accountLoad.EndLoad();
            personLoad.EndLoad();

            person.AcceptChanges();
            account.AcceptChanges();
            personItems.AcceptChanges();

            ds.EnforceConstraints = true;

            return ds;
        }

        private static Map<string, object> GetPersonObj(int i, DateTime dateTime)
        {
            return _.MapObj(
                ("FirstName", "Costa" + i), 
                ("LastName", "Bru" + i),
                ("BirthPlace", "Universe" + i),
                ("DOB", dateTime.Date.AddDays(i)));
        }
        
        private static Map<string, object> GetPersonObjKey(Map<string, object> personObj)
        {
            var dict = new Map<string, object>();

            foreach (var p in personObj)
            {
                dict["Person_" + p.Key] = p.Value;
            }

            return dict;
        }
    }
}