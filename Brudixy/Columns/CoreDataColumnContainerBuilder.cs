namespace Brudixy
{
    public class DataColumnContainerBuilder : CoreDataColumnContainerBuilder
    {
        public DataColumnContainerBuilder()
        {
        }

        public string Caption { get; set; }

        public string Expression { get; set; }

        public bool IsReadOnly { get; set; }
        
        
        public override CoreDataColumnObj ToImmutable()
        {
            return new DataColumnObj(Caption,
                IsReadOnly, 
                Expression,
                TableName,
                DefaultValue,
                ColumnName,
                AllowNull,
                IsAutomaticValue,
                MaxLength,
                Type,
                TypeModifier,
                DataType,
                IsUnique,
                ColumnHandle,
                IsBuiltin,
                IsServiceColumn,
                HasIndex,
                XPropertiesStore.ToImmutable());
        }
    }
}