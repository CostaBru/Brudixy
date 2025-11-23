using System;
using System.Runtime.CompilerServices;
using Konsarpoo.Collections;

namespace Brudixy
{
    /// <summary>
    /// Bloom filter.
    /// </summary>
    /// <typeparam name="T">Item type </typeparam>
    public class BloomFilter<T> : IDisposable
    {
        private readonly int m_hashFunctionCount;
        private readonly BitArr m_hashBits;
        private readonly HashFunction m_getHashSecondary;
        private int m_hashBitsCount;

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        public BloomFilter(int capacity)
            : this(capacity < 2 ? 2 : capacity, null)
        {
        }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        public BloomFilter(int capacity, float errorRate)
            : this(capacity < 1 ? 1 : capacity, errorRate, null)
        {
        }

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public BloomFilter(int capacity, HashFunction hashFunction)
            : this(capacity < 2 ? 2 : capacity, BestErrorRate(capacity < 2 ? 2 : capacity), hashFunction)
        {
        }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public BloomFilter(int capacity, float errorRate, HashFunction hashFunction)
            : this(capacity < 2 ? 2 : capacity, errorRate, hashFunction, BestM(capacity < 2 ? 2 : capacity, errorRate), BestK(capacity < 2 ? 2 : capacity, errorRate))
        {
        }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public BloomFilter(int capacity, float errorRate, HashFunction hashFunction, int m, int k)
        {
            // validate the params are in range
            if (capacity < 2)
            {
                capacity = 2;
            }

            if (errorRate >= 1 || errorRate <= 0)
            {
                throw new ArgumentOutOfRangeException("errorRate", errorRate, string.Format("errorRate must be between 0 and 1, exclusive. Was {0}", errorRate));
            }

            // from overflow in bestM calculation
            if (m < 1)
            {
                throw new ArgumentOutOfRangeException(string.Format("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}", capacity, errorRate));
            }

            // set the secondary hash function
            if (hashFunction == null)
            {
                if (typeof(T) == typeof(string))
                {
                    m_getHashSecondary = HashOfString;
                }
                else if (typeof(T) == typeof(int))
                {
                    m_getHashSecondary = HashInt32;
                }
                else
                {
                    m_getHashSecondary = HashCommon;
                }
            }
            else
            {
                m_getHashSecondary = hashFunction;
            }

            m_hashFunctionCount = k;
            m_hashBits = new BitArr(m);
            m_hashBitsCount = m_hashBits.Count;
        }

        /// <summary>
        /// A function that can be used to hash input.
        /// </summary>
        /// <param name="input">The values to be hashed.</param>
        /// <returns>The resulting hash code.</returns>
        public delegate int HashFunction(T input);

        /// <summary>
        /// The ratio of false to true bits in the filter. E.g., 1 true bit in a 10 bit filter means a truthiness of 0.1.
        /// </summary>
        public double Truthiness
        {
            get
            {
                return (double)TrueBits() / m_hashBitsCount;
            }
        }


        /// <summary>
        /// Adds a new item to the filter. It cannot be removed.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(int item)
        {
            int secondaryHash = item;
            // start flipping bits for each hash of item
            int primaryHash = item.GetHashCode();

            HashInt(ref secondaryHash);

            for (int i = 0; i < m_hashFunctionCount; i++)
            {
                int hash = ComputeHash(ref primaryHash, ref secondaryHash, ref i);
                m_hashBits[hash] = true;
            }
        }

        /// <summary>
        /// Adds a new item to the filter. It cannot be removed.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(string item)
        {
            // start flipping bits for each hash of item
            int primaryHash = item.GetHashCode();
            int secondaryHash = HashString(item);
            for (int i = 0; i < m_hashFunctionCount; i++)
            {
                int hash = ComputeHash(ref primaryHash, ref secondaryHash,ref i);
                m_hashBits[hash] = true;
            }
        }

