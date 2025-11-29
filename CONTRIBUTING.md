# Contributing to Brudixy

Thank you for your interest in contributing to Brudixy! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please be respectful and professional in all interactions. We aim to foster an inclusive and welcoming community.

## How to Contribute

### Reporting Issues

- Check existing issues before creating a new one
- Provide a clear title and description
- Include steps to reproduce the issue
- Specify your environment (OS, .NET version, etc.)
- Include relevant code snippets or error messages

### Submitting Changes

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following the coding standards below
3. **Add tests** for any new functionality
4. **Run all tests** to ensure nothing broke
5. **Update documentation** if needed
6. **Submit a pull request** with a clear description

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, JetBrains Rider, or VS Code

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/brudixy.git
cd brudixy

# Restore dependencies
dotnet restore Brudixy.sln

# Build the solution
dotnet build Brudixy.sln

# Run tests
dotnet test Brudixy.sln
```

## Coding Standards

### General Guidelines

- Follow existing code style and conventions
- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation comments for public APIs
- Avoid unnecessary dependencies

### C# Conventions

- Use `PascalCase` for class names, method names, and properties
- Use `camelCase` for local variables and parameters
- Use `_camelCase` for private fields
- Prefer `var` when the type is obvious
- Use modern C# features appropriately

### Performance Considerations

- Be mindful of allocations in hot paths
- Use `Span<T>` and `Memory<T>` where appropriate
- Benchmark performance-critical code changes
- Document any performance implications

## Testing

- Write unit tests for all new functionality
- Maintain or improve code coverage
- Use descriptive test names that explain the scenario
- Include edge cases and error conditions
- Performance benchmarks should use BenchmarkDotNet

### Test Structure

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var sut = CreateSystemUnderTest();
    
    // Act
    var result = sut.DoSomething();
    
    // Assert
    Assert.That(result, Is.EqualTo(expected));
}
```

## Documentation

- Update README.md for significant changes
- Add XML documentation for public APIs
- Include code examples for new features
- Keep CHANGELOG.md updated

## Pull Request Process

1. Update the README.md with details of changes if applicable
2. Ensure all tests pass and code builds without warnings
3. Request review from maintainers
4. Address any feedback or requested changes
5. Once approved, your PR will be merged

## Performance Benchmarks

When adding benchmarks:

- Use BenchmarkDotNet
- Include baseline comparisons
- Document benchmark results in PR
- Place benchmarks in `Brudixy.Tests/Benchmarks/`

## Source Generator Development

When working on source generators:

- Target `netstandard2.0` for analyzer/generator projects
- Test with multiple Visual Studio/Roslyn versions
- Provide clear error messages
- Document the generated code structure
- Include example schemas

## JSON Schema Store Submission

