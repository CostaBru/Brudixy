namespace Brudixy.Exceptions
{
    public class MissingMetadataException : DataException
    {
        public MissingMetadataException()
        {
        }

        public MissingMetadataException(string message) : base(message)
        {
        }
    }
}