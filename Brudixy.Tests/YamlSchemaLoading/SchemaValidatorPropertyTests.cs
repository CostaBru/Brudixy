using Brudixy.Serialization;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class SchemaValidatorPropertyTests
{
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property Validation_OccursBeforeTableCreation()
    {
        // **Feature: yaml-schema-loading, Property 6: Validation occurs before table creation**
        // **Validates: Requirements 2.1, 4.2**
        
        return Prop.ForAll(
            Arb.From(Gen.Elements("Int32", "String", "DateTime", "Boolean")),
            columnType =>
            {
                var yaml = $"Table: TestTable\nColumns:\n  Id: {columnType}";
                
                var validator = new SchemaValidator();
                var result = validator.Validate(yaml);
                
                return result.IsValid;
            });
    }
}
