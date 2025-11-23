namespace Brudixy.Interfaces.Delegates
{
    public interface IDataEventReceiver<T>
    {
        bool OnEvent(T args, string context = null);
    }
}