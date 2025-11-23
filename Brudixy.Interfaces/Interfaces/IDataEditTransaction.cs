namespace Brudixy.Interfaces
{
    public interface IDataEditTransaction
    {
        void Commit();

        bool Rollback();
    }
}