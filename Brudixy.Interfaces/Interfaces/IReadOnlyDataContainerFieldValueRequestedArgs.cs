namespace Brudixy.Interfaces
{
    public interface IReadOnlyDataContainerFieldValueRequestedArgs
    {
        IDataRowReadOnlyContainer Row { get; }

        string ColumnName { get; }

        object Value { get; set; }

        bool IsOverriden { get; set; }
    }
}