        /// <summary>
        /// Adds a new item to the filter. It cannot be removed.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(T item)
        {
            // start flipping bits for each hash of item
            int primaryHash = item.GetHashCode();
            int secondaryHash = m_getHashSecondary(item);
            for (int i = 0; i < m_hashFunctionCount; i++)
            {
                int hash = ComputeHash(ref primaryHash, ref secondaryHash, ref i);
                m_hashBits[hash] = true;
            }
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The <see cref="bool"/>. </returns>
        public bool Contains(int item)
        {
            var secondaryHash = item;
            int primaryHash = item.GetHashCode();
            HashInt(ref secondaryHash);
            for (int i = 0; i < m_hashFunctionCount; i++)
            {
                int hash = ComputeHash(ref primaryHash, ref secondaryHash, ref i);
                if (m_hashBits[hash] == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The <see cref="bool"/>. </returns>
        public bool Contains(string item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = HashString(item);
            for (int i = 0; i < m_hashFunctionCount; i++)
            {
                int hash = ComputeHash(ref primaryHash, ref secondaryHash, ref i);
                if (m_hashBits[hash] == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The <see cref="bool"/>. </returns>
        public bool Contains(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = m_getHashSecondary(item);
            for (int i = 0; i < m_hashFunctionCount; i++)
            {
                int hash = ComputeHash(ref primaryHash, ref secondaryHash, ref i);
                if (m_hashBits[hash] == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The best k.
        /// </summary>
        /// <param name="capacity"> The capacity. </param>
        /// <param name="errorRate"> The error rate. </param>
        /// <returns> The <see cref="int"/>. </returns>
        private static int BestK(int capacity, float errorRate)
        {
            return (int)Math.Round(Math.Log(2.0) * BestM(capacity, errorRate) / capacity);
        }

        /// <summary>
        /// The best m.
        /// </summary>
        /// <param name="capacity"> The capacity. </param>
        /// <param name="errorRate"> The error rate. </param>
        /// <returns> The <see cref="int"/>. </returns>
        private static int BestM(int capacity, float errorRate)
        {
            return (int)Math.Ceiling(capacity * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        /// <summary>
        /// The best error rate.
        /// </summary>
        /// <param name="capacity"> The capacity. </param>
        /// <returns> The <see cref="float"/>. </returns>
        private static float BestErrorRate(int capacity)
        {
            float c = (float)(1.0 / capacity);
            if (c != 0)
            {
                return c;
            }

            // default
            // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
            return (float)Math.Pow(0.6185, int.MaxValue / capacity);
        }

        /// <summary>
        /// Hashes a 32-bit signed int using Thomas Wang's method v3.1 (http://www.concentric.net/~Ttwang/tech/inthash.htm).
        /// Runtime is suggested to be 11 cycles. 
        /// </summary>
        /// <param name="input">The integer to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashCommon(T input)
        {
            return input.GetHashCode();
        }

        /// <summary>
        /// Hashes a 32-bit signed int using Thomas Wang's method v3.1 (http://www.concentric.net/~Ttwang/tech/inthash.htm).
        /// Runtime is suggested to be 11 cycles. 
        /// </summary>
        /// <param name="input">The integer to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashInt32(T input)
        {
            int x = 0;

            GenericConverter.ConvertTo(ref input, ref x);

            HashInt(ref x);

            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HashInt(ref int x)
        {
            unchecked
            {
                x = ~x + (x << 15); // x = (x << 15) - x- 1, as (~x) + y is equivalent to y - x - 1 in two's complement representation
                x = x ^ (x >> 12);
                x = x + (x << 2);
                x = x ^ (x >> 4);
                x = x * 2057; // x = (x + (x << 3)) + (x<< 11);
                x = x ^ (x >> 16);
            }
        }

        /// <summary>
        /// Hashes a string using Bob Jenkin's "One At A Time" method from Dr. Dobbs (http://burtleburtle.net/bob/hash/doobs.html).
        /// Runtime is suggested to be 9x+9, where x = input.Length. 
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashOfString(T input)
        {
            string s = input as string;
            return HashString(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashString(string s)
        {
            int hash = 0;

            var length = s.Length;

            for (int i = 0; i < length; i++)
            {
                hash += s[i];
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }

            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            return hash;
        }

        /// <summary>
        /// The true bits.
        /// </summary>
        /// <returns> The <see cref="int"/>. </returns>
        private int TrueBits()
        {
            int output = 0;

            for (int i = 0; i < m_hashBitsCount; i++)
            {
                var bit = m_hashBits[i];

                if (bit)
                {
                    output++;
                }
            }

            return output;
        }

        /// <summary>
        /// Performs Dillinger and Manolios double hashing. 
        /// </summary>
        /// <param name="primaryHash"> The primary hash. </param>
        /// <param name="secondaryHash"> The secondary hash. </param>
        /// <param name="i"> The i. </param>
        /// <returns> The <see cref="int"/>. </returns>
        private int ComputeHash(ref int primaryHash, ref int secondaryHash, ref int i)
        {
            int resultingHash = (primaryHash + (i * secondaryHash)) % m_hashBitsCount;
            return Math.Abs(resultingHash);
        }

        private void DisposeCore()
        {
            m_hashBits?.Dispose();

            m_hashBitsCount = 0;
        }

        public void Clear()
        {
            m_hashBits?.Clear();

            m_hashBitsCount = 0;
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        ~BloomFilter()
        {
            DisposeCore();
        }
    }
}