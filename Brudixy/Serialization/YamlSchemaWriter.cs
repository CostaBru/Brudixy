using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.TypeGenerator.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Brudixy.Serialization;

/// <summary>
/// Helper class for writing YAML schemas from CoreDataTable instances
/// </summary>
public class YamlSchemaWriter
{
    private readonly ISerializer _serializer;

    public YamlSchemaWriter()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
            .Build();
    }

    public string WriteSchema(DataTable table)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));

        var schema = new DataTableObj();
        schema.Table = table.Name;
        
        // Populate Columns
        foreach (var col in table.GetColumns())
        {
            var conciseType = GetConciseTypeString(col);
            schema.Columns[col.ColumnName] = conciseType;
            
            // Populate Column Options if they differ from defaults
            if (HasNonDefaultOptions(col, conciseType))
            {
                var options = new ColumnInfo();
                
                // Only set properties that are NOT implied by the concise type string
                // or are explicitly non-default
                
                if (col.IsUnique) options.IsUnique = true;
                if (col.IsAutomaticValue) options.Auto = true;
                if (col.IsServiceColumn) options.IsService = true;
                if (table.HasIndex(col.ColumnName)) options.HasIndex = true;
                
                if (col.DefaultValue != null) options.DefaultValue = col.DefaultValue.ToString();
                // if (!string.IsNullOrEmpty(col.DataExpression)) options.Expression = col.DataExpression;
                
                // MaxLength logic: only if set and relevant
                if (col.MaxLength.HasValue && col.MaxLength.Value > 0)
                {
                    options.MaxLength = col.MaxLength.Value;
                }
                
                // Store options
                schema.ColumnObjects[col.ColumnName] = options;
            }
        }

        // Primary Key
        if (table.PrimaryKeyColumnCount > 0)
        {
            schema.PrimaryKey = table.PrimaryKey.Select(c => c.ColumnName).ToList();
        }

        // Indexes (Multi-column)
        if (table.MultiColumnIndexInfo != null && table.MultiColumnIndexInfo.HasAny)
        {
            var multiColIndexes = table.MultiColumnIndexInfo.Indexes;
            int idxCounter = 1;
            foreach (var index in multiColIndexes)
            {
                var indexName = $"Index{idxCounter++}";
                schema.Indexes[indexName] = new Brudixy.TypeGenerator.Core.Index 
                { 
                    Columns = index.Columns.Select(h => table.GetColumn(h).ColumnName).ToList(),
                    Unique = index.IsUnique
                };
            }
        }
        
        // Return serialized YAML
        return _serializer.Serialize(schema);
    }
    
    private bool HasNonDefaultOptions(CoreDataColumn col, string conciseType)
    {
        // Check if any option is non-default
        if (col.IsUnique) return true;
        if (col.IsAutomaticValue) return true;
        if (col.IsServiceColumn) return true;
        if (col.DefaultValue != null) return true;
        // if (!string.IsNullOrEmpty(col.DataExpression)) return true; // DataExpression not exposed in CoreDataColumn yet
        if (col.MaxLength.HasValue && col.MaxLength.Value > 0) return true;
        
        return false; 
    }

    private string GetConciseTypeString(CoreDataColumn col)
    {
        // Map TableStorageType to string
        string typeStr = col.Type.ToString();
        
        // Handle UserType special case
        if (col.Type == TableStorageType.String && col.DataType != null)
        {
            var userTypeName = col.DataType.Name;
            if (userTypeName != "String" && userTypeName != "Object")
            { 
               typeStr = userTypeName; 
            }
        }

        // Modifiers
        if (col.TypeModifier == TableStorageTypeModifier.Array)
        {
            typeStr += "[]";
        }
        else if (col.TypeModifier == TableStorageTypeModifier.Range)
        {
            typeStr += "<>";
        }
        
        // Nullability
        bool defaultNullable = IsNullableByDefault(col.Type);
        
        if (col.AllowNull && !defaultNullable)
        {
            typeStr += "?";
        }
        else if (!col.AllowNull && defaultNullable)
        {
            typeStr += "!";
        }
        
        return typeStr;
    }
    
    private bool IsNullableByDefault(TableStorageType type)
    {
        return type == TableStorageType.String || type == TableStorageType.Object;
    }
}
