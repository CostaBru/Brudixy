using Brudixy.TypeGenerator.Core.Validation;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    [TestFixture]
    public class ColumnTypeInfoTests
    {
        [Test]
        public void Parse_ConflictingNullabilityModifiers_SetsBothFlags()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("String?!");

            // Assert
            Assert.IsTrue(typeInfo.IsNullable, "Should detect nullable modifier");
            Assert.IsTrue(typeInfo.IsNonNull, "Should detect non-null modifier");
            Assert.IsFalse(typeInfo.IsValid(out var error), "Should be invalid");
            Assert.IsNotNull(error, "Should have error message");
            Assert.That(error, Does.Contain("nullable").And.Contain("non-null"));
        }

        [Test]
        public void Parse_MultipleStructuralModifiers_SetsBothFlags()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("Int32[]<>");

            // Assert
            Assert.IsTrue(typeInfo.IsArray, "Should detect array modifier");
            Assert.IsTrue(typeInfo.IsRange, "Should detect range modifier");
            Assert.IsFalse(typeInfo.IsValid(out var error), "Should be invalid");
            Assert.IsNotNull(error, "Should have error message");
            Assert.That(error, Does.Contain("structural modifier"));
        }

        [Test]
        public void Parse_ArrayRangeAndComplex_SetsAllFlags()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("Int32[]<> | Complex");

            // Assert
            Assert.IsTrue(typeInfo.IsArray, "Should detect array modifier");
            Assert.IsTrue(typeInfo.IsRange, "Should detect range modifier");
            Assert.IsTrue(typeInfo.IsComplex, "Should detect complex modifier");
            Assert.AreEqual(3, typeInfo.StructuralModifierCount, "Should have 3 structural modifiers");
            Assert.IsFalse(typeInfo.IsValid(out var error), "Should be invalid");
            Assert.IsNotNull(error, "Should have error message");
        }

        [Test]
        public void Parse_ValidNullableString_IsValid()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("String?");

            // Assert
            Assert.IsTrue(typeInfo.IsNullable, "Should be nullable");
            Assert.IsFalse(typeInfo.IsNonNull, "Should not be non-null");
            Assert.AreEqual("String", typeInfo.BaseType);
            Assert.IsTrue(typeInfo.IsValid(out _), "Should be valid");
        }

        [Test]
        public void Parse_ValidArray_IsValid()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("Int32[]");

            // Assert
            Assert.IsTrue(typeInfo.IsArray, "Should be array");
            Assert.IsFalse(typeInfo.IsRange, "Should not be range");
            Assert.IsFalse(typeInfo.IsComplex, "Should not be complex");
            Assert.AreEqual("Int32", typeInfo.BaseType);
            Assert.IsTrue(typeInfo.IsValid(out _), "Should be valid");
        }

        [Test]
        public void Parse_StringWithMaxLength_ParsesCorrectly()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("String | 256");

            // Assert
            Assert.AreEqual("String", typeInfo.BaseType);
            Assert.AreEqual(256u, typeInfo.MaxLength);
            Assert.IsTrue(typeInfo.IsValid(out _), "Should be valid");
        }

        [Test]
        public void Parse_WithIndexModifier_ParsesCorrectly()
        {
            // Act
            var typeInfo = ColumnTypeInfo.Parse("String | 256 | Index");

            // Assert
            Assert.AreEqual("String", typeInfo.BaseType);
            Assert.AreEqual(256u, typeInfo.MaxLength);
            Assert.IsTrue(typeInfo.HasIndex, "Should have index");
            Assert.IsTrue(typeInfo.IsValid(out _), "Should be valid");
        }
    }
}
