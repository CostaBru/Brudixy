namespace Brudixy.Interfaces
{
    public interface IDataColumnChangingEventArgs : IDataTableEventArgs
    {
        string ColumnName { get; }

        object NewValue { get; set; }

        object OldValue { get; }

        IDataTableRow Row { get; }

        bool IsCancel { get; set; }
        
        string ErrorMessage { get; set; }
    }
    
    public interface IDataColumnChangingTypedEventArgs<T> : IDataColumnChangingEventArgs
    {
        new T NewValue { get; set; }

        new T OldValue { get; }
        
        bool NewValueIsNull { get; set; }
        
        bool PrevValueIsNull { get; }
    }
}