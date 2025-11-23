namespace Brudixy.Interfaces
{
    public interface IDataLockEventState
    {
        void ResetAggregatedEvents();
        
        void UnlockEvents();
    }
}