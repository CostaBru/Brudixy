namespace Brudixy
{
    public class DataRowTypeAttribute : Attribute
    {
        public Type DataRowType { get; private set; }
        public DataRowTypeAttribute(Type dataRowType)
        {
            DataRowType = dataRowType;
        }
    }
}
