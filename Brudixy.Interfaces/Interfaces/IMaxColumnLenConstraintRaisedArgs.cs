namespace Brudixy.Interfaces
{
    public interface IMaxColumnLenConstraintRaisedArgs 
    {
        ICoreDataTable Table { get; }

        ICoreDataRowReadOnlyAccessor Row { get; }

        string ColumnName { get; }

        object Value { get; }

        T GetValue<T>();
        
        bool RaiseError { get; set; }
        bool PreValidating { get; }
    }
}