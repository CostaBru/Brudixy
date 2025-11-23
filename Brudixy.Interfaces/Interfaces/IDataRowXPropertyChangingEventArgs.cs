namespace Brudixy.Interfaces
{
    public interface IDataRowXPropertyChangingEventArgs : IDataTableEventArgs
    {
        string PropertyCode { get; }

        void SetNewValue<T>(T value);
        
        T GetNewValue<T>();

        T GetOldValue<T>();

        IDataTableRow Row { get; }

        bool IsCancel { get; set; }
    }
}