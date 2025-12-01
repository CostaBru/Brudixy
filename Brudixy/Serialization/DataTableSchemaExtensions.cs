using System;
using System.Collections.Generic;
using System.IO;

namespace Brudixy.Serialization;

/// <summary>
/// Extension methods for loading YAML schemas into DataTable instances
/// </summary>
public static class DataTableSchemaExtensions
{
    /// <summary>
    /// Loads a YAML schema into the current DataTable
    /// </summary>
    /// <param name="table">The table to load the schema into</param>
    /// <param name="yamlContent">The YAML content to load</param>
    /// <param name="schemaPath">Optional path to a custom JSON schema file</param>
    /// <exception cref="ArgumentNullException">Thrown when table or yamlContent is null</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public static void LoadSchemaFromYaml(this DataTable table, string yamlContent, string schemaPath = null)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));
        if (string.IsNullOrEmpty(yamlContent))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yamlContent));
        
        var loader = string.IsNullOrEmpty(schemaPath) 
            ? new YamlSchemaLoader() 
            : new YamlSchemaLoader(schemaPath);
        
        loader.LoadIntoTable(table, yamlContent);
    }
    
    /// <summary>
    /// Loads a YAML schema from a file into the current DataTable
    /// </summary>
    /// <param name="table">The table to load the schema into</param>
    /// <param name="yamlFilePath">Path to the YAML file</param>
    /// <param name="schemaPath">Optional path to a custom JSON schema file</param>
    /// <exception cref="ArgumentNullException">Thrown when table or yamlFilePath is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public static void LoadSchemaFromYamlFile(this DataTable table, string yamlFilePath, string schemaPath = null)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));
        if (string.IsNullOrEmpty(yamlFilePath))
            throw new ArgumentException("YAML file path cannot be null or empty", nameof(yamlFilePath));
        
        var loader = string.IsNullOrEmpty(schemaPath) 
            ? new YamlSchemaLoader() 
            : new YamlSchemaLoader(schemaPath);
        
        loader.LoadIntoTableFromFile(table, yamlFilePath);
    }
    
    /// <summary>
    /// Creates a new child table in a dataset from YAML content
    /// </summary>
    /// <param name="dataset">The dataset (DataTable acting as dataset) to add the child table to</param>
    /// <param name="yamlContent">The YAML content to load</param>
    /// <param name="schemaPath">Optional path to a custom JSON schema file</param>
    /// <returns>The newly created child table</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataset or yamlContent is null</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public static CoreDataTable LoadChildTableFromYaml(this DataTable dataset, string yamlContent, string schemaPath = null)
    {
        if (dataset == null)
            throw new ArgumentNullException(nameof(dataset));
        if (string.IsNullOrEmpty(yamlContent))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yamlContent));
        
        var loader = string.IsNullOrEmpty(schemaPath) 
            ? new YamlSchemaLoader() 
            : new YamlSchemaLoader(schemaPath);
        
        return loader.LoadAsChildTable(dataset, yamlContent);
    }
    
    /// <summary>
    /// Creates a new child table in a dataset from a YAML file
    /// </summary>
    /// <param name="dataset">The dataset (DataTable acting as dataset) to add the child table to</param>
    /// <param name="yamlFilePath">Path to the YAML file</param>
    /// <param name="schemaPath">Optional path to a custom JSON schema file</param>
    /// <returns>The newly created child table</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataset or yamlFilePath is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public static CoreDataTable LoadChildTableFromYamlFile(this DataTable dataset, string yamlFilePath, string schemaPath = null)
    {
        if (dataset == null)
            throw new ArgumentNullException(nameof(dataset));
        if (string.IsNullOrEmpty(yamlFilePath))
            throw new ArgumentException("YAML file path cannot be null or empty", nameof(yamlFilePath));
        
        var loader = string.IsNullOrEmpty(schemaPath) 
            ? new YamlSchemaLoader() 
            : new YamlSchemaLoader(schemaPath);
        
        return loader.LoadAsChildTableFromFile(dataset, yamlFilePath);
    }
    
    /// <summary>
    /// Loads multiple child tables with relations into a dataset
    /// </summary>
    /// <param name="dataset">The dataset (DataTable acting as dataset) to load tables into</param>
    /// <param name="yamlFilePaths">Collection of YAML file paths to load</param>
    /// <param name="schemaPath">Optional path to a custom JSON schema file</param>
    /// <exception cref="ArgumentNullException">Thrown when dataset or yamlFilePaths is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when any file doesn't exist</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails</exception>
    public static void LoadMultipleChildTables(this DataTable dataset, IEnumerable<string> yamlFilePaths, string schemaPath = null)
    {
        if (dataset == null)
            throw new ArgumentNullException(nameof(dataset));
        if (yamlFilePaths == null)
            throw new ArgumentNullException(nameof(yamlFilePaths));
        
        var loader = string.IsNullOrEmpty(schemaPath) 
            ? new YamlSchemaLoader() 
            : new YamlSchemaLoader(schemaPath);
        
        var yamlContents = new List<string>();
        foreach (var filePath in yamlFilePaths)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"YAML file not found: {filePath}", filePath);
            }
            yamlContents.Add(File.ReadAllText(filePath));
        }
        
        loader.LoadMultipleTables(dataset, yamlContents);
    }
}
