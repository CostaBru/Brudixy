namespace Brudixy.Interfaces
{
    public interface IDataContainerFieldValueRequestedArgs : IReadOnlyDataContainerFieldValueRequestedArgs
    {
        new IDataRowContainer Row { get; }
    }
}