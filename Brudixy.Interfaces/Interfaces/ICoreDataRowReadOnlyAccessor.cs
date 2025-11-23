using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface ICoreDataRowReadOnlyAccessor
    {
        string GetTableName();

        [NotNull]
        IEnumerable<ICoreTableReadOnlyColumn> PrimaryKeyColumn { get; }
        
        IComparable[] GetRowKeyValue();

        [NotNull]
        ICoreTableReadOnlyColumn GetColumn([NotNull] string columnName);
        
        [NotNull]
        ICoreTableReadOnlyColumn GetColumn(int columnHandle);

        [CanBeNull]
        ICoreTableReadOnlyColumn TryGetColumn([NotNull] string columnName);

        bool IsExistsField([NotNull] string columnName);

        string ToString(ICoreTableReadOnlyColumn column);
        
        string ToString(string columnOrXProp);

        [NotNull]
        IEnumerable<ICoreTableReadOnlyColumn> GetColumns();

        RowState RowRecordState { get; }

        [CanBeNull]
        T Field<T>([NotNull] string columnOrXProp);

        [CanBeNull]
        T Field<T>([NotNull] ICoreTableReadOnlyColumn column);

        [CanBeNull]
        T Field<T>([NotNull] string columnOrXProp, T defaultIfNull);

        [CanBeNull]
        T Field<T>([NotNull] ICoreTableReadOnlyColumn column, T defaultIfNull);

        IReadOnlyList<T> FieldArray<T>(string columnOrXProp);

        IReadOnlyList<T> FieldArray<T>(string columnOrXProp, IReadOnlyList<T> defaultIfNull);

        bool IsNull([NotNull] string columnOrXProp);

        bool IsNotNull([NotNull] string columnOrXProp);

        bool IsNull([NotNull] ICoreTableReadOnlyColumn column);

        bool IsNotNull([NotNull] ICoreTableReadOnlyColumn column);

        [CanBeNull]
        object this[[NotNull] ICoreTableReadOnlyColumn column] { get; }

        [CanBeNull]
        object this[[NotNull] string columnOrXProp] { get; }

        object this[int handle] { get; }

        [NotNull]
        IEnumerable<string> GetChangedFields();

        [NotNull]
        IEnumerable<string> GetChangedXProperties();

        [CanBeNull]
        object GetOriginalValue([NotNull] ICoreTableReadOnlyColumn column);

        [CanBeNull]
        object GetOriginalValue([NotNull] string columnOrXProp);

        T GetOriginalValue<T>(string columnOrXProp);
        
        T GetOriginalValue<T>([NotNull] ICoreTableReadOnlyColumn column);

        [CanBeNull]
        T GetXProperty<T>([NotNull] string xPropertyName, bool original = false);

        [NotNull]
        IEnumerable<string> GetXProperties();

        bool HasXProperty(string xPropertyName);

        ulong GetRowAge();

        int GetColumnCount();

        int RowHandle { get; }
    }
}