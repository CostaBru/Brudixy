using System;
using System.Runtime.Serialization;
using Brudixy.Expressions;

namespace Brudixy.Exceptions
{
    [Serializable]
    public class InvalidConstraintException : DataException
    {
        protected InvalidConstraintException(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
        }

        public InvalidConstraintException()
          : base("Invalid constraint exception")
        {
            HResult = -2146232028;
        }

        public InvalidConstraintException(string s)
          : base(s)
        {
            HResult = -2146232028;
        }

        public InvalidConstraintException(string message, Exception innerException)
          : base(message, innerException)
        {
            HResult = -2146232028;
        }
    }
}
