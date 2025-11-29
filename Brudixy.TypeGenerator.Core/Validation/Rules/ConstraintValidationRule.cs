using System.Collections.Generic;
using System.Linq;

namespace Brudixy.TypeGenerator.Core.Validation.Rules
{
    /// <summary>
    /// Validates constraints such as primary keys and unique columns.
    /// </summary>
    public class ConstraintValidationRule : IValidationRule
    {
        public string RuleName => "ConstraintValidation";
        public int Priority => 30; // Run after column type and reference validation

        public void Validate(ValidationContext context, ValidationResult result)
        {
            if (context.Table == null)
            {
                return;
            }

            var table = context.Table;

            // Validate primary key constraints
            ValidatePrimaryKeyConstraints(table, context.FilePath, result);

            // Validate unique column constraints
            ValidateUniqueColumnConstraints(table, context.FilePath, result);
        }

        private void ValidatePrimaryKeyConstraints(DataTableObj table, string filePath, ValidationResult result)
        {
            if (table.PrimaryKey == null || table.PrimaryKey.Count == 0)
            {
                return;
            }

            // Check for duplicate column names in PrimaryKey
            var duplicates = table.PrimaryKey
                .GroupBy(col => col, System.StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                result.AddError(new ValidationError(
                    filePath,
                    "PrimaryKey",
                    $"PrimaryKey contains duplicate column name '{duplicate}'.",
                    "Remove duplicate entries from the PrimaryKey array."));
            }

            // Validate that primary key columns are non-nullable
            foreach (var columnName in table.PrimaryKey)
            {
                // Check if column exists in current table (base table columns checked by ColumnReferenceValidationRule)
                string typeString = null;
                if (table.Columns.ContainsKey(columnName))
                {
                    typeString = table.Columns[columnName];
                }
                else
                {
                    // Column might be in base table - skip type validation here
                    // ColumnReferenceValidationRule will verify it exists
                    continue;
                }

                var typeInfo = ColumnTypeInfo.Parse(typeString);

                // Check if column is nullable (only for columns defined in current table)
                if (typeInfo.IsNullable)
                {
                    result.AddError(new ValidationError(
                        filePath,
                        $"PrimaryKey",
                        $"Primary key column '{columnName}' cannot be nullable. Column type is '{typeString}'.",
                        $"Remove the '?' modifier from column '{columnName}' or remove it from PrimaryKey."));
                }

                // Check ColumnOptions for AllowNull setting (only for current table)
                if (table.ColumnOptions.ContainsKey(columnName))
                {
                    var options = table.ColumnOptions[columnName];
                    if (options.ContainsKey("AllowNull"))
                    {
                        var allowNull = DataTableObj.ConvertToBool(options["AllowNull"]);
                        if (allowNull == true)
                        {
                            result.AddError(new ValidationError(
                                filePath,
                                $"ColumnOptions.{columnName}.AllowNull",
                                $"Primary key column '{columnName}' cannot have AllowNull set to true.",
                                $"Set AllowNull to false for column '{columnName}' or remove it from PrimaryKey."));
                        }
                    }
                }
            }
        }

        private void ValidateUniqueColumnConstraints(DataTableObj table, string filePath, ValidationResult result)
        {
            // Check columns marked as unique in ColumnOptions (current table only)
            foreach (var columnOption in table.ColumnOptions)
            {
                var columnName = columnOption.Key;
                var options = columnOption.Value;

                if (!options.ContainsKey("IsUnique"))
                {
                    continue;
                }

                var isUnique = DataTableObj.ConvertToBool(options["IsUnique"]);
                if (isUnique != true)
                {
                    continue;
                }

                // Check if AllowNull is set to true
                if (options.ContainsKey("AllowNull"))
                {
                    var allowNull = DataTableObj.ConvertToBool(options["AllowNull"]);
                    if (allowNull == true)
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"ColumnOptions.{columnName}",
                            $"Unique column '{columnName}' cannot have AllowNull set to true.",
                            $"Set AllowNull to false for column '{columnName}' or remove IsUnique."));
                    }
                }

                // Check if the column type is nullable (check current table first, then base tables)
                string typeString = null;
                if (table.Columns.ContainsKey(columnName))
                {
                    typeString = table.Columns[columnName];
                }

                if (typeString != null)
                {
                    var typeInfo = ColumnTypeInfo.Parse(typeString);

                    if (typeInfo.IsNullable)
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"Columns.{columnName}",
                            $"Unique column '{columnName}' cannot be nullable. Column type is '{typeString}'.",
                            $"Remove the '?' modifier from column '{columnName}' or remove IsUnique."));
                    }
                }
            }

            // Check columns with Unique modifier in type string (current table and base tables)
            var allColumns = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            
            // Add columns from current table
            foreach (var col in table.Columns)
            {
                allColumns[col.Key] = col.Value;
            }

            foreach (var column in allColumns)
            {
                var columnName = column.Key;
                var typeString = column.Value;
                var typeInfo = ColumnTypeInfo.Parse(typeString);

                if (!typeInfo.IsUnique)
                {
                    continue;
                }

                // Check if the column is nullable
                if (typeInfo.IsNullable)
                {
                    // Only report error if column is defined in current table
                    if (table.Columns.ContainsKey(columnName))
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"Columns.{columnName}",
                            $"Unique column '{columnName}' cannot be nullable. Column type is '{typeString}'.",
                            $"Remove the '?' modifier or the 'Unique' modifier from column '{columnName}'."));
                    }
                }

                // Check ColumnOptions for conflicting AllowNull (only for current table)
                if (table.ColumnOptions.ContainsKey(columnName))
                {
                    var options = table.ColumnOptions[columnName];
                    if (options.ContainsKey("AllowNull"))
                    {
                        var allowNull = DataTableObj.ConvertToBool(options["AllowNull"]);
                        if (allowNull == true)
                        {
                            result.AddError(new ValidationError(
                                filePath,
                                $"ColumnOptions.{columnName}.AllowNull",
                                $"Unique column '{columnName}' cannot have AllowNull set to true.",
                                $"Set AllowNull to false for column '{columnName}' or remove the 'Unique' modifier."));
                        }
                    }
                }
            }
        }
    }
}
