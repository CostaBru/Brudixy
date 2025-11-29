# Design Document

## Overview

This design document describes a YAML schema validation system for the Brudixy TypeGenerator. The system will validate `.brudixy.yaml` schema files before code generation, ensuring structural correctness, type safety, and constraint validity. The validation system will be integrated into the existing TypeGenerator pipeline and provide detailed error reporting to developers.

## Architecture

The validation system follows a rule-based architecture with three main layers:

1. **Schema Loading Layer**: Responsible for loading YAML files and deserializing them into DataTableObj instances
2. **Validation Engine Layer**: Orchestrates validation rules and collects errors/warnings
3. **Validation Rules Layer**: Individual validators that check specific aspects of the schema

```
┌─────────────────────────────────────────┐
│      TypeGenerator (Entry Point)        │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│      YamlSchemaReader (Loading)         │
│  - Deserialize YAML to DataTableObj     │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│    SchemaValidationEngine (Core)        │
│  - Orchestrate validation rules         │
│  - Collect errors and warnings          │
│  - Report results                       │
└──────────────────┬──────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
┌───────▼────────┐   ┌────────▼──────────┐
│ Validation     │   │  Validation       │
│ Rules          │   │  Context          │
│ - Column       │   │  - DataTableObj   │
│ - Relation     │   │  - File path      │
│ - Index        │   │  - Base tables    │
│ - CodeGen      │   │                   │
└────────────────┘   └───────────────────┘
```

## Components and Interfaces

### 1. ValidationResult

Represents the outcome of validation with errors and warnings.

```csharp
public class ValidationResult
{
    public bool IsValid { get; }
    public List<ValidationError> Errors { get; }
    public List<ValidationWarning> Warnings { get; }
    
    public void AddError(ValidationError error);
    public void AddWarning(ValidationWarning warning);
}

public class ValidationError
{
    public string FilePath { get; set; }
    public string PropertyPath { get; set; }
    public string Message { get; set; }
    public string SuggestedFix { get; set; }
}

public class ValidationWarning
{
    public string FilePath { get; set; }
    public string PropertyPath { get; set; }
    public string Message { get; set; }
}
```

### 2. ValidationContext

Provides context information to validation rules.

```csharp
public class ValidationContext
{
    public DataTableObj Table { get; }
    public string FilePath { get; }
    public Dictionary<string, DataTableObj> LoadedBaseTables { get; }
    public IFileSystemAccessor FileSystem { get; }
    
    public ValidationContext(DataTableObj table, string filePath, 
        IFileSystemAccessor fileSystem);
}
```

### 3. IValidationRule

Interface for all validation rules.

```csharp
public interface IValidationRule
{
    string RuleName { get; }
    void Validate(ValidationContext context, ValidationResult result);
}
```

### 4. SchemaValidationEngine

Core validation orchestrator.

```csharp
public class SchemaValidationEngine
{
    private readonly List<IValidationRule> _rules;
    
    public SchemaValidationEngine();
    public void RegisterRule(IValidationRule rule);
    public ValidationResult Validate(ValidationContext context);
}
```

### 5. Enhanced YamlSchemaReader

Extended to support validation.

```csharp
public class YamlSchemaReader : ISchemaReader
{
    private readonly IDeserializer _deserializer;
    private readonly SchemaValidationEngine _validator;
    
    public DataTableObj GetTable(string content, string filePath, 
        bool validate = true);
    public ValidationResult ValidateTable(DataTableObj table, 
        string filePath);
}
```

## Data Models

### Validation Error Categories

Errors are categorized for better organization:

- **StructuralErrors**: Missing required fields, invalid YAML structure
- **TypeErrors**: Invalid column types, type modifier errors
- **ConstraintErrors**: Invalid primary keys, index definitions
- **RelationErrors**: Invalid foreign key definitions
- **CodeGenErrors**: Invalid namespaces, class names, or generation options
- **ReferenceErrors**: Missing base tables, non-existent column references

