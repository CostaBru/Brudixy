using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NJsonSchema;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Brudixy.Serialization;

/// <summary>
/// Validates YAML content against the brudixy-table-schema.json schema
/// </summary>
public class SchemaValidator
{
    private readonly JsonSchema _schema;
    
    /// <summary>
    /// Initializes a new instance of SchemaValidator using the embedded schema resource
    /// </summary>
    public SchemaValidator()
    {
        var schemaJson = ResourceHelper.GetEmbeddedSchemaJson();
        _schema = JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Initializes a new instance of SchemaValidator using a custom schema file path
    /// </summary>
    /// <param name="schemaFilePath">Path to the JSON schema file</param>
    /// <exception cref="SchemaNotFoundException">Thrown when the schema file cannot be found</exception>
    public SchemaValidator(string schemaFilePath)
    {
        if (!File.Exists(schemaFilePath))
        {
            throw new SchemaNotFoundException(schemaFilePath);
        }
        
        var schemaJson = File.ReadAllText(schemaFilePath);
        _schema = JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Validates YAML content against the JSON schema
    /// </summary>
    /// <param name="yamlContent">The YAML content to validate</param>
    /// <returns>A ValidationResult indicating success or failure with error details</returns>
    public ValidationResult Validate(string yamlContent)
    {
        try
        {
            // Parse YAML using the node API to preserve types
            using var reader = new StringReader(yamlContent);
            var yaml = new YamlStream();
            yaml.Load(reader);
            
            if (yaml.Documents.Count == 0)
            {
                return new ValidationResult { IsValid = true };
            }
            
            // Convert YAML node to JSON, preserving types
            var jsonContent = ConvertYamlNodeToJson(yaml.Documents[0].RootNode);
            
            // Validate against JSON schema
            var errors = _schema.Validate(jsonContent);
            
            if (errors == null || errors.Count == 0)
            {
                return new ValidationResult { IsValid = true };
            }
            
            var validationErrors = errors.Select(e => new ValidationError
            {
                Path = e.Path,
                Message = e.ToString(),
                Kind = e.Kind.ToString()
            }).ToList();
            
            return new ValidationResult
            {
                IsValid = false,
                Errors = validationErrors
            };
        }
        catch (YamlException yamlEx)
        {
            // YAML parsing error
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Path = $"Line {yamlEx.Start.Line}, Column {yamlEx.Start.Column}",
                        Message = yamlEx.Message,
                        Kind = "YamlParsingError"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            // Other validation errors
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Path = "Unknown",
                        Message = ex.Message,
                        Kind = "ValidationError"
                    }
                }
            };
        }
    }
    
    /// <summary>
    /// Converts a YAML node to JSON string, preserving types
    /// </summary>
    private string ConvertYamlNodeToJson(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            var value = scalar.Value;

            if (scalar.Style == ScalarStyle.DoubleQuoted)
            {
                return $"\"{scalar.Value}\"";
            }
            
            // Check if it's null
            if (string.IsNullOrEmpty(value) || value == "null" || value == "~")
                return "null";
            
            // Check if it's a boolean
            if (value == "true" || value == "false")
                return value;
            
            // Check if it's a number (int, double, etc.)
            if (int.TryParse(value, out _) || 
                long.TryParse(value, out _) ||
                double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                return value;
            
            // It's a string - escape and quote it
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }
        
        if (node is YamlMappingNode mapping)
        {
            var pairs = mapping.Children.Select(kvp => 
            {
                var key = ((YamlScalarNode)kvp.Key).Value;
                var value = ConvertYamlNodeToJson(kvp.Value);
                return $"\"{key}\":{value}";
            });
            return "{" + string.Join(",", pairs) + "}";
        }
        
        if (node is YamlSequenceNode sequence)
        {
            var items = sequence.Children.Select(child => ConvertYamlNodeToJson(child));
            return "[" + string.Join(",", items) + "]";
        }
        
        return "null";
    }
    
    /// <summary>
    /// Validates a YAML file against the JSON schema
    /// </summary>
    /// <param name="yamlFilePath">Path to the YAML file to validate</param>
    /// <returns>A ValidationResult indicating success or failure with error details</returns>
    /// <exception cref="FileNotFoundException">Thrown when the YAML file cannot be found</exception>
    public ValidationResult ValidateFromFile(string yamlFilePath)
    {
        if (!File.Exists(yamlFilePath))
        {
            throw new FileNotFoundException($"YAML file not found: {yamlFilePath}", yamlFilePath);
        }
        
        var yamlContent = File.ReadAllText(yamlFilePath);
        return Validate(yamlContent);
    }
    
    /// <summary>
    /// Converts a YAML object to JSON string, preserving types (booleans, numbers, etc.)
    /// </summary>
    private string ConvertToJson(object obj)
    {
        if (obj == null)
            return "null";
            
        if (obj is bool b)
            return b ? "true" : "false";
            
        // Handle all numeric types (including uint, ushort, ulong, byte, sbyte, short)
        if (obj is int || obj is long || obj is double || obj is decimal || obj is float ||
            obj is uint || obj is ulong || obj is ushort || obj is byte || obj is sbyte || obj is short)
            return obj.ToString();
            
        if (obj is string s)
            return $"\"{s.Replace("\"", "\\\"")}\"";
            
        if (obj is Dictionary<object, object> dict)
        {
            var pairs = dict.Select(kvp => $"\"{kvp.Key}\":{ConvertToJson(kvp.Value)}");
            return "{" + string.Join(",", pairs) + "}";
        }
        
        if (obj is List<object> list)
        {
            var items = list.Select(item => ConvertToJson(item));
            return "[" + string.Join(",", items) + "]";
        }
        
        // Fallback for other types
        return $"\"{obj}\"";
    }
}
