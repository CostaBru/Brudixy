using System;
using System.Collections.Generic;

namespace Brudixy
{
    public interface ICompareStorage
    {
        IEnumerable<int> Filter<V>(V value) where V : IComparable;
    }
}