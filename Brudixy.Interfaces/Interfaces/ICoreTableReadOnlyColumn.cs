using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface ICoreTableReadOnlyColumn
    {
        string ColumnName { get; }
        bool IsAutomaticValue { get; }

        bool IsUnique { get; }

        TableStorageType Type { get; }
        TableStorageTypeModifier TypeModifier { get; }

        string TableName { get; }

        object DefaultValue { get; }

        uint? MaxLength { get; }

        [CanBeNull]
        T GetXProperty<T>(string xPropertyName);

        bool HasXProperty(string xPropertyName);

        [NotNull]
        IEnumerable<string> XProperties { get; }

        [NotNull]
        IReadOnlyCollection<KeyValuePair<string, object>> GetXProperties();
        
        Type DataType { get; }
        
        int ColumnHandle { get; }
    }
}