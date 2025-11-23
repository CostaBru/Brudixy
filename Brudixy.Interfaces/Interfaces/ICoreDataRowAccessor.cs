using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface ICoreDataRowAccessor : ICoreDataRowReadOnlyAccessor
    {
        [CanBeNull]
        new object this[[NotNull] string columnOrXProp] { get; set; }
        
        [CanBeNull]
        new object this[[NotNull] ICoreTableReadOnlyColumn column] { get; set; }
        
        [NotNull]
        ICoreDataRowAccessor Set<T>([NotNull] string columnOrXProp, T value);
        
        [NotNull]
        ICoreDataRowAccessor Set<T>([NotNull] ICoreTableReadOnlyColumn column, T value);

        [NotNull]
        ICoreDataRowAccessor SetNull([NotNull] string columnOrXProp);

        [NotNull]
        ICoreDataRowAccessor SetNull([NotNull] ICoreTableReadOnlyColumn column);
        
        [CanBeNull]
        new object this[int handle] { get; set; }
        
        void SilentlySetValue([NotNull] string columnOrXProp, object value);

        void SilentlySetValue([NotNull] ICoreTableReadOnlyColumn columnName, object value);

        void SetXProperty<T>([NotNull] string propertyCode, [CanBeNull] T value);

        void CopyFrom(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null);

        void CopyChanges(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null);
        
        void SetModified();

        void AcceptChanges();

        void SetAdded();
    }
}