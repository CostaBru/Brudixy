namespace Brudixy.Interfaces
{
    public interface IDataContainerFieldValueChangingArgs
    {
        IDataRowContainer Row { get; }

        string ColumnName { get; }

        object NewValue { get; set; }

        object OldValue { get; }

        bool IsCancel { get; set; }
    }
}