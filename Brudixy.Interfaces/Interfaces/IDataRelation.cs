using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataRelation
    {
        string Name { get; }
        ICoreReadOnlyDataTable ParentTable { get; }
        ICoreReadOnlyDataTable ChildTable { get; }
        IEnumerable<ICoreDataTableColumn> ParentColumns { get; }
        IEnumerable<ICoreDataTableColumn> ChildColumns { get; }
        string ParentTableName { get; }
        string ChildTableName { get; }
        IReadOnlyList<T> GetChildRows<T>(int rowHandle, bool ignoreDeleted) where T: ICoreDataRowReadOnlyAccessor;
        ICoreDataRowReadOnlyAccessor GetParentRow(int rowHandle);
        IReadOnlyList<T> GetParentRows<T>(int rowHandle) where T : ICoreDataRowReadOnlyAccessor;
    }
}