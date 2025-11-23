namespace Brudixy.Expressions
{
    internal interface IFilter
    {
        bool Invoke(int? row = null);
    }
}
