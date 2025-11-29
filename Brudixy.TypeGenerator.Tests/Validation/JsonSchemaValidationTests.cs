using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NJsonSchema;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Property-based tests for JSON Schema validation behavior.
    /// Tests that the schema correctly validates valid and invalid YAML files.
    /// </summary>
    [TestFixture]
    public class JsonSchemaValidationTests
    {
        private static JsonSchema _schema;
        private static ISerializer _yamlSerializer;
        private static IDeserializer _yamlDeserializer;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load the JSON Schema
            var schemaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "schemas", "brudixy-table-schema.json");
            var schemaJson = File.ReadAllText(schemaPath);
            _schema = JsonSchema.FromJsonAsync(schemaJson).Result;

            // Set up YAML serializer/deserializer
            _yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
        }

        // Feature: json-schema-definition, Property 1: Schema validation completeness
        // Validates: Requirements 1.1, 1.3, 1.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(BrudixYamlGenerators) })]
        public Property ValidYaml_PassesSchemaValidation(ValidBrudixYaml validYaml)
        {
            // Arrange
            var yaml = validYaml.ToYaml(_yamlSerializer);
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            if (errors.Count > 0)
            {
                TestContext.WriteLine($"YAML:\n{yaml}");
                TestContext.WriteLine($"JSON:\n{json}");
            }
            return (errors.Count == 0).Label($"Valid YAML should pass validation. Errors: {string.Join(", ", errors.Select(e => e.ToString()))}");
        }

        // Feature: json-schema-definition, Property 1: Schema validation completeness
        // Validates: Requirements 1.1, 1.3, 1.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(BrudixYamlGenerators) })]
        public Property InvalidYaml_FailsSchemaValidation(InvalidBrudixYaml invalidYaml)
        {
            // Arrange
            var yaml = invalidYaml.ToYaml(_yamlSerializer);
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            return (errors.Count > 0).Label($"Invalid YAML should fail validation. Expected errors but got none.");
        }

        private string YamlToJson(string yaml)
        {
            // Parse YAML using YamlDotNet's parser to preserve types
            using (var reader = new StringReader(yaml))
            {
                var yamlStream = new YamlDotNet.RepresentationModel.YamlStream();
                yamlStream.Load(reader);
                
                var rootNode = (YamlDotNet.RepresentationModel.YamlMappingNode)yamlStream.Documents[0].RootNode;
                var dict = ConvertYamlNode(rootNode);
                
                return Newtonsoft.Json.JsonConvert.SerializeObject(dict);
            }
        }

        private object ConvertYamlNode(YamlDotNet.RepresentationModel.YamlNode node)
        {
            if (node is YamlDotNet.RepresentationModel.YamlScalarNode scalar)
            {
                // Try to parse as boolean
                if (bool.TryParse(scalar.Value, out var boolValue))
                    return boolValue;
                
                // Try to parse as integer
                if (int.TryParse(scalar.Value, out var intValue))
                    return intValue;
                
                // Try to parse as double
                if (double.TryParse(scalar.Value, out var doubleValue))
                    return doubleValue;
                
                // Return as string
                return scalar.Value;
            }
            else if (node is YamlDotNet.RepresentationModel.YamlMappingNode mapping)
            {
                var dict = new Dictionary<string, object>();
                foreach (var entry in mapping.Children)
                {
                    var key = ((YamlDotNet.RepresentationModel.YamlScalarNode)entry.Key).Value;
                    dict[key] = ConvertYamlNode(entry.Value);
                }
                return dict;
            }
            else if (node is YamlDotNet.RepresentationModel.YamlSequenceNode sequence)
            {
                var list = new List<object>();
                foreach (var item in sequence.Children)
                {
                    list.Add(ConvertYamlNode(item));
                }
                return list;
            }
            
            return null;
        }
    }

    /// <summary>
    /// Represents a valid Brudixy YAML structure
    /// </summary>
    public class ValidBrudixYaml
    {
        public string Table { get; set; }
        public Dictionary<string, string> Columns { get; set; }
        public CodeGenerationOptions CodeGenerationOptions { get; set; }
        public Dictionary<string, ColumnOption> ColumnOptions { get; set; }
        public List<string> PrimaryKey { get; set; }
        public Dictionary<string, Relation> Relations { get; set; }
        public Dictionary<string, string> GroupedProperties { get; set; }
        public Dictionary<string, XProperty> XProperties { get; set; }

        public string ToYaml(ISerializer serializer)
        {
            var obj = new Dictionary<string, object>
            {
                ["Table"] = Table,
                ["Columns"] = Columns
            };

            if (CodeGenerationOptions != null)
            {
                var codeGenDict = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(CodeGenerationOptions.Namespace))
                    codeGenDict["Namespace"] = CodeGenerationOptions.Namespace;
                if (!string.IsNullOrEmpty(CodeGenerationOptions.Class))
                    codeGenDict["Class"] = CodeGenerationOptions.Class;
                if (!string.IsNullOrEmpty(CodeGenerationOptions.RowClass))
                    codeGenDict["RowClass"] = CodeGenerationOptions.RowClass;
                if (CodeGenerationOptions.Abstract.HasValue)
                    codeGenDict["Abstract"] = CodeGenerationOptions.Abstract.Value;
                if (CodeGenerationOptions.Sealed.HasValue)
                    codeGenDict["Sealed"] = CodeGenerationOptions.Sealed.Value;
                if (CodeGenerationOptions.ExtraUsing != null && CodeGenerationOptions.ExtraUsing.Count > 0)
                    codeGenDict["ExtraUsing"] = CodeGenerationOptions.ExtraUsing;
                
                if (codeGenDict.Count > 0)
                    obj["CodeGenerationOptions"] = codeGenDict;
            }
            
            if (ColumnOptions != null && ColumnOptions.Count > 0)
                obj["ColumnOptions"] = ColumnOptions;
            if (PrimaryKey != null && PrimaryKey.Count > 0)
                obj["PrimaryKey"] = PrimaryKey;
            if (Relations != null && Relations.Count > 0)
                obj["Relations"] = Relations;
            if (GroupedProperties != null && GroupedProperties.Count > 0)
                obj["GroupedProperties"] = GroupedProperties;
            if (XProperties != null && XProperties.Count > 0)
                obj["XProperties"] = XProperties;

            return serializer.Serialize(obj);
        }
    }

    /// <summary>
    /// Represents an invalid Brudixy YAML structure
    /// </summary>
    public class InvalidBrudixYaml
    {
        public InvalidYamlType Type { get; set; }
        public object Data { get; set; }

        public string ToYaml(ISerializer serializer)
        {
            return serializer.Serialize(Data);
        }
    }

    public enum InvalidYamlType
    {
        MissingTable,
        MissingColumns,
        InvalidColumnType,
        InvalidRelation,
        InvalidCodeGenOptions
    }

    public class CodeGenerationOptions
    {
        public string Namespace { get; set; }
        public string Class { get; set; }
        public string RowClass { get; set; }
        public bool? Abstract { get; set; }
        public bool? Sealed { get; set; }
        public List<string> ExtraUsing { get; set; }
    }

    public class ColumnOption
    {
        public string Type { get; set; }
        public bool? AllowNull { get; set; }
        public bool? IsUnique { get; set; }
        public bool? HasIndex { get; set; }
        public int? MaxLength { get; set; }
        public string EnumType { get; set; }
        public Dictionary<string, XProperty> XProperties { get; set; }
    }

    public class Relation
    {
        public string ParentTable { get; set; }
        public string ChildTable { get; set; }
        public List<string> ParentKey { get; set; }
        public List<string> ChildKey { get; set; }
    }

    public class XProperty
    {
        public string Type { get; set; }
        public string DataType { get; set; }
        public object Value { get; set; }
    }

    /// <summary>
    /// FsCheck generators for Brudixy YAML structures
    /// </summary>
    public static class BrudixYamlGenerators
    {
        private static readonly string[] BuiltInTypes = new[]
        {
            "Int32", "Int64", "String", "DateTime", "Boolean", "Decimal", "Guid"
        };

        private static readonly string[] ValidIdentifiers = new[]
        {
            "Id", "Name", "Email", "CreatedDate", "UserId", "Status", "Description"
        };

        public static Arbitrary<ValidBrudixYaml> ValidBrudixYamlArbitrary()
        {
            var genTableName = Gen.Elements(ValidIdentifiers);
            
            var genColumnType = Gen.Elements(BuiltInTypes)
                .Select(t => Gen.OneOf(
                    Gen.Constant(t),
                    Gen.Constant($"{t}?"),
                    Gen.Constant($"{t} | 256"),
                    Gen.Constant($"{t}[]")
                ))
                .SelectMany(x => x);

            var genColumns = Gen.NonEmptyListOf(Gen.Elements(ValidIdentifiers))
                .Select(names => names.Distinct().Take(5).ToList())
                .SelectMany(names => 
                    Gen.Sequence(names.Select(name => 
                        genColumnType.Select(type => (name, type))))
                    .Select(pairs => pairs.ToDictionary(p => p.name, p => p.type)));

            var genCodeGenOptions = Gen.OneOf(
                Gen.Constant<CodeGenerationOptions>(null),
                Gen.Fresh(() => new CodeGenerationOptions
                {
                    Namespace = "MyApp.Data",
                    Class = "MyTable",
                    RowClass = "MyRow"
                }),
                Gen.Fresh(() => new CodeGenerationOptions
                {
                    Namespace = "MyApp.Data.Tables",
                    Class = "UserTable",
                    RowClass = "UserRow",
                    Sealed = true
                }),
                Gen.Fresh(() => new CodeGenerationOptions
                {
                    Namespace = "MyApp.Core",
                    Class = "BaseTable",
                    RowClass = "BaseRow",
                    Abstract = true,
                    Sealed = false
                })
            );

            var genPrimaryKey = Gen.OneOf(
                Gen.Constant<List<string>>(null),
                Gen.Constant(new List<string> { "Id" })
            );

            var genValidYaml = from table in genTableName
                               from columns in genColumns
                               from codeGen in genCodeGenOptions
                               from pk in genPrimaryKey
                               select new ValidBrudixYaml
                               {
                                   Table = table,
                                   Columns = columns,
                                   CodeGenerationOptions = codeGen,
                                   PrimaryKey = pk
                               };

            return Arb.From(genValidYaml);
        }

        public static Arbitrary<InvalidBrudixYaml> InvalidBrudixYamlArbitrary()
        {
            var genMissingTable = Gen.Fresh(() => new InvalidBrudixYaml
            {
                Type = InvalidYamlType.MissingTable,
                Data = new Dictionary<string, object>
                {
                    ["Columns"] = new Dictionary<string, string> { ["Id"] = "Int32" }
                }
            });

            var genMissingColumns = Gen.Fresh(() => new InvalidBrudixYaml
            {
                Type = InvalidYamlType.MissingColumns,
                Data = new Dictionary<string, object>
                {
                    ["Table"] = "MyTable"
                }
            });

            var genInvalidColumnType = Gen.Fresh(() => new InvalidBrudixYaml
            {
                Type = InvalidYamlType.InvalidColumnType,
                Data = new Dictionary<string, object>
                {
                    ["Table"] = "MyTable",
                    ["Columns"] = new Dictionary<string, object>
                    {
                        ["Id"] = "InvalidType@#$"
                    }
                }
            });

            var genInvalidRelation = Gen.Fresh(() => new InvalidBrudixYaml
            {
                Type = InvalidYamlType.InvalidRelation,
                Data = new Dictionary<string, object>
                {
                    ["Table"] = "MyTable",
                    ["Columns"] = new Dictionary<string, string> { ["Id"] = "Int32" },
                    ["Relations"] = new Dictionary<string, object>
                    {
                        ["FK_Test"] = new Dictionary<string, object>
                        {
                            // Missing required ParentKey and ChildKey
                            ["ParentTable"] = "Parent"
                        }
                    }
                }
            });

            var genInvalidCodeGen = Gen.Fresh(() => new InvalidBrudixYaml
            {
                Type = InvalidYamlType.InvalidCodeGenOptions,
                Data = new Dictionary<string, object>
                {
                    ["Table"] = "MyTable",
                    ["Columns"] = new Dictionary<string, string> { ["Id"] = "Int32" },
                    ["CodeGenerationOptions"] = new Dictionary<string, object>
                    {
                        ["Namespace"] = "123Invalid",  // Invalid namespace
                        ["Class"] = "My Class"  // Invalid identifier with space
                    }
                }
            });

            var genInvalidYaml = Gen.OneOf(
                genMissingTable,
                genMissingColumns,
                genInvalidColumnType,
                genInvalidRelation,
                genInvalidCodeGen
            );

            return Arb.From(genInvalidYaml);
        }
    }
}
