using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NJsonSchema;
using YamlDotNet.Core;
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
            // First, parse YAML to ensure it's valid YAML
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
            
            var yamlObject = deserializer.Deserialize(new StringReader(yamlContent));
            
            // Convert YAML to JSON for schema validation
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();
            
            var jsonContent = serializer.Serialize(yamlObject);
            
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
}
