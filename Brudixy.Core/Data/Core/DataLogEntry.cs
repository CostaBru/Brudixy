using System;
using Brudixy.Interfaces;

namespace Brudixy
{
    public class DataLogEntry : IDataLogEntry
    {
        public DataLogEntry(int? tranId)
        {
            TranId = tranId;
        }
        
        public DataLogEntry(int rowHandle, object context, string columnOrXProperty, object value, object prevValue, ulong age, DateTime? utcTimestamp, int? tranId)
        {
            RowHandle = rowHandle;
            Context = context;
            ColumnOrXProperty = columnOrXProperty;
            Value = value;
            PrevValue = prevValue;
            Age = age;
            UtcTimestamp = utcTimestamp;
            TranId = tranId;
        }

        public int RowHandle { get; }
        
        public object Context { get; }
        
        public string ColumnOrXProperty { get; }
        
        public ulong Age { get; }
        
        public object Value { get; }
        
        public object PrevValue { get; }
        
        public DateTime? UtcTimestamp { get; }
        
        public int? TranId { get; }
    }
}