using System;
using NUnit.Framework;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Index;

namespace Brudixy.Tests
{
    [TestFixture]
    public class GuidCoreUniqueIndexTest
    {
        [Test]
        public void Test_Add()
        {
            var guidIndex = new CoreStructHashIndex<Guid>(true);

            var value = Guid.NewGuid();
            guidIndex.Add(value, 1);

            Assert.AreEqual(1, guidIndex.Count);
            
            Assert.Throws<ConstraintException>(() => guidIndex.Add(new Guid?(), 1));
            Assert.Throws<ConstraintException>(() => guidIndex.Update(new Guid?(), 1, value));
            Assert.Throws<ConstraintException>(() => guidIndex.Update((IComparable)new Guid?(), 1, value));

            Assert.AreEqual(1, guidIndex.Count);
        }

        [Test]
        public void Test_Search()
        {
            var test = Guid.NewGuid();

            var guidIndex = new CoreStructHashIndex<Guid>(true);
            guidIndex.Add(test, 1);

            var result = guidIndex.Search(test);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_SearchRange()
        { 
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>(true);
            guidIndex.Add(test, 1);
           
            Assert.Throws<ConstraintException>(() => guidIndex.Add(test, 2));

            var results = guidIndex.SearchRange(test).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Contains(1));
        }

        [Test]
        public void Test_Update()
        { 
            var test1 = Guid.NewGuid();
            var test2 = Guid.NewGuid();
            
            var guidIndex = new CoreStructHashIndex<Guid>( true);
            guidIndex.Add(test1, 1);
            guidIndex.Update(test2, 1, test1);

            var result = guidIndex.Search(test2);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_Remove()
        {  
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>( true);
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
            var guidIndex = new CoreStructHashIndex<Guid>( true);
            guidIndex.Add(test, 1);
            
            var copy = guidIndex.Copy() as CoreStructHashIndex<Guid>;

            var result = copy.Search(test);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Test_Clone()
        { 
            var test = Guid.NewGuid();
            var guidIndex = new CoreStructHashIndex<Guid>( true);
            guidIndex.Add(test, 1);
            
            var clone = guidIndex.Clone() as CoreStructHashIndex<Guid>;

            var result = clone.Search(test);

            Assert.AreEqual(-1, result);
        }
    }
}