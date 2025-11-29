using System;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Property-based tests for JSON Schema C# identifier validation.
    /// Tests the regex patterns defined in the JSON Schema for C# identifiers and namespaces.
    /// </summary>
    [TestFixture]
    public class JsonSchemaCSharpIdentifierTests
    {
        // The regex patterns from the JSON Schema definition
        private const string CSharpIdentifierPattern = @"^[A-Za-z_][A-Za-z0-9_]*$";
        private const string CSharpNamespacePattern = @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$";
        
        private static readonly Regex IdentifierRegex = new Regex(CSharpIdentifierPattern, RegexOptions.Compiled);
        private static readonly Regex NamespaceRegex = new Regex(CSharpNamespacePattern, RegexOptions.Compiled);

        // Feature: json-schema-definition, Property 3: C# identifier validation
        // Validates: Requirements 4.1, 4.2
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(CSharpIdentifierGenerators) })]
        public Property ValidIdentifiers_MatchPattern(ValidCSharpIdentifier validIdentifier)
        {
            // Act
            var matches = IdentifierRegex.IsMatch(validIdentifier.Value);

            // Assert
            return matches.Label($"Valid C# identifier '{validIdentifier.Value}' should match pattern");
        }

        // Feature: json-schema-definition, Property 3: C# identifier validation
        // Validates: Requirements 4.1, 4.2
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(CSharpIdentifierGenerators) })]
        public Property InvalidIdentifiers_DoNotMatchPattern(InvalidCSharpIdentifier invalidIdentifier)
        {
            // Act
            var matches = IdentifierRegex.IsMatch(invalidIdentifier.Value);

            // Assert
            return (!matches).Label($"Invalid C# identifier '{invalidIdentifier.Value}' should not match pattern");
        }

        // Feature: json-schema-definition, Property 3: C# identifier validation
        // Validates: Requirements 4.1, 4.2
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(CSharpIdentifierGenerators) })]
        public Property ValidNamespaces_MatchPattern(ValidCSharpNamespace validNamespace)
        {
            // Act
            var matches = NamespaceRegex.IsMatch(validNamespace.Value);

            // Assert
            return matches.Label($"Valid C# namespace '{validNamespace.Value}' should match pattern");
        }

        // Feature: json-schema-definition, Property 3: C# identifier validation
        // Validates: Requirements 4.1, 4.2
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(CSharpIdentifierGenerators) })]
        public Property InvalidNamespaces_DoNotMatchPattern(InvalidCSharpNamespace invalidNamespace)
        {
            // Act
            var matches = NamespaceRegex.IsMatch(invalidNamespace.Value);

            // Assert
            return (!matches).Label($"Invalid C# namespace '{invalidNamespace.Value}' should not match pattern");
        }

        [Test]
        public void ValidIdentifiers_MatchPattern()
        {
            var validIdentifiers = new[]
            {
                "MyClass",
                "myVariable",
                "_privateField",
                "value1",
                "MyClass123",
                "_",
                "__temp",
                "a",
                "A",
                "MyTableClass",
                "MyRowClass",
                "AppendRow"
            };

            foreach (var identifier in validIdentifiers)
            {
                Assert.IsTrue(IdentifierRegex.IsMatch(identifier), $"Valid identifier '{identifier}' should match");
            }
        }

        [Test]
        public void InvalidIdentifiers_DoNotMatchPattern()
        {
            // Note: This test validates syntactic structure only, not semantic correctness.
            // C# keywords like "class", "int", etc. are syntactically valid identifiers
            // and will match the pattern. Semantic validation (keyword checking) is the
            // responsibility of the C# compiler, not the JSON Schema.
            var invalidIdentifiers = new[]
            {
                "",                  // Empty
                " ",                 // Whitespace
                "123Class",          // Starts with number
                "My Class",          // Contains space
                "My-Class",          // Contains hyphen
                "My.Class",          // Contains dot
                "@class",            // Verbatim identifier (not supported by pattern)
                "My@Class",          // Contains @
                "My#Class",          // Contains #
                "My$Class",          // Contains $
            };

            foreach (var identifier in invalidIdentifiers)
            {
                Assert.IsFalse(IdentifierRegex.IsMatch(identifier), $"Invalid identifier '{identifier}' should not match");
            }
        }

        [Test]
        public void ValidNamespaces_MatchPattern()
        {
            var validNamespaces = new[]
            {
                "MyApp",
                "MyApp.Data",
                "MyApp.Data.Tables",
                "System.Collections.Generic",
                "_MyApp._Data",
                "A.B.C.D.E",
                "MyCompany.MyProduct.MyModule",
            };

            foreach (var ns in validNamespaces)
            {
                Assert.IsTrue(NamespaceRegex.IsMatch(ns), $"Valid namespace '{ns}' should match");
            }
        }

        [Test]
        public void InvalidNamespaces_DoNotMatchPattern()
        {
            var invalidNamespaces = new[]
            {
                "",                      // Empty
                " ",                     // Whitespace
                ".",                     // Just dot
                ".MyApp",                // Starts with dot
                "MyApp.",                // Ends with dot
                "MyApp..Data",           // Double dot
                "123App.Data",           // Starts with number
                "MyApp.123Data",         // Segment starts with number
                "My App.Data",           // Contains space
                "MyApp.Data.Tables ",    // Trailing space
                " MyApp.Data",           // Leading space
                "MyApp-Data",            // Contains hyphen
            };

            foreach (var ns in invalidNamespaces)
            {
                Assert.IsFalse(NamespaceRegex.IsMatch(ns), $"Invalid namespace '{ns}' should not match");
            }
        }
    }

    /// <summary>
    /// Wrapper for valid C# identifiers
    /// </summary>
    public class ValidCSharpIdentifier
    {
        public string Value { get; set; }

        public ValidCSharpIdentifier(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper for invalid C# identifiers
    /// </summary>
    public class InvalidCSharpIdentifier
    {
        public string Value { get; set; }

        public InvalidCSharpIdentifier(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper for valid C# namespaces
    /// </summary>
    public class ValidCSharpNamespace
    {
        public string Value { get; set; }

        public ValidCSharpNamespace(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper for invalid C# namespaces
    /// </summary>
    public class InvalidCSharpNamespace
    {
        public string Value { get; set; }

        public InvalidCSharpNamespace(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// FsCheck generators for C# identifiers and namespaces
    /// </summary>
    public static class CSharpIdentifierGenerators
    {
        private static readonly char[] ValidFirstChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_".ToCharArray();
        private static readonly char[] ValidSubsequentChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_".ToCharArray();

        public static Arbitrary<ValidCSharpIdentifier> ValidCSharpIdentifierArbitrary()
        {
            var genFirstChar = Gen.Elements(ValidFirstChars);
            var genSubsequentChars = Gen.ArrayOf(Gen.Elements(ValidSubsequentChars));

            var genValidIdentifier = from first in genFirstChar
                                     from rest in genSubsequentChars
                                     select new ValidCSharpIdentifier($"{first}{new string(rest)}");

            return Arb.From(genValidIdentifier);
        }

        public static Arbitrary<InvalidCSharpIdentifier> InvalidCSharpIdentifierArbitrary()
        {
            var genEmpty = Gen.Constant("");
            var genWhitespace = Gen.Constant("   ");
            var genStartsWithNumber = Gen.Constant("123Class");
            var genContainsSpace = Gen.Constant("My Class");
            var genContainsHyphen = Gen.Constant("My-Class");
            var genContainsDot = Gen.Constant("My.Class");
            var genContainsAt = Gen.Constant("My@Class");
            var genContainsHash = Gen.Constant("My#Class");
            var genContainsDollar = Gen.Constant("My$Class");
            var genOnlyNumbers = Gen.Constant("123");
            var genSpecialChars = Gen.Constant("!@#$%");

            var genInvalidIdentifier = Gen.OneOf(
                genEmpty, genWhitespace, genStartsWithNumber, genContainsSpace,
                genContainsHyphen, genContainsDot, genContainsAt, genContainsHash,
                genContainsDollar, genOnlyNumbers, genSpecialChars
            );

            return Arb.From(genInvalidIdentifier.Select(s => new InvalidCSharpIdentifier(s)));
        }

        public static Arbitrary<ValidCSharpNamespace> ValidCSharpNamespaceArbitrary()
        {
            var genFirstChar = Gen.Elements(ValidFirstChars);
            var genSubsequentChars = Gen.ArrayOf(Gen.Elements(ValidSubsequentChars));

            var genIdentifier = from first in genFirstChar
                                from rest in genSubsequentChars
                                where rest.Length < 20  // Keep identifiers reasonable length
                                select $"{first}{new string(rest)}";

            var genNamespace = from count in Gen.Choose(1, 5)  // 1 to 5 segments
                               from segments in Gen.ArrayOf(count, genIdentifier)
                               where segments.Length > 0
                               select new ValidCSharpNamespace(string.Join(".", segments));

            return Arb.From(genNamespace);
        }

        public static Arbitrary<InvalidCSharpNamespace> InvalidCSharpNamespaceArbitrary()
        {
            var genEmpty = Gen.Constant("");
            var genWhitespace = Gen.Constant("   ");
            var genJustDot = Gen.Constant(".");
            var genStartsWithDot = Gen.Constant(".MyApp");
            var genEndsWithDot = Gen.Constant("MyApp.");
            var genDoubleDot = Gen.Constant("MyApp..Data");
            var genStartsWithNumber = Gen.Constant("123App.Data");
            var genSegmentStartsWithNumber = Gen.Constant("MyApp.123Data");
            var genContainsSpace = Gen.Constant("My App.Data");
            var genTrailingSpace = Gen.Constant("MyApp.Data ");
            var genLeadingSpace = Gen.Constant(" MyApp.Data");
            var genContainsHyphen = Gen.Constant("MyApp-Data");

            var genInvalidNamespace = Gen.OneOf(
                genEmpty, genWhitespace, genJustDot, genStartsWithDot,
                genEndsWithDot, genDoubleDot, genStartsWithNumber,
                genSegmentStartsWithNumber, genContainsSpace, genTrailingSpace,
                genLeadingSpace, genContainsHyphen
            );

            return Arb.From(genInvalidNamespace.Select(s => new InvalidCSharpNamespace(s)));
        }
    }
}
