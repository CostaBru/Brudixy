using System.Collections.Generic;
using System.Linq;

namespace Brudixy.TypeGenerator.Core.Validation.Rules
{
    /// <summary>
    /// Validates that column references in PrimaryKey, Relations, Indexes, and GroupedProperties exist.
    /// </summary>
    public class ColumnReferenceValidationRule : IValidationRule
    {
        public string RuleName => "ColumnReferenceValidation";
        public int Priority => 20; // Run after column type validation

        public void Validate(ValidationContext context, ValidationResult result)
        {
            if (context.Table == null)
            {
                return;
            }

            var table = context.Table;
            var availableColumns = GetAvailableColumns(table, context);

            // Validate PrimaryKey column references
            ValidatePrimaryKey(table, availableColumns, context.FilePath, result);

            // Validate Relation column references
            ValidateRelations(table, availableColumns, context.FilePath, result);

            // Validate Index column references
            ValidateIndexes(table, availableColumns, context.FilePath, result);

            // Validate GroupedProperty column references
            ValidateGroupedProperties(table, availableColumns, context.FilePath, result);
        }

        private HashSet<string> GetAvailableColumns(DataTableObj table, ValidationContext context)
        {
            var columns = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // Add columns from current table
            foreach (var column in table.Columns.Keys)
            {
                columns.Add(column);
            }

            // Add columns from base tables
            foreach (var baseTable in context.LoadedBaseTables.Values)
            {
                foreach (var column in baseTable.Columns.Keys)
                {
                    columns.Add(column);
                }
            }

            return columns;
        }

        private void ValidatePrimaryKey(DataTableObj table, HashSet<string> availableColumns, string filePath, ValidationResult result)
        {
            if (table.PrimaryKey == null || table.PrimaryKey.Count == 0)
            {
                return;
            }

            foreach (var columnName in table.PrimaryKey)
            {
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    result.AddError(new ValidationError(
                        filePath,
                        "PrimaryKey",
                        "PrimaryKey contains an empty or null column name.",
                        "Remove empty entries from the PrimaryKey array."));
                    continue;
                }

                if (!availableColumns.Contains(columnName))
                {
                    result.AddError(new ValidationError(
                        filePath,
                        "PrimaryKey",
                        $"PrimaryKey references non-existent column '{columnName}'.",
                        $"Add column '{columnName}' to the Columns definition or remove it from PrimaryKey."));
                }
            }
        }