### Column Type Validation Model

```csharp
public class ColumnTypeInfo
{
    public string BaseType { get; set; }
    public bool IsNullable { get; set; }
    public bool IsNonNull { get; set; }
    public bool IsArray { get; set; }
    public bool IsRange { get; set; }
    public bool IsComplex { get; set; }
    public bool IsUserType { get; set; }
    public uint? MaxLength { get; set; }
    
    public static ColumnTypeInfo Parse(string typeString);
    public bool IsValid(out string error);
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Validation completeness
*For any* DataTableObj instance, running validation should execute all registered validation rules and return a complete ValidationResult containing all errors and warnings found
**Validates: Requirements 1.1, 1.3, 1.4, 9.3**

### Property 2: Column reference existence
*For any* column reference (in PrimaryKey, Relations, Indexes, or GroupedProperties), the referenced column name must exist in either the Columns definition or inherited columns from base tables
**Validates: Requirements 3.2, 5.1, 5.3, 6.1, 6.5**

### Property 3: Column type validity
*For any* column type specification, it must either match a known built-in type or follow the user-defined type pattern, and any type modifiers (nullable `?`, non-null `!`, array `[]`, range `<>`, complex) must be syntactically correct with at most one structural modifier (array, range, or complex)
**Validates: Requirements 2.1, 2.2, 2.4**

### Property 4: Type constraint consistency
*For any* column definition, if the column is in PrimaryKey or has IsUnique set to true, then AllowNull must be false or the type must use the non-null modifier `!`
**Validates: Requirements 2.3, 5.2**

### Property 5: Relation key symmetry
*For any* relation definition, the ParentKey and ChildKey arrays must both be non-empty and have equal length
**Validates: Requirements 3.1, 3.3**

### Property 6: Code generation option validity
*For any* CodeGenerationOptions, if Abstract is true then Sealed must be false, and all specified identifiers (Namespace, Class, RowClass) must follow valid C# naming conventions
**Validates: Requirements 4.1, 4.2, 4.3**

### Property 7: Base table acyclic dependency
*For any* table with BaseTableFileName, following the chain of base table file references must terminate without creating a cycle
**Validates: Requirements 8.4**

### Property 8: XProperty structure validity
*For any* XProperty definition (at table or column level), it must have either a Type or DataType specified, and if a Value is provided, it must be compatible with the specified Type
**Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**

### Property 9: Validation error message completeness
*For any* validation error generated, the error message must include the YAML file path, and if the error relates to a specific property, it must include the property path in dot notation
**Validates: Requirements 1.2, 9.1, 9.2**

### Property 10: Warning non-blocking behavior
*For any* validation run that produces only warnings (no errors), the validation result should indicate success (IsValid = true) and code generation should proceed
**Validates: Requirements 1.5, 6.3, 6.4**

### Property 11: Primary key uniqueness
*For any* PrimaryKey definition, all column names in the array must be unique (no duplicates)
**Validates: Requirements 5.5**

### Property 12: Validation rule exception handling
*For any* validation rule that throws an exception during execution, the validation engine must catch the exception and convert it to a validation error without terminating the validation process
**Validates: Requirements 10.4**

### Property 13: Validation rule execution order determinism
*For any* validation run with the same set of registered rules, the rules must execute in the same deterministic order across multiple invocations
**Validates: Requirements 10.3**

### Property 14: Base table loading precedence
*For any* table with BaseTableFileName, the base table must be loaded and validated before the derived table validation completes
**Validates: Requirements 8.1, 8.5**

### Property 15: Derived table column compatibility
*For any* derived table that defines columns or ColumnOptions, the definitions must not conflict with inherited base table column definitions
**Validates: Requirements 8.2, 8.3**

## Error Handling

### Error Reporting Strategy

1. **Fail Fast for Critical Errors**: Stop validation immediately for:
   - YAML deserialization failures
   - File system access errors
   - Circular base table dependencies

2. **Collect All Validation Errors**: Continue validation to collect all errors for:
   - Column definition errors
   - Relation errors
   - Constraint violations
   - Code generation option errors

3. **Non-Blocking Warnings**: Emit warnings but continue for:
   - Deprecated properties
   - Unused properties
   - Suboptimal configurations

### Error Message Format

```
Error in 'path/to/file.brudixy.yaml' at 'ColumnOptions.id.AllowNull':
  Primary key column 'id' cannot allow null values.
  Suggested fix: Set AllowNull to false or remove from PrimaryKey.
