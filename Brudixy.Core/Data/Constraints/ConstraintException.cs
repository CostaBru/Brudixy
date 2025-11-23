using System;
using System.Runtime.Serialization;
using Brudixy.Exceptions;
using Brudixy.Expressions;

namespace Brudixy.Constraints
{
   
    [Serializable]
    public class ConstraintException : DataException
    {
        protected ConstraintException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

         public ConstraintException()
            : base("Dataset constraint exception")
        {
            HResult = -2146232022;
        }

        public ConstraintException(string s)
            : base(s)
        {
            HResult = -2146232022;
        }

       public ConstraintException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = -2146232022;
        }
    }
}