using System;
using System.Runtime.Serialization;

namespace Brudixy.Exceptions
{
    public class DataDetachedException : DataException
    {
        protected DataDetachedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DataDetachedException(string s) : base(s)
        {
        }

        public DataDetachedException(string s, Exception innerException) : base(s, innerException)
        {
        }
    }
}