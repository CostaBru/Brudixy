using System;

namespace Brudixy.Interfaces
{
    public interface IDataLogEntry
    {
        int RowHandle { get; }
        object Context { get; }
        string ColumnOrXProperty { get; }
        ulong Age { get; }
        object Value { get; }
        object PrevValue { get; }
        DateTime? UtcTimestamp { get; }
        int? TranId { get; }
    }
}