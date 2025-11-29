using System;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Property-based tests for JSON Schema column type pattern validation.
    /// Tests the regex pattern defined in the JSON Schema for column types.
    /// </summary>
    [TestFixture]
    public class JsonSchemaColumnTypePatternTests
    {
        // The regex pattern from the JSON Schema definition
        private const string ColumnTypePattern = @"^(\([A-Za-z_][A-Za-z0-9_.]*\)|[A-Za-z_][A-Za-z0-9_.]*)[?!]?(\[\]|<>)?( \| [A-Za-z0-9_ ]+)*$";
        private static readonly Regex ColumnTypeRegex = new Regex(ColumnTypePattern, RegexOptions.Compiled);

        // Feature: json-schema-definition, Property 2: Column type pattern matching
        // Validates: Requirements 2.1, 2.2, 2.3
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(ColumnTypeGenerators) })]
        public Property ValidColumnTypes_MatchPattern(ValidColumnType validType)
        {
            // Act
            var matches = ColumnTypeRegex.IsMatch(validType.Value);

            // Assert
            return matches.Label($"Valid column type '{validType.Value}' should match pattern");
        }

        // Feature: json-schema-definition, Property 2: Column type pattern matching
        // Validates: Requirements 2.1, 2.2, 2.3
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(ColumnTypeGenerators) })]
        public Property InvalidColumnTypes_DoNotMatchPattern(InvalidColumnType invalidType)
        {
            // Act
            var matches = ColumnTypeRegex.IsMatch(invalidType.Value);

            // Assert
            return (!matches).Label($"Invalid column type '{invalidType.Value}' should not match pattern");
        }

        [Test]
        public void BuiltInTypes_MatchPattern()
        {
            var builtInTypes = new[]
            {
                "Int32", "Int64", "UInt32", "UInt64", "Int16", "UInt16",
                "Byte", "SByte", "Single", "Double", "Decimal",
                "DateTime", "DateTimeOffset", "TimeSpan",
                "String", "Char", "Boolean", "Guid", "Object"
            };

            foreach (var type in builtInTypes)
            {
                Assert.IsTrue(ColumnTypeRegex.IsMatch(type), $"Built-in type '{type}' should match");
            }
        }

        [Test]
        public void TypesWithModifiers_MatchPattern()
        {
            var typesWithModifiers = new[]
            {
                "String?",           // Nullable
                "String!",           // Non-null
                "Int32[]",           // Array
                "Int32<>",           // Range
                "String?[]",         // Nullable array
                "DateTime!<>",       // Non-null range
                "(Point2D)",         // Struct
                "(Point2D)?",        // Nullable struct
                "(MyStruct)[]",      // Struct array
            };

            foreach (var type in typesWithModifiers)
            {
                Assert.IsTrue(ColumnTypeRegex.IsMatch(type), $"Type with modifiers '{type}' should match");
            }
        }

        [Test]
        public void TypesWithOptions_MatchPattern()
        {
            var typesWithOptions = new[]
            {
                "String | 256",
                "String | 256 | Index",
                "String | 256 | Index | Unique",
                "Int32 | Auto",
                "String | Service",
                "String | Nullable",
                "String | Not null",
                "MyClass | Complex",
                "MyClass | Class",
                "String? | 256 | Index",
            };

            foreach (var type in typesWithOptions)
            {
                Assert.IsTrue(ColumnTypeRegex.IsMatch(type), $"Type with options '{type}' should match");
            }
        }

        [Test]
        public void InvalidTypes_DoNotMatchPattern()
        {
            var invalidTypes = new[]
            {
                "",                  // Empty
                " ",                 // Whitespace only
                "123Type",           // Starts with number
                "Type Name",         // Space in type name (without pipe)
                "String??",          // Double nullable
                "String!!",          // Double non-null
                "String[][]",        // Double array
                "String<><>",        // Double range
                "String|256",        // Missing space before pipe
                "String | ",         // Trailing pipe
                "| String",          // Leading pipe
                "String | | Index",  // Empty option
            };

            foreach (var type in invalidTypes)
            {
                Assert.IsFalse(ColumnTypeRegex.IsMatch(type), $"Invalid type '{type}' should not match");
            }
        }
    }

    /// <summary>
    /// Wrapper for valid column types
    /// </summary>
    public class ValidColumnType
    {
        public string Value { get; set; }

        public ValidColumnType(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper for invalid column types
    /// </summary>
    public class InvalidColumnType
    {
        public string Value { get; set; }

        public InvalidColumnType(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// FsCheck generators for column type strings
    /// </summary>
    public static class ColumnTypeGenerators
    {
        private static readonly string[] BuiltInTypes = new[]
        {
            "Int32", "Int64", "UInt32", "UInt64", "Int16", "UInt16",
            "Byte", "SByte", "Single", "Double", "Decimal",
            "DateTime", "DateTimeOffset", "TimeSpan",
            "String", "Char", "Boolean", "Guid", "Object"
        };

        private static readonly string[] NullabilityModifiers = new[] { "", "?", "!" };
        private static readonly string[] StructuralModifiers = new[] { "", "[]", "<>" };
        private static readonly string[] Options = new[]
        {
            "", " | 256", " | Index", " | Unique", " | Complex", " | Class",
            " | Auto", " | Service", " | Nullable", " | Not null",
            " | 256 | Index", " | 256 | Index | Unique"
        };

        public static Arbitrary<ValidColumnType> ValidColumnTypeArbitrary()
        {
            var genBuiltInType = Gen.Elements(BuiltInTypes);
            var genUserType = Gen.Elements("MyClass", "MyStruct", "Point2D", "CustomType");
            var genStructType = genUserType.Select(t => $"({t})");
            
            var genBaseType = Gen.OneOf(genBuiltInType, genUserType, genStructType);
            var genNullability = Gen.Elements(NullabilityModifiers);
            var genStructural = Gen.Elements(StructuralModifiers);
            var genOptions = Gen.Elements(Options);

            var genValidType = from baseType in genBaseType
                               from nullability in genNullability
                               from structural in genStructural
                               from options in genOptions
                               select new ValidColumnType($"{baseType}{nullability}{structural}{options}");

            return Arb.From(genValidType);
        }

        public static Arbitrary<InvalidColumnType> InvalidColumnTypeArbitrary()
        {
            var genEmpty = Gen.Constant("");
            var genWhitespace = Gen.Constant("   ");
            var genStartsWithNumber = Gen.Constant("123Type");
            var genSpaceInName = Gen.Constant("Type Name");
            var genDoubleNullable = Gen.Constant("String??");
            var genDoubleNonNull = Gen.Constant("String!!");
            var genDoubleArray = Gen.Constant("String[][]");
            var genDoubleRange = Gen.Constant("String<><>");
            var genMissingSpaceBeforePipe = Gen.Constant("String|256");
            var genTrailingPipe = Gen.Constant("String | ");
            var genLeadingPipe = Gen.Constant("| String");
            var genEmptyOption = Gen.Constant("String | | Index");
            var genInvalidChars = Gen.Constant("String@#$");
            var genMismatchedParens = Gen.Constant("(String");

            var genInvalidType = Gen.OneOf(
                genEmpty, genWhitespace, genStartsWithNumber, genSpaceInName,
                genDoubleNullable, genDoubleNonNull, genDoubleArray, genDoubleRange,
                genMissingSpaceBeforePipe, genTrailingPipe, genLeadingPipe,
                genEmptyOption, genInvalidChars, genMismatchedParens
            );

            return Arb.From(genInvalidType.Select(s => new InvalidColumnType(s)));
        }
    }
}
