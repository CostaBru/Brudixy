# Brudixy Schema Validation

The Brudixy TypeGenerator now includes comprehensive schema validation to catch errors early in the development process.

## Features

### Automatic Validation

All YAML schema files (`.st.brudixy.yaml`, `.dt.brudixy.yaml`, `.ds.brudixy.yaml`) are automatically validated during code generation. If validation errors are found:

1. **Compilation Errors**: Validation errors are reported as MSBuild diagnostics with error severity
2. **Detailed Messages**: Each error includes:
   - The YAML file path
   - The property path (e.g., `PrimaryKey`, `Columns.BadColumn`)
   - A clear error message
   - Suggested fixes (when applicable)
3. **Code Generation Blocked**: Code generation is skipped for schemas with validation errors
4. **Error File Generated**: A `.ValidationErrors.cs` file is generated showing all validation errors

### Validation Rules

The validation engine checks for:

#### Column Type Validation
- Conflicting nullability modifiers (e.g., `String?!`)
- Multiple structural modifiers (e.g., `Int32[]<>`)
- Conflicting `AllowNull` settings with type modifiers
- Empty or invalid `EnumType` properties

#### Column Reference Validation
- PrimaryKey references non-existent columns
- Relation keys reference non-existent columns
- Index columns reference non-existent columns
- GroupedProperty columns reference non-existent columns
- Empty or mismatched relation key arrays

### Configuration

#### Disable Validation

To disable validation entirely (not recommended):

```xml
<PropertyGroup>
  <BrudixyDisableValidation>true</BrudixyDisableValidation>
</PropertyGroup>
```

#### Strict Validation Mode

To treat warnings as errors:

```xml
<PropertyGroup>
  <BrudixyStrictValidation>true</BrudixyStrictValidation>
</PropertyGroup>
```

## Error Examples

### Example 1: Non-Existent Column in PrimaryKey

**YAML:**
```yaml
Table: Users
Columns:
  Id: Int32
  Name: String
PrimaryKey:
  - Id
  - NonExistentColumn
```

**Error:**
```
error BRXVAL001: Users.st.brudixy.yaml: PrimaryKey references non-existent column 'NonExistentColumn'.
```

### Example 2: Conflicting Type Modifiers

**YAML:**
```yaml
Table: Products
Columns:
  Id: Int32
  Name: String?!
```

**Error:**
```
error BRXVAL001: Products.st.brudixy.yaml: Invalid type definition for column 'Name': Column type cannot have both nullable (?) and non-null (!) modifiers.
```

### Example 3: Mismatched Relation Keys

**YAML:**
```yaml
Relations:
  ParentRelation:
    ParentKey:
      - Id
    ChildKey:
      - ParentId
      - SecondId
```

**Error:**
```
error BRXVAL001: Schema.yaml: Relation 'ParentRelation' has mismatched key array lengths: ParentKey has 1 columns, ChildKey has 2 columns.
```

## Diagnostic Codes

- **BRXVAL001**: Schema Validation Error - A validation rule found an error in the schema
- **BRXVAL002**: Schema Validation Warning - A validation rule found a potential issue
- **BRXTY001**: YamlDotNet Loading Error - Failed to load the YAML parser
- **BRXTY002**: YamlSchemaReader Initialization Error - Failed to initialize the schema reader
- **BRXTY003**: Type Generator Error - An unexpected error occurred during code generation

## Best Practices

1. **Fix Validation Errors First**: Always resolve validation errors before attempting to use generated code
2. **Review Warnings**: Warnings indicate potential issues that should be reviewed
3. **Use Strict Mode in CI**: Enable `BrudixyStrictValidation` in continuous integration to catch all issues
4. **Keep Schemas Simple**: Complex schemas are harder to validate and maintain
5. **Test with Real Data**: Validation catches structural errors, but you should still test with real data

## Extending Validation

The validation system is extensible. To add custom validation rules:

1. Implement `IValidationRule` interface
2. Register your rule with `SchemaValidationEngine.RegisterRule()`
3. Set appropriate priority for rule execution order

See `Brudixy.TypeGenerator.Core/Validation/Rules/` for examples.