```

### Integration with Build System

Validation errors will be reported through the source generator diagnostic system:

```csharp
context.ReportDiagnostic(Diagnostic.Create(
    new DiagnosticDescriptor(
        id: "BRUDIXY001",
        title: "Schema Validation Error",
        messageFormat: "{0}",
        category: "BrudixtyTypeGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true),
    Location.None,
    errorMessage));
```

## Testing Strategy

### Unit Testing

Unit tests will verify individual validation rules:

- Test each validation rule in isolation with valid and invalid inputs
- Test ValidationResult accumulation of errors and warnings
- Test ValidationContext construction and data access
- Test ColumnTypeInfo parsing for all type patterns
- Test identifier validation for edge cases

### Property-Based Testing

Property-based tests will verify universal correctness properties:

- Generate random DataTableObj instances and verify validation completeness
- Generate random column references and verify existence checking
- Generate random type strings and verify parsing consistency
- Generate random relation definitions and verify key length validation
- Generate random base table chains and verify cycle detection

The property-based testing library for C# will be **FsCheck** (version 2.16.6 or later), which provides:
- Arbitrary data generators for complex types
- Shrinking to find minimal failing cases
- Integration with NUnit test framework
- Configurable test iteration counts (minimum 100 iterations per property)

Each property-based test will be tagged with a comment referencing the correctness property:
```csharp
// Feature: yaml-schema-validation, Property 1: Validation completeness
[FsCheck.NUnit.Property(MaxTest = 100)]
public Property ValidationCompleteness_AllRulesExecuted() { ... }
```

### Integration Testing

Integration tests will validate the complete pipeline:

- Load real YAML files from test fixtures
- Validate against known good and bad schemas
- Verify error messages match expected format
- Test base table inheritance validation
- Test file system error handling

### Test Data

Test fixtures will include:
- Valid minimal schema
- Valid complex schema with all features
- Invalid schemas for each error category
- Edge cases (empty tables, single column, etc.)
- Base table inheritance scenarios

## Implementation Notes

### Performance Considerations

1. **Lazy Base Table Loading**: Load base tables only when needed for validation
2. **Rule Ordering**: Execute fast rules first (structural checks) before expensive rules (cross-table validation)
3. **Caching**: Cache loaded base tables to avoid redundant file reads
4. **Parallel Validation**: Validate independent tables in parallel when processing datasets

### Extensibility Points

1. **Custom Validation Rules**: Developers can implement IValidationRule and register with the engine
2. **Custom Type Validators**: Support for validating custom user-defined types
3. **Validation Profiles**: Different rule sets for different validation strictness levels (strict, normal, permissive)

### Backward Compatibility

The validation system will be opt-in initially:
- Default behavior: validation enabled with warnings
- MSBuild property to disable: `<BrudixtySchemaValidation>false</BrudixtySchemaValidation>`
- MSBuild property for strict mode: `<BrudixtySchemaValidationStrict>true</BrudixtySchemaValidationStrict>`

### Integration Points

1. **TypeGenerator.cs**: Add validation call before code generation
2. **YamlSchemaReader.cs**: Integrate validation engine
3. **DataTableObj.cs**: Add validation helper methods
4. **Build Output**: Report diagnostics through Roslyn diagnostic system
