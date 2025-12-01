using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NJsonSchema;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Integration tests for JSON Schema validation with real YAML files.
    /// Tests that the schema correctly validates actual Brudixy YAML files.
    /// </summary>
    [TestFixture]
    public class JsonSchemaIntegrationTests
    {
        private static JsonSchema _schema;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load the JSON Schema
            var schemaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "schemas", "brudixy-table-schema.json");
            var schemaJson = File.ReadAllText(schemaPath);
            _schema = JsonSchema.FromJsonAsync(schemaJson).Result;
        }

        [Test]
        public void TestBaseTable_PassesValidation()
        {
            // Arrange
            var yamlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Brudixy.Tests", "TypedDs", "TestBaseTable.st.brudixy.yaml");
            var yaml = File.ReadAllText(yamlPath);
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            if (errors.Count > 0)
            {
                TestContext.WriteLine($"Validation errors for TestBaseTable.st.brudixy.yaml:");
                foreach (var error in errors)
                {
                    TestContext.WriteLine($"  - {error}");
                }
            }
            Assert.AreEqual(0, errors.Count, $"TestBaseTable.st.brudixy.yaml should pass validation. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void TestGroupTable_PassesValidation()
        {
            // Arrange
            var yamlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Brudixy.Tests", "TypedDs", "TestGroupTable.st.brudixy.yaml");
            var yaml = File.ReadAllText(yamlPath);
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            if (errors.Count > 0)
            {
                TestContext.WriteLine($"Validation errors for TestGroupTable.st.brudixy.yaml:");
                foreach (var error in errors)
                {
                    TestContext.WriteLine($"  - {error}");
                }
            }
            Assert.AreEqual(0, errors.Count, $"TestGroupTable.st.brudixy.yaml should pass validation. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void InvalidYaml_MissingTable_FailsValidation()
        {
            // Arrange
            var yaml = @"
Columns:
  Id: Int32
  Name: String
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML missing required 'Table' property should fail validation");
            Assert.IsTrue(errors.Any(e => e.ToString().Contains("Table")), "Error should mention missing 'Table' property");
        }

        /*[Test]
        public void InvalidYaml_MissingColumns_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML missing required 'Columns' property should fail validation");
            Assert.IsTrue(errors.Any(e => e.ToString().Contains("Columns")), "Error should mention missing 'Columns' property");
        }*/

        [Test]
        public void InvalidYaml_InvalidColumnType_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
Columns:
  Id: InvalidType@#$
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML with invalid column type should fail validation");
        }

        [Test]
        public void InvalidYaml_InvalidRelation_MissingKeys_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
Columns:
  Id: Int32
Relations:
  FK_Test:
    ParentTable: Parent
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML with relation missing required keys should fail validation");
            Assert.IsTrue(errors.Any(e => e.ToString().Contains("ParentKey") || e.ToString().Contains("ChildKey")), 
                "Error should mention missing ParentKey or ChildKey");
        }

        [Test]
        public void InvalidYaml_InvalidNamespace_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
Columns:
  Id: Int32
CodeGenerationOptions:
  Namespace: 123Invalid
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML with invalid namespace should fail validation");
        }

        [Test]
        public void InvalidYaml_InvalidIdentifier_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
Columns:
  Id: Int32
CodeGenerationOptions:
  Class: My Class
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML with invalid identifier (space in name) should fail validation");
        }

        [Test]
        public void InvalidYaml_EmptyParentKey_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
Columns:
  Id: Int32
Relations:
  FK_Test:
    ParentKey: []
    ChildKey:
      - Id
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML with empty ParentKey array should fail validation");
        }

        [Test]
        public void InvalidYaml_InvalidGroupedPropertyFormat_FailsValidation()
        {
            // Arrange
            var yaml = @"
Table: MyTable
Columns:
  X: Int32
  Y: Int32
GroupedProperties:
  Point: X
";
            var json = YamlToJson(yaml);

            // Act
            var errors = _schema.Validate(json);

            // Assert
            Assert.Greater(errors.Count, 0, "YAML with grouped property containing only one column should fail validation");
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
}
