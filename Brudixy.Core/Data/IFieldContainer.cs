namespace Brudixy
{
    public interface IFieldContainer
    {
        void Set(string col, object objValue);

        object Get(string col);
    }
}