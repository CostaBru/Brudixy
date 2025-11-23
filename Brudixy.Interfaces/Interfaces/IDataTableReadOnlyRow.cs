using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataTableReadOnlyRow : IDataRowReadOnlyAccessor
    {
        bool IsAddedRow { get; }

        bool IsDeletedRow { get; }

        ulong GetColumnAge(string columnName);

        bool IsDetachedRow { get; }

        bool IsModified { get; }

        bool IsModifiedOrAdded { get; }

        bool IsUnchangedRow { get; }

        bool IsChangedRow { get; }

        bool IsValidRowState { get; }

        [NotNull]
        IReadOnlyList<string> GetTableColumnNames();

        bool IsChanged([NotNull] string field);

        T GetValueOrDefault<T>(string columnOrXProp, T defaultValue);
       
        ICoreDataRowReadOnlyContainer ToContainer();
        
        [NotNull]
        new IEnumerable<IDataTableReadOnlyColumn> GetTableColumns();

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetChildRows([NotNull] string relationName);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetChildRows([NotNull] string keyFieldName, [NotNull] string parentKeyFieldName);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetChildRows([NotNull] IDataTableReadOnlyColumn keyFieldName, [NotNull] IDataTableReadOnlyColumn parentKeyFieldName);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetChildRows([NotNull] IDataRelation relation);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetParentRows([NotNull] IDataRelation relation);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetParentRows([NotNull] string relationName);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetParentRows([NotNull] string keyFieldName, [NotNull] string parentKeyFieldName);

        [CanBeNull]
        IDataTableReadOnlyRow GetParentRow([NotNull] IDataRelation relation);

        [CanBeNull]
        IDataTableReadOnlyRow GetParentRow([NotNull] string relationName);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetAllParentRows(string keyField, string parentField, bool addCurrent = true);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetAllChildRows(string keyField, string parentField, bool addCurrent = true);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetAllParentRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent = true);

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> GetAllChildRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent = true);
                
        [NotNull]
        new IDataRowReadOnlyContainer ToReadOnlyDataContainer();
    }
}