        private void ValidateRelations(DataTableObj table, HashSet<string> availableColumns, string filePath, ValidationResult result)
        {
            if (table.Relations == null || table.Relations.Count == 0)
            {
                return;
            }

            foreach (var relation in table.Relations)
            {
                var relationName = relation.Key;
                var relationObj = relation.Value;

                // Validate ParentKey and ChildKey are non-empty
                if (relationObj.ParentKey == null || relationObj.ParentKey.Length == 0)
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"Relations.{relationName}.ParentKey",
                        $"Relation '{relationName}' has an empty or null ParentKey array.",
                        "Specify at least one column in the ParentKey array."));
                }

                if (relationObj.ChildKey == null || relationObj.ChildKey.Length == 0)
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"Relations.{relationName}.ChildKey",
                        $"Relation '{relationName}' has an empty or null ChildKey array.",
                        "Specify at least one column in the ChildKey array."));
                }

                // Validate key array lengths match
                if (relationObj.ParentKey != null && relationObj.ChildKey != null &&
                    relationObj.ParentKey.Length != relationObj.ChildKey.Length)
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"Relations.{relationName}",
                        $"Relation '{relationName}' has mismatched key array lengths: ParentKey has {relationObj.ParentKey.Length} columns, ChildKey has {relationObj.ChildKey.Length} columns.",
                        "Ensure ParentKey and ChildKey have the same number of columns."));
                }

                // Validate ChildKey column references (only if ChildTable is current table or not specified)
                if (string.IsNullOrEmpty(relationObj.ChildTable) || 
                    string.Equals(relationObj.ChildTable, table.Table, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (relationObj.ChildKey != null)
                    {
                        foreach (var columnName in relationObj.ChildKey)
                        {
                            if (string.IsNullOrWhiteSpace(columnName))
                            {
                                result.AddError(new ValidationError(
                                    filePath,
                                    $"Relations.{relationName}.ChildKey",
                                    $"Relation '{relationName}' ChildKey contains an empty or null column name.",
                                    "Remove empty entries from the ChildKey array."));
                                continue;
                            }

                            if (!availableColumns.Contains(columnName))
                            {
                                result.AddError(new ValidationError(
                                    filePath,
                                    $"Relations.{relationName}.ChildKey",
                                    $"Relation '{relationName}' ChildKey references non-existent column '{columnName}'.",
                                    $"Add column '{columnName}' to the Columns definition or correct the ChildKey reference."));
                            }
                        }
                    }
                }

                // Validate ParentKey column references (only if ParentTable is current table or not specified)
                if (string.IsNullOrEmpty(relationObj.ParentTable) || 
                    string.Equals(relationObj.ParentTable, table.Table, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (relationObj.ParentKey != null)
                    {
                        foreach (var columnName in relationObj.ParentKey)
                        {
                            if (string.IsNullOrWhiteSpace(columnName))
                            {
                                result.AddError(new ValidationError(
                                    filePath,
                                    $"Relations.{relationName}.ParentKey",
                                    $"Relation '{relationName}' ParentKey contains an empty or null column name.",
                                    "Remove empty entries from the ParentKey array."));
                                continue;
                            }

                            if (!availableColumns.Contains(columnName))
                            {
                                result.AddError(new ValidationError(
                                    filePath,
                                    $"Relations.{relationName}.ParentKey",
                                    $"Relation '{relationName}' ParentKey references non-existent column '{columnName}'.",
                                    $"Add column '{columnName}' to the Columns definition or correct the ParentKey reference."));
                            }
                        }
                    }
                }
            }
        }

        private void ValidateIndexes(DataTableObj table, HashSet<string> availableColumns, string filePath, ValidationResult result)
        {
            if (table.Indexes == null || table.Indexes.Count == 0)
            {
                return;
            }

            foreach (var index in table.Indexes)
            {
                var indexName = index.Key;
                var indexObj = index.Value;

                if (indexObj.Columns == null || indexObj.Columns.Count == 0)
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"Indexes.{indexName}",
                        $"Index '{indexName}' has no columns defined.",
                        "Add at least one column to the index."));
                    continue;
                }

                foreach (var columnName in indexObj.Columns)
                {
                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"Indexes.{indexName}",
                            $"Index '{indexName}' contains an empty or null column name.",
                            "Remove empty entries from the index columns."));
                        continue;
                    }

                    if (!availableColumns.Contains(columnName))
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"Indexes.{indexName}",
                            $"Index '{indexName}' references non-existent column '{columnName}'.",
                            $"Add column '{columnName}' to the Columns definition or remove it from the index."));
                    }
                }
            }
        }

        private void ValidateGroupedProperties(DataTableObj table, HashSet<string> availableColumns, string filePath, ValidationResult result)
        {
            if (table.GroupedProperties == null || table.GroupedProperties.Count == 0)
            {
                return;
            }

            foreach (var groupedProperty in table.GroupedProperties)
            {
                var propertyName = groupedProperty.Key;
                var columnList = groupedProperty.Value;

                if (string.IsNullOrWhiteSpace(columnList))
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"GroupedProperties.{propertyName}",
                        $"Grouped property '{propertyName}' has an empty column list.",
                        "Specify at least one column for the grouped property."));
                    continue;
                }

                // Parse the column list (pipe-separated)
                var columns = columnList.Split('|')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                if (columns.Count == 0)
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"GroupedProperties.{propertyName}",
                        $"Grouped property '{propertyName}' has no valid columns after parsing.",
                        "Specify at least one column for the grouped property."));
                    continue;
                }

                // Validate each column exists
                foreach (var columnName in columns)
                {
                    if (!availableColumns.Contains(columnName))
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"GroupedProperties.{propertyName}",
                            $"Grouped property '{propertyName}' references non-existent column '{columnName}'.",
                            $"Add column '{columnName}' to the Columns definition or remove it from the grouped property."));
                    }
                }

                // Warn if fewer than 2 columns
                if (columns.Count < 2)
                {
                    result.AddWarning(new ValidationWarning(
                        filePath,
                        $"GroupedProperties.{propertyName}",
                        $"Grouped property '{propertyName}' has only {columns.Count} column(s). Grouped properties typically contain at least 2 columns."));
                }
            }
        }
    }
}
