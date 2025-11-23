using System;
using System.Collections.Generic;
using Brudixy.Constraints;
using Brudixy.Index;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.Tests
{
    [TestFixture]
    public class MulityColumnIndexTest
    {
        [Test]
        public void Test1([Values(1, 10)] int num1)
        {
            var index = new MultiColumnBisectIndex( 100, 1, false);

            var expectedData = new Dictionary<int, int>();

            int reference = 0;
            for (int i = 1; i <= num1; i++)
            {
                var key = new IComparable[] {i};

                index.Add(key, reference);

                expectedData[i] = reference;


                reference++;
            }

            foreach (var dataKey in expectedData)
            {
                var key = new IComparable[] { dataKey.Key};

                var row = index.Search(key);

                Assert.AreEqual(dataKey.Value, row);
            }
        }


        [Test]
        public void Test2([Values(1, 10)] int num1, [Values(1, 10)] int num2)
        {
            var index = new MultiColumnBisectIndex( 100, 2, false);

            var expectedData = new Dictionary<Tuple<int, int>, int>();

            var expectedDataForSingle = new Dictionary<int, HashSet<int>>();

            int reference = 0;
            for (int i = 1; i <= num1; i++)
            {
                expectedDataForSingle[i] = new HashSet<int>();

                for (int j = 1; j <= num2; j++)
                {
                    var key = new IComparable[] {i, j};

                    index.Add(key, reference);

                    expectedData[new Tuple<int, int>(i, j)] = reference;

                    expectedDataForSingle[i].Add(reference);

                    reference++;
                }
            }

            foreach (var dataKey in expectedDataForSingle)
            {
                var key = new IComparable[] { dataKey.Key };

                var row = index.Search(key);

                Assert.True(dataKey.Value.Contains(row));
            }

            foreach (var dataKey in expectedData)
            {
                var key = new IComparable[]{ dataKey.Key.Item1, dataKey.Key.Item2 };

                var row = index.Search(key);

                Assert.AreEqual(dataKey.Value, row);
            }
        }

        [Test]
        public void Test3([Values(1, 10)] int num1, [Values(1, 10)] int num2, [Values(1, 10)] int num3)
        {
            var index = new MultiColumnBisectIndex( 100, 3, false);

            var expectedData = new Dictionary<Tuple<int, int, int>, int>();

            var expectedDataForSingle = new Dictionary<int, HashSet<int>>();

            var expectedDataForTuple = new Dictionary<Tuple<int, int>, HashSet<int>>();

            int reference = 0;
            for (int i = 1; i <= num1; i++)
            {
                expectedDataForSingle[i] = new HashSet<int>();

                for (int j = 1; j <= num2; j++)
                {
                    expectedDataForTuple[new Tuple<int, int>(i, j)] = new HashSet<int>();

                    for (int k = 1; k <= num3; k++)
                    {
                        var key = new IComparable[] { i, j, k };

                        index.Add(key, reference);

                        expectedData[new Tuple<int, int, int>(i, j, k)] = reference;

                        expectedDataForSingle[i].Add(reference);

                        expectedDataForTuple[new Tuple<int, int>(i, j)].Add(reference);

                        reference++;
                    }
                }
            }

            foreach (var dataKey in expectedDataForTuple)
            {
                var key = new IComparable[] { dataKey.Key.Item1, dataKey.Key.Item2 };

                var row = index.Search(key);

                Assert.True(dataKey.Value.Contains(row));
            }

            foreach (var dataKey in expectedDataForSingle)
            {
                var key = new IComparable[] { dataKey.Key };

                var row = index.Search(key);

                Assert.True(dataKey.Value.Contains(row));
            }

            foreach (var dataKey in expectedData)
            {
                var key = new IComparable[] { dataKey.Key.Item1, dataKey.Key.Item2, dataKey.Key.Item3 };

                var row = index.Search(key);

                Assert.AreEqual(dataKey.Value, row);
            }
        }

        [Test]
        public void TestUnique([Values(1, 2, 3, 10)]int count,[Values(1, 2, 3, 50)] int rows)
        {
            var index = new MultiColumnBisectIndex( 100, count, true);

            int valueOffset = 0;

            var keys = new Data<IComparable[]>();

            for (int r = 0; r < rows; r++)
            {
                var key = new IComparable[count ];

                for (int i = 0; i < count; i++)
                {
                    key[i] = valueOffset;
                }

                valueOffset += count;

                index.Add(key, r);

                keys.Add(key);
            }

            foreach (var key in keys)
            {
                Assert.Throws<ConstraintException>(() => index.Add(key, 0));
            }
        }

        [Test]
        public void TestUniqueUniuon1([Values(1, 2, 3, 10)]int count, [Values(1, 2, 3, 50)] int rows1)
        {
            int valueOffset = 0;

            var keys = new Data<IComparable[]>();
            
            var index =  new MultiColumnBisectIndex(  100, count,true);

            for (int r = 0; r < rows1; r++)
            {
                var key = new IComparable[count ];

                for (int i = 0; i < count; i++)
                {
                    key[i] = valueOffset;
                }

                valueOffset += count;

                keys.Add(key);
                
                index.Add(key, r);
            }

            foreach (var key in keys)
            {
                Assert.Throws<ConstraintException>(() => index.Add(key, 0));
            }
        }
    }
}
