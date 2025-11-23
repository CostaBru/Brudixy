namespace Brudixy.Interfaces
{
    public interface IDataTableReadOnlyColumn : ICoreTableReadOnlyColumn
    {
        string Caption { get; }

        bool IsReadOnly { get; }

        string Expression { get; }
    }
}