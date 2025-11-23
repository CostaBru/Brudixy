using NUnit.Framework;
using Brudixy.Index;

namespace Brudixy.Tests
{
    [TestFixture]
    public class StringIndexTest
    {
        [Test]
        public void Test_Add()
        {
            var stringIndex = new StringIndex(false, false, false);

            stringIndex.Add("test", 1);

            Assert.AreEqual(1, stringIndex.Count);
        }

        [Test]
        public void Test_Search()
        {
            var stringIndex = new StringIndex(false, false, false);
            stringIndex.Add("test", 1);

            var result = stringIndex.Search("test");

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_SearchRange()
        { 
            var stringIndex = new StringIndex(false, false, false);
            stringIndex.Add("test", 1);
            stringIndex.Add("test", 2);

            var results = stringIndex.SearchRange("test").ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(1));
            Assert.IsTrue(results.Contains(2));
        }

        [Test]
        public void Test_Update()
        { 
            var stringIndex = new StringIndex(false, false, false);
            stringIndex.Add("test1", 1);
            stringIndex.Update("test2", 1, "test1");

            var result = stringIndex.Search("test2");

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_Remove()
        {  
            var stringIndex = new StringIndex(false, false, false);
            var trieIndexCount = stringIndex.Count;
            
            stringIndex.Add("test", 1);
            
            Assert.AreEqual(1, stringIndex.Search("test"));
            Assert.AreEqual(trieIndexCount + 1, stringIndex.Count);
            
            stringIndex.Remove("test", 1);

            var result = stringIndex.Search("test");

            Assert.AreEqual(-1, result);
            Assert.AreEqual(trieIndexCount, stringIndex.Count);
        }

        [Test]
        public void Test_Copy()
        {  
            var stringIndex = new StringIndex(false, false, false);
            stringIndex.Add("test", 1);
            
            var copy = stringIndex.Copy() as StringIndex;

            var result = copy.Search("test");

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_Clone()
        { 
            var stringIndex = new StringIndex(false, false, false);
            stringIndex.Add("test", 1);
            
            var clone = stringIndex.Clone() as StringIndex;

            var result = clone.Search("test");

            Assert.AreEqual(-1, result);
        }
    }
}