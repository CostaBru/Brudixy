using System;
using System.Runtime.Serialization;

namespace Brudixy.Exceptions
{
    [Serializable]
    public class DataException : SystemException
    {
       protected DataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

       public DataException()
       {
       }

        public DataException(string s)
            : base(s)
        {
        }

        public DataException(string s, Exception innerException)
            : base(s, innerException)
        {
        }
    }
}