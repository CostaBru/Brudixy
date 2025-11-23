using System;

namespace Brudixy
{
    public static class MeasureSize
    {
        public static long GetByteSize<T>(Func<int, T> generator, int numberOfInstances = 10000)
        {
            var m_memArray = new T[numberOfInstances];

            //Make one to make sure it is jitted
            generator(0);

            long oldSize = GC.GetTotalMemory(false);
            for (int i = 0; i < m_memArray.Length; i++)
            {
                m_memArray[i] = generator(i);
            }

            long newSize = GC.GetTotalMemory(false);
            return (newSize - oldSize) / m_memArray.Length;
        }
    }
}
