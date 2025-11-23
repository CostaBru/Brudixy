using System;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataTableRow : IDataTableReadOnlyRow, IDataRowAccessor
    {
        void Delete();

        IDataEditTransaction StartTransaction();
        
        [NotNull]
        new IDataRowContainer ToContainer();
        
        [NotNull]
        IDataTableRow Set([NotNull] string columnOrXProp, string value);

        [NotNull]
        IDataTableRow Set([NotNull] string columnOrXProp, XElement value);

        [NotNull]
        IDataTableRow Set([NotNull] string columnOrXProp, byte[] value);

        [NotNull]
        IDataTableRow Set([NotNull] string columnOrXProp, char[] value);

        [NotNull]
        IDataTableRow Set([NotNull] string columnOrXProp, Uri value);

        [NotNull]
        IDataTableRow Set([NotNull] string columnOrXProp, Type value);

        [NotNull]
        new IDataTableRow Set<T>([NotNull] string columnOrXProp, Nullable<T> value) where T : struct, IComparable, IComparable<T>;

        [NotNull]
        new IDataTableRow Set<T>([NotNull] string columnOrXProp, T value) where T : struct, IComparable, IComparable<T>;

        [NotNull]
        IDataTableRow Set([NotNull] IDataTableReadOnlyColumn column, string value);

        [NotNull]
        IDataTableRow Set([NotNull] IDataTableReadOnlyColumn column, XElement value);

        [NotNull]
        IDataTableRow Set([NotNull] IDataTableReadOnlyColumn column, byte[] value);

        [NotNull]
        IDataTableRow Set([NotNull] IDataTableReadOnlyColumn column, char[] value);

        [NotNull]
        IDataTableRow Set([NotNull] IDataTableReadOnlyColumn column, Uri value);

        [NotNull]
        IDataTableRow Set([NotNull] IDataTableReadOnlyColumn column, Type value);

        [NotNull]
        IDataTableRow Set<T>([NotNull] IDataTableReadOnlyColumn column, Nullable<T> value) where T : struct, IComparable, IComparable<T>;

        [NotNull]
        new IDataTableRow Set<T>([NotNull] IDataTableReadOnlyColumn column, T value) where T : struct, IComparable, IComparable<T>;

        [NotNull]
        new IEnumerable<IDataTableRow> GetChildRows([NotNull] string relationName);
        
        [NotNull]
        new IEnumerable<IDataTableRow> GetParentRows([NotNull] string relationName);

        [NotNull]
        new IEnumerable<IDataTableRow> GetChildRows([NotNull] string keyFieldName, [NotNull] string parentKeyFieldName);

        [NotNull]
        new IEnumerable<IDataTableRow> GetChildRows([NotNull] IDataTableReadOnlyColumn keyFieldName, [NotNull] IDataTableReadOnlyColumn parentKeyFieldName);

        [NotNull]
        new IEnumerable<IDataTableRow> GetChildRows([NotNull] IDataRelation relation);

        [NotNull]
        new IEnumerable<IDataTableRow> GetParentRows([NotNull] IDataRelation relation);

        [NotNull]
        new IEnumerable<IDataTableRow> GetParentRows([NotNull] string keyFieldName, [NotNull] string parentKeyFieldName);

        [CanBeNull]
        new IDataTableRow GetParentRow([NotNull] IDataRelation relation);

        [CanBeNull]
        new IDataTableRow GetParentRow([NotNull] string relationName);

        [NotNull]
        new IEnumerable<IDataTableRow> GetAllParentRows(string keyField, string parentField, bool addCurrent = true);

        [NotNull]
        new IEnumerable<IDataTableRow> GetAllChildRows(string keyField, string parentField, bool addCurrent = true);

        [NotNull]
        new IEnumerable<IDataTableRow> GetAllParentRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent = true);

        [NotNull]
        new IEnumerable<IDataTableRow> GetAllChildRows(IDataTableReadOnlyColumn keyField, IDataTableReadOnlyColumn parentField, bool addCurrent = true);

        void EditRow<T>(Action<T> action) where T : IDataRowAccessor;
    }
}