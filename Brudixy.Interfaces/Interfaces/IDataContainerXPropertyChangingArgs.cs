namespace Brudixy.Interfaces
{
    public interface IDataContainerXPropertyChangingArgs
    {
        IDataRowContainer Row { get; }

        string PropertyCode { get; }

        void SetNewValue<T>(T value);
        
        T GetNewValue<T>();

        T GetOldValue<T>();

        bool IsCancel { get; set; }
    }
}