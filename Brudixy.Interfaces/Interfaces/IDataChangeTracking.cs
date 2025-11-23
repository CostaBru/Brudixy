using System;
using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataChangeTracking
    {
        void StartTrackingChangeTimes(DateTime utcTime);
        void StopTrackingChangeTimes();
        IDisposable StartLoggingChanges(object context);
        void StopLoggingChanges();
        IReadOnlyList<IDataLogEntry> GetLoggedChanges();
        bool GetIsLoggingChanges();
        void ClearLoggedChanges();
        object CurrentChangingContext();
    }
}