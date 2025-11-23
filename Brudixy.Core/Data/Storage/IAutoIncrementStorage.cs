using System;

namespace Brudixy
{
    public interface IAutoIncrementStorage
    {
        void ResetMax(Func<int, bool> checkFunc);

        object Max { get; set; }

        object NextAutoIncrementValue();
    }

    public interface IAutoIncrementStorageTyped<T> : IAutoIncrementStorage
    {
        T Max { get; set; }

        T NextAutoIncrementValue();
    }
}