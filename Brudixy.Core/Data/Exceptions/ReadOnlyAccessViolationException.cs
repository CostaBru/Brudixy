namespace Brudixy.Exceptions
{
    public class ReadOnlyAccessViolationException : DataException
    {
        public ReadOnlyAccessViolationException()
        {
        }

        public ReadOnlyAccessViolationException(string message) : base(message)
        {
        }
    }
}