The Brudixy Table Schema is registered with the [JSON Schema Store](https://www.schemastore.org/) to provide automatic schema association in IDEs. If you need to update the schema registration:

### Prerequisites

- The schema file must be publicly accessible via a stable URL
- The schema must be valid JSON Schema Draft 7 or later
- Test the schema with real YAML files before submission

### Submission Process

1. **Prepare the Schema**
   - Ensure `schemas/brudixy-table-schema.json` is up to date
   - Validate the schema structure using JSON Schema validators
   - Test with example YAML files in `Brudixy.Tests/TypedDs/`
   - Run all schema validation tests: `dotnet test --filter "FullyQualifiedName~JsonSchema"`

2. **Update Catalog Entry**
   - The catalog entry is maintained in `schemas/catalog-entry.json`
   - Verify the file patterns match the supported extensions:
     - `*.st.brudixy.yaml` - Single table definitions
     - `*.dt.brudixy.yaml` - Data table definitions
     - `*.ds.brudixy.yaml` - Dataset definitions
   - Ensure the URL points to the raw GitHub content

3. **Fork and Clone Schema Store Repository**
   ```bash
   # Fork the repository on GitHub first
   git clone https://github.com/YOUR-USERNAME/schemastore.git
   cd schemastore
   git remote add upstream https://github.com/SchemaStore/schemastore.git
   ```

4. **Add Schema to Schema Store**
   ```bash
   # Create a new branch
   git checkout -b add-brudixy-schema
   
   # Copy the schema file
   cp /path/to/brudixy/schemas/brudixy-table-schema.json src/schemas/json/brudixy-table-schema.json
   ```

5. **Update Catalog**
   - Open `src/api/json/catalog.json`
   - Add the catalog entry from `schemas/catalog-entry.json` to the `schemas` array
   - Maintain alphabetical order by schema name
   - Example entry:
   ```json
   {
     "name": "Brudixy Table Schema",
     "description": "Schema for Brudixy TypeGenerator YAML files that define table structure, columns, relations, indexes, grouped properties, and code generation options",
     "fileMatch": [
       "*.st.brudixy.yaml",
       "*.dt.brudixy.yaml",
       "*.ds.brudixy.yaml"
     ],
     "url": "https://raw.githubusercontent.com/brudixy/brudixy/main/schemas/brudixy-table-schema.json"
   }
   ```

6. **Test the Changes**
   ```bash
   # Run schema store tests
   npm install
   npm test
   ```

7. **Commit and Push**
   ```bash
   git add src/schemas/json/brudixy-table-schema.json src/api/json/catalog.json
   git commit -m "Add Brudixy Table Schema for TypeGenerator YAML files"
   git push origin add-brudixy-schema
   ```

8. **Create Pull Request**
   - Go to your fork on GitHub
   - Click "New Pull Request"
   - Use the pull request template (see below)
   - Wait for review and address any feedback

### Pull Request Template

Use this template when submitting to the JSON Schema Store:

```markdown
## Schema Information

**Schema Name**: Brudixy Table Schema

**Description**: JSON Schema for Brudixy TypeGenerator YAML files that define table structure, columns, relations, indexes, grouped properties, and code generation options.

**File Patterns**:
- `*.st.brudixy.yaml` - Single table definitions
- `*.dt.brudixy.yaml` - Data table definitions  
- `*.ds.brudixy.yaml` - Dataset definitions

**Schema URL**: https://raw.githubusercontent.com/brudixy/brudixy/main/schemas/brudixy-table-schema.json

**Project Repository**: https://github.com/brudixy/brudixy

## Testing Checklist

- [x] Schema is valid JSON Schema Draft 7
- [x] Schema validates successfully against test files
- [x] All required properties are documented
- [x] Examples are included and valid
- [x] File patterns are correct and tested
- [x] Schema URL is publicly accessible
- [x] Schema has been tested in VS Code with YAML extension
- [x] Schema has been tested in JetBrains IDEs
- [x] All schema validation tests pass

## Example Files

Example YAML files that validate against this schema:

### Minimal Table Example
```yaml
Table: Users
Columns:
  Id: Int32
  Name: String | 256
  Email: String | 512
```

### Complete Example
```yaml
Table: Products
CodeGenerationOptions:
  Namespace: MyApp.Data.Tables
  Class: ProductTable
  RowClass: ProductRow
  Sealed: true

PrimaryKey:
  - Id

Columns:
  Id: Int32
  Name: String | 256 | Index
  Price: Decimal
  CategoryId: Int32
  CreatedDate: DateTime!

ColumnOptions:
  Id:
    IsUnique: true
    Auto: true

Relations:
  FK_Product_Category:
    ParentTable: Categories
    ParentKey:
      - Id
    ChildKey:
      - CategoryId

GroupedProperties:
  AuditInfo: CreatedDate|ModifiedDate

GroupedPropertyOptions:
  AuditInfo:
    Type: Tuple
    IsReadOnly: true

XProperties:
  DisplayOrder:
    Type: Int32
    Value: 1
```

## Additional Information

Brudixy is a TypeGenerator that creates strongly-typed C# table and row classes from YAML schema definitions. The schema provides IDE-level validation, autocomplete, and documentation for developers writing Brudixy table definitions.

The schema includes comprehensive validation for:
- Column type syntax with modifiers (nullable, arrays, ranges)
- C# identifier validation (namespaces, class names, properties)
- Relation structures (parent/child foreign keys)
- Grouped properties (tuples and structs)
- Extended properties (XProperties)
- Code generation options

Documentation and examples are available in the repository at `schemas/README.md`.
```

### Updating an Existing Schema

If the schema needs to be updated after initial submission:

1. Update `schemas/brudixy-table-schema.json` in the Brudixy repository
2. Increment the version appropriately (see Versioning below)
3. Test thoroughly with existing YAML files
4. The Schema Store will automatically pick up changes from the GitHub URL
5. For major breaking changes, consider creating a versioned schema file

### Versioning

- **Patch updates** (documentation, bug fixes): Update in place
- **Minor updates** (new properties, non-breaking): Update in place, document in CHANGELOG
- **Major updates** (breaking changes): Consider creating `schemas/v2.0.0/brudixy-table-schema.json`

### Testing Before Submission

```bash
# Run all schema validation tests
dotnet test Brudixy.TypeGenerator.Tests --filter "FullyQualifiedName~JsonSchema"

# Test with real YAML files
# Open Brudixy.Tests/TypedDs/TestBaseTable.st.brudixy.yaml in VS Code
# Verify validation and autocomplete work correctly
```

### Troubleshooting

**Schema Store PR rejected**:
- Ensure schema is valid JSON Schema Draft 7
- Check that file patterns don't conflict with existing schemas
- Verify the schema URL is publicly accessible
- Make sure examples validate against the schema

**Schema not working after submission**:
- Clear IDE caches and reload
- Check that the GitHub URL is accessible
- Verify file patterns match your YAML files
- Try using the inline directive as a fallback

## Release Process

Maintainers will handle releases:

1. Update version numbers in .csproj files
2. Update CHANGELOG.md
3. Create a version tag (e.g., `v1.0.0`)
4. GitHub Actions will build and publish NuGet packages

## Questions?

Feel free to open an issue for questions or clarifications about contributing.

## License

By contributing to Brudixy, you agree that your contributions will be licensed under the MIT License.
