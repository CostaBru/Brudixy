using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataTableColumn  : ICoreDataTableColumn, IDataTableReadOnlyColumn
    {
        new string Caption { get; set; }
        
        new bool IsReadOnly { get; set; }
        
        new string Expression { get; set; }
    }
    
    public interface ICoreDataTableColumn : ICoreTableReadOnlyColumn 
    {
        new string ColumnName { get; set; }

        new bool IsAutomaticValue { get; set; }

        new bool IsUnique { get; set; }

        new TableStorageType Type { get; set; }
        new TableStorageTypeModifier TypeModifier { get; set; }

        new object DefaultValue { get; set; }

        new uint? MaxLength { get; set; }

        void SetXProperty<T>(string propertyName, [CanBeNull] T value);
    }
}