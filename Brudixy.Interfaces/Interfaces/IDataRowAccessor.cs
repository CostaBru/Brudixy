using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataRowAccessor : IDataRowReadOnlyAccessor, ICoreDataRowAccessor
    {
        [CanBeNull]
        new object this[string columnOrXProp] { get; set; }

        [CanBeNull]
        new object this[IDataTableReadOnlyColumn column] { get; set; }

        [NotNull]
        new IDataRowAccessor Set<T>(string columnName, T value);

        new IDataRowAccessor Set<T>(string column, T? value) where T : struct, IComparable, IComparable<T>;

        [NotNull]
        IDataRowAccessor Set<T>(IDataTableReadOnlyColumn column, T value);

        IDataRowAccessor Set<T>(IDataTableReadOnlyColumn column, T? value) where T : struct, IComparable, IComparable<T>;

        [NotNull]
        new IDataRowAccessor SetNull(string columnName);

        [NotNull]
        new IDataRowAccessor SetNull(IDataTableReadOnlyColumn column);

        [CanBeNull]
        new object this[int handle] { get; set; }

        new void SilentlySetValue([NotNull] string columnName, object value);

        new void SilentlySetValue([NotNull] IDataTableReadOnlyColumn columnName, object value);

        new void SetXProperty<T>([NotNull] string propertyCode, [CanBeNull] T value);

        void SetColumnError([NotNull] string column, [CanBeNull] string error);

        void SetColumnWarning([NotNull] string column, [CanBeNull] string warning);

        void SetColumnInfo([NotNull] string column, [CanBeNull] string info);

        void SetRowFault([CanBeNull] string value);
        
        void SetRowError([CanBeNull] string value);

        void SetRowWarning([CanBeNull] string value);

        void SetRowInfo([CanBeNull] string value);

        void CopyFrom(IDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null);

        void CopyChanges(IDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null);
        
        IDataLockEventState LockEvents();

        void SetXPropertyAnnotation([CanBeNull] string propertyCode, string key, object value);

        void SetRowAnnotation([NotNull] string type, [CanBeNull] object value);

        void SetCellAnnotation([NotNull] string column, [NotNull] string type, [CanBeNull] object value);
    }
}