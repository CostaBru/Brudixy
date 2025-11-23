using System;
using Konsarpoo.Collections;

namespace Brudixy.Index
{
    internal abstract class UniqueIndex<T> : CoreHashIndex<T> where T : IComparable
    {
        protected UniqueIndex(bool unique) : base(unique)
        {
        }

        protected UniqueIndex(bool unique, Map<T, int> storage) : base(unique, storage)
        {
        }
    }
}
