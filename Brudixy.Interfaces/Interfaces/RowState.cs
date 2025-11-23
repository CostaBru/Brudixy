
namespace Brudixy.Interfaces
{
    public enum RowState
    {
        New = -1,
        Unchanged = 0,
        Added = 2,
        Modified = 4,
        Deleted = 8,
        Detached = 16,
        Disposed = 32,
    }
}