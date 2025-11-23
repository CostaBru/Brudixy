namespace Brudixy
{
    public interface ICompareStorageTyped<T>
    {
        int Compare(T val1, T val2);
    }
}