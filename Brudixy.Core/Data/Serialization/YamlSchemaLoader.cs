using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Brudixy.Interfaces;
using Brudixy.TypeGenerator;
using Brudixy.TypeGenerator.Core;

namespace Brudixy.Serialization;

/// <summary>
/// Orchestrates loading and applying YAML schemas to CoreDataTable instances
/// </summary>
public class YamlSchemaLoader
{
    private readonly SchemaValidator _validator;
    private readonly YamlSchemaReader _reader;
    
    /// <summary>
    /// Initializes a new instance of YamlSchemaLoader using the embedded schema resource
    /// </summary>
    public YamlSchemaLoader()
    {
        _validator = new SchemaValidator();
        _reader = new YamlSchemaReader();
    }
    
    /// <summary>
    /// Initializes a new instance of YamlSchemaLoader using a custom schema file path
    /// </summary>
    /// <param name="schemaFilePath">Path to the JSON schema file</param>
    public YamlSchemaLoader(string schemaFilePath)
    {
        _validator = new SchemaValidator(schemaFilePath);
        _reader = new YamlSchemaReader();
    }
    
    /// <summary>
    /// Loads a YAML schema into an existing CoreDataTable
    /// </summary>
    /// <param name="table">The table to load the schema into</param>
    /// <param name="yamlContent">The YAML content to load</param>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public void LoadIntoTable(CoreDataTable table, string yamlContent)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));
        if (string.IsNullOrEmpty(yamlContent))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yamlContent));
        
        // Validate the YAML content
        var validationResult = _validator.Validate(yamlContent);
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors, yamlContent);
        }
        
        // Parse the YAML into DataTableObj
        var schema = _reader.GetTable(yamlContent);
        
        // Apply the schema to the table
        ApplyTableSchema(table, schema);
    }
    
    /// <summary>
    /// Loads a YAML schema from a file into an existing CoreDataTable
    /// </summary>
    /// <param name="table">The table to load the schema into</param>
    /// <param name="yamlFilePath">Path to the YAML file</param>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public void LoadIntoTableFromFile(CoreDataTable table, string yamlFilePath)
    {
        if (!File.Exists(yamlFilePath))
        {
            throw new FileNotFoundException($"YAML file not found: {yamlFilePath}", yamlFilePath);
        }
        
        var yamlContent = File.ReadAllText(yamlFilePath);
        LoadIntoTable(table, yamlContent);
    }
    
    /// <summary>
    /// Creates a new child table in a dataset from YAML content
    /// </summary>
    /// <param name="dataset">The dataset (CoreDataTable acting as dataset) to add the child table to</param>
    /// <param name="yamlContent">The YAML content to load</param>
    /// <returns>The newly created child table</returns>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public CoreDataTable LoadAsChildTable(CoreDataTable dataset, string yamlContent)
    {
        if (dataset == null)
            throw new ArgumentNullException(nameof(dataset));
        if (string.IsNullOrEmpty(yamlContent))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yamlContent));
        
        // Validate the YAML content
        var validationResult = _validator.Validate(yamlContent);
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors, yamlContent);
        }
        
        // Parse the YAML into DataTableObj
        var schema = _reader.GetTable(yamlContent);
        
        // Create a new child table
        var childTable = dataset.NewTable(schema.Table);
        
        // Apply the schema to the child table
        ApplyTableSchema(childTable, schema);
        
        // Add the table to the dataset
        dataset.AddTable(childTable);
        
        return childTable;
    }
    
    /// <summary>
    /// Creates a new child table in a dataset from a YAML file
    /// </summary>
    /// <param name="dataset">The dataset (CoreDataTable acting as dataset) to add the child table to</param>
    /// <param name="yamlFilePath">Path to the YAML file</param>
    /// <returns>The newly created child table</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public CoreDataTable LoadAsChildTableFromFile(CoreDataTable dataset, string yamlFilePath)
    {
        if (!File.Exists(yamlFilePath))
        {
            throw new FileNotFoundException($"YAML file not found: {yamlFilePath}", yamlFilePath);
        }
        
        var yamlContent = File.ReadAllText(yamlFilePath);
        return LoadAsChildTable(dataset, yamlContent);
    }
    
    /// <summary>
    /// Loads multiple tables with relations into a dataset
    /// </summary>
    /// <param name="dataset">The dataset (CoreDataTable acting as dataset) to load tables into</param>
    /// <param name="yamlContents">Collection of YAML contents to load</param>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public void LoadMultipleTables(CoreDataTable dataset, IEnumerable<string> yamlContents)
    {
        if (dataset == null)
            throw new ArgumentNullException(nameof(dataset));
        if (yamlContents == null)
            throw new ArgumentNullException(nameof(yamlContents));
        
        var schemas = new List<DataTableObj>();
        var contents = yamlContents.ToList();
        
        // Validate all YAML contents first
        foreach (var yamlContent in contents)
        {
            var validationResult = _validator.Validate(yamlContent);
            if (!validationResult.IsValid)
            {
                throw new SchemaValidationException(validationResult.Errors, yamlContent);
            }
            
            var schema = _reader.GetTable(yamlContent);
            schemas.Add(schema);
        }
        
        // Create all tables first (without relations)
        var tables = new Dictionary<string, CoreDataTable>(StringComparer.OrdinalIgnoreCase);
        foreach (var schema in schemas)
        {
            CoreDataTable table;
            if (dataset.HasTable(schema.Table))
            {
                table = dataset.GetTable(schema.Table);
            }
            else
            {
                table = dataset.NewTable(schema.Table);
                ApplyTableSchema(table, schema);
                dataset.AddTable(table);
            }
            
            tables[schema.Table] = table;
        }
        
        // Now establish all relations
        ApplyRelations(dataset, schemas);
    }
    
    /// <summary>
    /// Applies a DataTableObj schema to a CoreDataTable
    /// </summary>
    private void ApplyTableSchema(CoreDataTable table, DataTableObj schema)
    {
        // Apply table name
        if (!string.IsNullOrEmpty(schema.Table))
        {
            table.Name = schema.Table;
        }
        
        // Apply columns
        ApplyColumns(table, schema);
        
        // Apply primary key
        ApplyPrimaryKey(table, schema);
        
        // Apply indexes
        ApplyIndexes(table, schema);
        
        // Apply column options (constraints, defaults, etc.)
        ApplyColumnOptions(table, schema);
        
        // Apply extended properties
        ApplyXProperties(table, schema);
        
        // Apply grouped properties
        ApplyGroupedProperties(table, schema);
    }
    
    /// <summary>
    /// Creates columns from DataTableObj
    /// </summary>
    private void ApplyColumns(CoreDataTable table, DataTableObj schema)
    {
        foreach (var columnKv in schema.ColumnObjects)
        {
            var columnName = columnKv.Key;
            var columnInfo = columnKv.Value;

            // Map column type
            var (storageType, typeModifier, userType) = MapColumnType(columnInfo);

            table.AddColumn(columnName: columnName,
                valueType: storageType,
                valueTypeModifier: typeModifier,
                auto: columnInfo.Auto,
                unique: columnInfo.IsUnique,
                columnMaxLength: columnInfo.MaxLength,
                defaultValue: ParseDefaultValue(columnInfo.DefaultValue, storageType),
                builtin: true,
                serviceColumn: columnInfo.IsService ?? false,
                allowNull: columnInfo.AllowNull ?? true);
        }
    }
    
    /// <summary>
    /// Maps a ColumnInfo type to CoreDataTable storage types
    /// </summary>
    private (TableStorageType storageType, TableStorageTypeModifier modifier, Type userType) MapColumnType(ColumnInfo columnInfo)
    {
        var typeModifier = TableStorageTypeModifier.Simple;
        
        // Handle type modifiers
        if (!string.IsNullOrEmpty(columnInfo.TypeModifier))
        {
            typeModifier = columnInfo.TypeModifier switch
            {
                "Array" => TableStorageTypeModifier.Array,
                "Range" => TableStorageTypeModifier.Range,
                "Complex" => TableStorageTypeModifier.Complex,
                _ => TableStorageTypeModifier.Simple
            };
        }
        
        // Handle user types
        if (columnInfo.Type == "UserType")
        {
            // For user types, we'll use String storage and store the type name
            // The actual type resolution would need to be handled by the application
            return (TableStorageType.String, typeModifier, null);
        }
        
        // Map built-in types
        var storageType = columnInfo.Type switch
        {
            "Int32" => TableStorageType.Int32,
            "Int64" => TableStorageType.Int64,
            "String" => TableStorageType.String,
            "DateTime" => TableStorageType.DateTime,
            "Boolean" => TableStorageType.Boolean,
            "Guid" => TableStorageType.Guid,
            "Decimal" => TableStorageType.Decimal,
            "Double" => TableStorageType.Double,
            "Byte" => TableStorageType.Byte,
            _ => TableStorageType.String // Default to string for unknown types
        };
        
        return (storageType, typeModifier, null);
    }
    
    /// <summary>
    /// Parses a default value string into the appropriate type
    /// </summary>
    private object ParseDefaultValue(string defaultValue, TableStorageType storageType)
    {
        if (string.IsNullOrEmpty(defaultValue))
            return null;
        
        try
        {
            return storageType switch
            {
                TableStorageType.Int32 => int.Parse(defaultValue),
                TableStorageType.Int64 => long.Parse(defaultValue),
                TableStorageType.Boolean => bool.Parse(defaultValue),
                TableStorageType.Decimal => decimal.Parse(defaultValue),
                TableStorageType.Double => double.Parse(defaultValue),
                TableStorageType.Byte => byte.Parse(defaultValue),
                TableStorageType.DateTime => DateTime.Parse(defaultValue),
                TableStorageType.Guid => Guid.Parse(defaultValue),
                _ => defaultValue
            };
        }
        catch
        {
            // If parsing fails, return the string value
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Configures primary key columns
    /// </summary>
    private void ApplyPrimaryKey(CoreDataTable table, DataTableObj schema)
    {
        if (schema.PrimaryKey != null && schema.PrimaryKey.Count > 0)
        {
            table.SetPrimaryKeyColumns(schema.PrimaryKey);
        }
    }
    
    /// <summary>
    /// Creates indexes from schema
    /// </summary>
    private void ApplyIndexes(CoreDataTable table, DataTableObj schema)
    {
        if (schema.Indexes == null)
            return;
        
        foreach (var indexKv in schema.Indexes)
        {
            var index = indexKv.Value;
            
            if (index.Columns != null && index.Columns.Count > 0)
            {
                if (index.Columns.Count == 1)
                {
                    // Single column index
                    table.AddIndex(index.Columns[0], index.Unique);
                }
                else
                {
                    // Multi-column index
                    table.AddMultiColumnIndex(index.Columns, index.Unique);
                }
            }
        }
    }
    
    /// <summary>
    /// Applies column options like MaxLength, IsUnique, DefaultValue, AllowNull, HasIndex
    /// </summary>
    private void ApplyColumnOptions(CoreDataTable table, DataTableObj schema)
    {
        // Column options are already applied during column creation in ApplyColumns
        // This method is kept for potential future enhancements
        
        // Apply HasIndex from ColumnOptions
        foreach (var columnKv in schema.ColumnObjects)
        {
            var columnName = columnKv.Key;
            var columnInfo = columnKv.Value;
            
            if (columnInfo.HasIndex == true && !table.HasIndex(columnName))
            {
                table.AddIndex(columnName, columnInfo.IsUnique == true);
            }
        }
    }
    
    /// <summary>
    /// Attaches extended properties to the table
    /// </summary>
    private void ApplyXProperties(CoreDataTable table, DataTableObj schema)
    {
        if (schema.XProperties == null)
            return;
        
        foreach (var xPropKv in schema.XProperties)
        {
            var propName = xPropKv.Key;
            var xProp = xPropKv.Value;
            
            // Parse the value based on the type
            object value = ParseXPropertyValue(xProp);
            
            table.SetXProperty(propName, value);
        }
        
        // Store CodeGenerationOptions as extended properties
        if (schema.CodeGenerationOptions != null)
        {
            table.SetXProperty("CodeGenerationOptions.BaseNamespace", schema.CodeGenerationOptions.BaseNamespace);
            table.SetXProperty("CodeGenerationOptions.BaseClass", schema.CodeGenerationOptions.BaseClass);
            table.SetXProperty("CodeGenerationOptions.BaseRowClass", schema.CodeGenerationOptions.BaseRowClass);
            table.SetXProperty("CodeGenerationOptions.Namespace", schema.CodeGenerationOptions.Namespace);
            table.SetXProperty("CodeGenerationOptions.Class", schema.CodeGenerationOptions.Class);
            table.SetXProperty("CodeGenerationOptions.RowClass", schema.CodeGenerationOptions.RowClass);
            table.SetXProperty("CodeGenerationOptions.Abstract", schema.CodeGenerationOptions.Abstract);
            table.SetXProperty("CodeGenerationOptions.Sealed", schema.CodeGenerationOptions.Sealed);
            table.SetXProperty("CodeGenerationOptions.BaseTableFileName", schema.CodeGenerationOptions.BaseTableFileName);
        }
    }
    
    /// <summary>
    /// Parses an XProperty value based on its type
    /// </summary>
    private object ParseXPropertyValue(XProperty xProp)
    {
        if (string.IsNullOrEmpty(xProp.Value))
            return null;
        
        if (string.IsNullOrEmpty(xProp.Type))
            return xProp.Value;
        
        try
        {
            return xProp.Type switch
            {
                "Int32" => int.Parse(xProp.Value),
                "Int64" => long.Parse(xProp.Value),
                "Boolean" => bool.Parse(xProp.Value),
                "Decimal" => decimal.Parse(xProp.Value),
                "Double" => double.Parse(xProp.Value),
                "DateTime" => DateTime.Parse(xProp.Value),
                "Guid" => Guid.Parse(xProp.Value),
                _ => xProp.Value
            };
        }
        catch
        {
            return xProp.Value;
        }
    }
    
    /// <summary>
    /// Stores grouped property definitions
    /// </summary>
    private void ApplyGroupedProperties(CoreDataTable table, DataTableObj schema)
    {
        if (schema.GroupColumnObjects == null || schema.GroupColumnObjects.Count == 0)
            return;
        
        // Store grouped properties as extended properties
        foreach (var groupKv in schema.GroupColumnObjects)
        {
            var groupName = groupKv.Key;
            var groupInfo = groupKv.Value;
            
            // Store the column list
            table.SetXProperty($"GroupedProperty.{groupName}.Columns", string.Join("|", groupInfo.Columns));
            
            // Store options
            if (!string.IsNullOrEmpty(groupInfo.StructName))
            {
                table.SetXProperty($"GroupedProperty.{groupName}.StructName", groupInfo.StructName);
            }
            
            table.SetXProperty($"GroupedProperty.{groupName}.IsReadOnly", groupInfo.IsReadOnly);
            table.SetXProperty($"GroupedProperty.{groupName}.Type", groupInfo.Type.ToString());
        }
    }
    
    /// <summary>
    /// Establishes relations between tables
    /// </summary>
    private void ApplyRelations(CoreDataTable dataset, List<DataTableObj> schemas)
    {
        foreach (var schema in schemas)
        {
            if (schema.Relations == null || schema.Relations.Count == 0)
                continue;
            
            foreach (var relationKv in schema.Relations)
            {
                var relationName = relationKv.Key;
                var relation = relationKv.Value;
                
                // Check if both tables exist
                if (!dataset.HasTable(relation.ParentTable))
                {
                    throw new RelationException(
                        $"Cannot create relation '{relationName}': parent table '{relation.ParentTable}' does not exist");
                }
                
                if (!dataset.HasTable(relation.ChildTable))
                {
                    throw new RelationException(
                        $"Cannot create relation '{relationName}': child table '{relation.ChildTable}' does not exist");
                }
                
                var parentTable = dataset.GetTable(relation.ParentTable);
                var childTable = dataset.GetTable(relation.ChildTable);
                
                // Verify columns exist
                foreach (var col in relation.ParentKey)
                {
                    if (parentTable.TryGetColumn(col) == null)
                    {
                        throw new RelationException(
                            $"Cannot create relation '{relationName}': column '{col}' does not exist in parent table '{relation.ParentTable}'");
                    }
                }
                
                foreach (var col in relation.ChildKey)
                {
                    if (childTable.TryGetColumn(col) == null)
                    {
                        throw new RelationException(
                            $"Cannot create relation '{relationName}': column '{col}' does not exist in child table '{relation.ChildTable}'");
                    }
                }
                
                // Build the key list with column objects
                var keys = new List<(CoreDataColumn parentColumn, CoreDataColumn childColumn)>();
                for (int i = 0; i < relation.ParentKey.Length; i++)
                {
                    var parentCol = (CoreDataColumn)parentTable.GetColumn(relation.ParentKey[i]);
                    var childCol = (CoreDataColumn)childTable.GetColumn(relation.ChildKey[i]);
                    keys.Add((parentCol, childCol));
                }
                
                // Add the relation with default rules
                dataset.AddRelation(
                    relationName,
                    keys
                );
            }
        }
    }
}
