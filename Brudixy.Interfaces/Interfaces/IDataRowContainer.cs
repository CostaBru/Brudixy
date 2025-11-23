using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataRowContainer :
        IDataRowReadOnlyContainer,
        ICoreDataRowContainer, 
        IDataRowAccessor,
        IDataRowEdit
    {
        [CanBeNull]
        new object this[[NotNull] string columnOrXProp] { get; set; }
        
        [NotNull]
        new IDataRowReadOnlyContainer ToReadOnly();

     
        [CanBeNull]
        new object this[int handle] { get; set; }

        [NotNull]
        new IDataRowContainer Set<T>([NotNull] string columnName, T value);

        [NotNull]
        new IDataRowContainer SetNull([NotNull] string columnName);

        [CanBeNull]
        new object this[IDataTableReadOnlyColumn column] { get; set; }

        [NotNull]
        new IDataRowContainer Set<T>([NotNull] IDataTableReadOnlyColumn columnName, T value);

        [NotNull]
        new IDataRowContainer SetNull([NotNull] IDataTableReadOnlyColumn columnName);
       
        [NotNull]
        new IDataRowContainer Clone();

        IDataLoadState BeginLoad();

        new RowState RowRecordState { get; set; }
    }
}