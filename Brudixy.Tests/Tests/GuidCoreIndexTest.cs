using System;
using NUnit.Framework;
using System.Linq;
using Brudixy.Index;

namespace Brudixy.Tests
{
    [TestFixture]
    public class GuidHashIndexTest
    {
        [Test]
        public void Test_Add()
        {
            var guidIndex = new CoreStructHashIndex<Guid>(false);

            guidIndex.Add(Guid.NewGuid(), 1);

            Assert.AreEqual(1, guidIndex.Count);
        }
        
        [Test]
        public void Test_Add_Comp()
        {
            var guidIndex = new CoreStructHashIndex<Guid>(false);

            guidIndex.Add((IComparable)Guid.NewGuid(), 1);

            Assert.AreEqual(1, guidIndex.Count);
        }

        [Test]
        public void Test_Search()
        {
            var test = Guid.NewGuid();

            var guidIndex = new CoreStructHashIndex<Guid>(false);
            guidIndex.Add(test, 1);

            Assert.AreEqual(1, guidIndex.Search(test));
            Assert.AreEqual(1, guidIndex.Search((IComparable)test));
            Assert.AreEqual(-1, guidIndex.Search(Guid.NewGuid()));
            Assert.AreEqual(-1, guidIndex.Search((IComparable)Guid.NewGuid()));
        }

        [Test]
        public void Test_SearchRange()
        { 
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>(false);
            guidIndex.Add(test, 1);
            guidIndex.Add(test, 2);

            var results = guidIndex.SearchRange(test).ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(1));
            Assert.IsTrue(results.Contains(2));
            
            results = guidIndex.SearchRange((IComparable)test).ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(1));
            Assert.IsTrue(results.Contains(2));
        }

        [Test]
        public void Test_Update()
        { 
            var test1 = Guid.NewGuid();
            var test2 = Guid.NewGuid();
            
            var guidIndex = new CoreStructHashIndex<Guid>( false);
            guidIndex.Add(test1, 1);
            guidIndex.Update(test2, 1, test1);

            var result = guidIndex.Search(test2);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_Remove()
        {  
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>( false);
            var guidIndexCount = guidIndex.Count;
            
            guidIndex.Add(test, 1);
            
            Assert.AreEqual(1, guidIndex.Search(test));
            Assert.AreEqual(guidIndexCount + 1, guidIndex.Count);
            
            guidIndex.Remove(test, 1);

            var result = guidIndex.Search(test);

            Assert.AreEqual(-1, result);
            Assert.AreEqual(guidIndexCount, guidIndex.Count);
        }

        [Test]
        public void Test_Copy()
        {  
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>( false);
            guidIndex.Add(test, 1);
            
            var copy = guidIndex.Copy() as CoreStructHashIndex<Guid>;

            var result = copy.Search(test);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_Clone()
        { 
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>( false);
            guidIndex.Add(test, 1);
            
            var clone = guidIndex.Clone() as CoreStructHashIndex<Guid>;

            var result = clone.Search(test);

            Assert.AreEqual(-1, result);
        }
    }
}