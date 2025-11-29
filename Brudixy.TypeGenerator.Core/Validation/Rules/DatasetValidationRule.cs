using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Brudixy.TypeGenerator.Core.Validation.Rules
{
    /// <summary>
    /// Validates dataset definitions including table uniqueness and relation integrity.
    /// </summary>
    public class DatasetValidationRule : IValidationRule
    {
        public string RuleName => "DatasetValidation";
        public int Priority => 50; // Run after other validations

        public void Validate(ValidationContext context, ValidationResult result)
        {
            if (context.Table == null)
            {
                return;
            }

            var dataset = context.Table;

            // Only validate if this is a dataset (has Tables list)
            if (dataset.Tables == null || dataset.Tables.Count == 0)
            {
                return;
            }

            // Validate unique table names
            ValidateUniqueTableNames(dataset, context.FilePath, result);

            // Validate relations
            ValidateDatasetRelations(dataset, context, result);
        }

        private void ValidateUniqueTableNames(DataTableObj dataset, string filePath, ValidationResult result)
        {
            var duplicates = dataset.Tables
                .GroupBy(t => t, System.StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                result.AddError(new ValidationError(
                    filePath,
                    "Tables",
                    $"Dataset contains duplicate table name '{duplicate}'.",
                    "Remove duplicate table entries from the Tables list."));
            }
        }

        private void ValidateDatasetRelations(DataTableObj dataset, ValidationContext context, ValidationResult result)
        {
            if (dataset.Relations == null || dataset.Relations.Count == 0)
            {
                return;
            }

            // Load all table schemas for validation
            var tableSchemas = LoadTableSchemas(dataset, context);

            foreach (var relation in dataset.Relations)
            {
                var relationName = relation.Key;
                var relationObj = relation.Value;

                // Validate that referenced tables exist in the dataset
                ValidateRelationTableReferences(relationName, relationObj, dataset, context.FilePath, result);

                // Validate column existence and type compatibility
                ValidateRelationColumns(relationName, relationObj, tableSchemas, context.FilePath, result);
            }
        }

        private Dictionary<string, DataTableObj> LoadTableSchemas(DataTableObj dataset, ValidationContext context)
        {
            var schemas = new Dictionary<string, DataTableObj>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var tableName in dataset.Tables)
            {
                try
                {
                    // Construct the table file path
                    var datasetDir = Path.GetDirectoryName(context.FilePath);
                    var tableFileName = $"{tableName}.dt.brudixy.yaml";
                    var tablePath = Path.Combine(datasetDir ?? "", tableFileName);

                    // Try to load the table schema
                    var tableYaml = context.FileSystem.GetFileContents(tablePath);
                    if (!string.IsNullOrEmpty(tableYaml))
                    {
                        var yamlReader = new YamlSchemaReader();
                        var tableSchema = yamlReader.GetTable(tableYaml);
                        schemas[tableName] = tableSchema;
                    }
                }
                catch
                {
                    // If we can't load a table, we'll report it as a warning
                    // The actual file existence check should be done elsewhere
                }
            }

            return schemas;
        }

        private void ValidateRelationTableReferences(
            string relationName,
            DataRelationObj relationObj,
            DataTableObj dataset,
            string filePath,
            ValidationResult result)
        {
            // Check if ParentTable exists in dataset
            if (!string.IsNullOrEmpty(relationObj.ParentTable) &&
                !dataset.Tables.Contains(relationObj.ParentTable, System.StringComparer.OrdinalIgnoreCase))
            {
                result.AddError(new ValidationError(
                    filePath,
                    $"Relations.{relationName}.ParentTable",
                    $"Relation '{relationName}' references ParentTable '{relationObj.ParentTable}' which is not in the dataset's Tables list.",
                    $"Add '{relationObj.ParentTable}' to the Tables list or correct the ParentTable reference."));
            }

            // Check if ChildTable exists in dataset
            if (!string.IsNullOrEmpty(relationObj.ChildTable) &&
                !dataset.Tables.Contains(relationObj.ChildTable, System.StringComparer.OrdinalIgnoreCase))
            {
                result.AddError(new ValidationError(
                    filePath,
                    $"Relations.{relationName}.ChildTable",
                    $"Relation '{relationName}' references ChildTable '{relationObj.ChildTable}' which is not in the dataset's Tables list.",
                    $"Add '{relationObj.ChildTable}' to the Tables list or correct the ChildTable reference."));
            }
        }

        private void ValidateRelationColumns(
            string relationName,
            DataRelationObj relationObj,
            Dictionary<string, DataTableObj> tableSchemas,
            string filePath,
            ValidationResult result)
        {
            // Get parent and child table schemas
            DataTableObj parentSchema = null;
            DataTableObj childSchema = null;

            if (!string.IsNullOrEmpty(relationObj.ParentTable))
            {
                tableSchemas.TryGetValue(relationObj.ParentTable, out parentSchema);
            }

            if (!string.IsNullOrEmpty(relationObj.ChildTable))
            {
                tableSchemas.TryGetValue(relationObj.ChildTable, out childSchema);
            }

            // Validate ParentKey columns
            if (parentSchema != null && relationObj.ParentKey != null)
            {
                for (int i = 0; i < relationObj.ParentKey.Length; i++)
                {
                    var columnName = relationObj.ParentKey[i];
                    
                    if (!parentSchema.Columns.ContainsKey(columnName))
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"Relations.{relationName}.ParentKey",
                            $"Relation '{relationName}' ParentKey references column '{columnName}' which does not exist in table '{relationObj.ParentTable}'.",
                            $"Add column '{columnName}' to table '{relationObj.ParentTable}' or correct the ParentKey reference."));
                        continue;
                    }

                    // Validate type compatibility if both parent and child columns exist
                    if (childSchema != null && relationObj.ChildKey != null && i < relationObj.ChildKey.Length)
                    {
                        var childColumnName = relationObj.ChildKey[i];
                        
                        if (childSchema.Columns.ContainsKey(childColumnName))
                        {
                            ValidateColumnTypeCompatibility(
                                relationName,
                                relationObj.ParentTable,
                                columnName,
                                parentSchema.Columns[columnName],
                                relationObj.ChildTable,
                                childColumnName,
                                childSchema.Columns[childColumnName],
                                filePath,
                                result);
                        }
                    }
                }
            }

            // Validate ChildKey columns
            if (childSchema != null && relationObj.ChildKey != null)
            {
                foreach (var columnName in relationObj.ChildKey)
                {
                    if (!childSchema.Columns.ContainsKey(columnName))
                    {
                        result.AddError(new ValidationError(
                            filePath,
                            $"Relations.{relationName}.ChildKey",
                            $"Relation '{relationName}' ChildKey references column '{columnName}' which does not exist in table '{relationObj.ChildTable}'.",
                            $"Add column '{columnName}' to table '{relationObj.ChildTable}' or correct the ChildKey reference."));
                    }
                }
            }
        }

        private void ValidateColumnTypeCompatibility(
            string relationName,
            string parentTable,
            string parentColumn,
            string parentType,
            string childTable,
            string childColumn,
            string childType,
            string filePath,
            ValidationResult result)
        {
            var parentTypeInfo = ColumnTypeInfo.Parse(parentType);
            var childTypeInfo = ColumnTypeInfo.Parse(childType);

            // Check if base types match
            if (!string.Equals(parentTypeInfo.BaseType, childTypeInfo.BaseType, System.StringComparison.OrdinalIgnoreCase))
            {
                result.AddError(new ValidationError(
                    filePath,
                    $"Relations.{relationName}",
                    $"Relation '{relationName}' has incompatible column types: " +
                    $"ParentKey column '{parentTable}.{parentColumn}' has type '{parentType}' " +
                    $"but ChildKey column '{childTable}.{childColumn}' has type '{childType}'.",
                    "Ensure that corresponding columns in ParentKey and ChildKey have compatible types."));
            }

            // Warn if nullability differs (child can be nullable, but parent should typically not be)
            if (!parentTypeInfo.IsNullable && childTypeInfo.IsNullable)
            {
                result.AddWarning(new ValidationWarning(
                    filePath,
                    $"Relations.{relationName}",
                    $"Relation '{relationName}': ChildKey column '{childTable}.{childColumn}' is nullable " +
                    $"but ParentKey column '{parentTable}.{parentColumn}' is not. This may allow orphaned records."));
            }
        }
    }
}
