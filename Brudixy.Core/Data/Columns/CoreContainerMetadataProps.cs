using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class CoreContainerMetadataProps
    {
        public readonly IReadOnlyList<string> KeyColumns;
        public readonly IReadOnlyDictionary<string, CoreDataColumnContainer> ColumnMap;
        public readonly IReadOnlyList<CoreDataColumnContainer> Columns;
        public readonly string TableName;
        public readonly int Age;

        public CoreContainerMetadataProps(string tableName, 
            IReadOnlyList<CoreDataColumnContainer> columns, 
            IReadOnlyDictionary<string, CoreDataColumnContainer> columnMap, 
            IReadOnlyList<string> keyColumn,
            int age)
        {
            TableName = tableName;
            ColumnMap = columnMap;
            Columns = columns;
            KeyColumns = keyColumn;
            Age = age;
        }

        public CoreContainerMetadataProps([NotNull] ICoreDataRowReadOnlyAccessor row, Func<ICoreTableReadOnlyColumn, CoreDataColumnContainer> colFactory)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            TableName = row.GetTableName();

            var columnMap = row.GetColumns()
                .ToFrozenDictionary(c => c.ColumnName, c => CoreDataColumnContainer.CreateFrom(c, colFactory), StringComparer.CurrentCultureIgnoreCase);
            
            ColumnMap = columnMap;
            Columns = row.GetColumns().Select(colFactory).ToArray();

            KeyColumns = row.PrimaryKeyColumn.Select(s => s.ColumnName).ToArray();
        }

        public IReadOnlyList<CoreDataColumnContainer> PrimaryKeyColumn
        {
            get
            {
                if (KeyColumns == null)
                {
                    return Array.Empty<CoreDataColumnContainer>();
                }
                
                var result = new CoreDataColumnContainer[KeyColumns.Count];

                for (var index = 0; index < KeyColumns.Count; index++)
                {
                    result[index] = ColumnMap[KeyColumns[index]];
                }

                return result;
            }
        }

        [NotNull]
        public CoreDataColumnContainer GetColumn(string columnName)
        {
            if (ColumnMap.TryGetValue(columnName, out var col))
            {
                return col;
            }

            throw new MissingMetadataException($"DataRowContainer created from {TableName} does not have {columnName} column.");
        }

        [CanBeNull]
        public CoreDataColumnContainer TryGetColumn(string columnName)
        {
            return ColumnMap.GetValueOrDefault(columnName);
        }

        public CoreDataColumnContainer GetColumn(int columnHandle)
        {
            return Columns[columnHandle];
        }
    }
}