# Brudixy Table Schema

This directory contains the JSON Schema definition for Brudixy YAML table schema files (`.brudixy.yaml`). The JSON Schema enables IDE-level validation, autocomplete, and documentation for developers writing Brudixy table definitions.

## Schema Files

- **`brudixy-table-schema.json`** - Latest version of the schema
- **`v1.0.0/brudixy-table-schema.json`** - Version 1.0.0 of the schema (stable)

## What is Brudixy?

Brudixy is a TypeGenerator that creates strongly-typed C# table and row classes from YAML schema definitions. The YAML files define table structure, columns, relations, indexes, grouped properties, and code generation options.

## File Patterns

The schema is designed to work with the following file patterns:

- `*.st.brudixy.yaml` - Single table definitions
- `*.dt.brudixy.yaml` - Data table definitions
- `*.ds.brudixy.yaml` - Dataset definitions

## Using the Schema

There are several ways to associate the JSON Schema with your Brudixy YAML files:

### Method 1: Inline Directive (Recommended)

Add a comment at the top of your YAML file:

```yaml
# yaml-language-server: $schema=https://brudixy.org/schemas/brudixy-table-schema.json

Table: Users
Columns:
  Id: Int32
  Name: String | 256
  Email: String | 512
```

**For local development**, you can use a relative path:

```yaml
# yaml-language-server: $schema=../../schemas/brudixy-table-schema.json
```

### Method 2: VS Code Configuration

Add to your workspace or user `settings.json`:

```json
{
  "yaml.schemas": {
    "https://brudixy.org/schemas/brudixy-table-schema.json": [
      "**/*.st.brudixy.yaml",
      "**/*.dt.brudixy.yaml",
      "**/*.ds.brudixy.yaml"
    ]
  }
}
```

**For local development**, use a file path:

```json
{
  "yaml.schemas": {
    "./schemas/brudixy-table-schema.json": [
      "**/*.st.brudixy.yaml",
      "**/*.dt.brudixy.yaml",
      "**/*.ds.brudixy.yaml"
    ]
  }
}
```

**Steps:**
1. Open VS Code
2. Press `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (Mac)
3. Type "Preferences: Open Settings (JSON)"
4. Add the configuration above

### Method 3: JetBrains IDEs (Rider, IntelliJ IDEA, etc.)

**Steps:**
1. Open Settings/Preferences (`Ctrl+Alt+S` on Windows/Linux, `Cmd+,` on Mac)
2. Navigate to: **Languages & Frameworks → Schemas and DTDs → JSON Schema Mappings**
3. Click the **+** button to add a new mapping
4. Configure:
   - **Name**: Brudixy Table Schema
   - **Schema file or URL**: `https://brudixy.org/schemas/brudixy-table-schema.json`
   - **Schema version**: JSON Schema version 7
5. Add file path patterns:
   - Click **+** in the file path patterns section
   - Add: `*.st.brudixy.yaml`
   - Add: `*.dt.brudixy.yaml`
   - Add: `*.ds.brudixy.yaml`
6. Click **OK** to save

**For local development**, use a file path in step 4:
- **Schema file or URL**: `$PROJECT_DIR$/schemas/brudixy-table-schema.json`

### Method 4: Visual Studio

Visual Studio uses the YAML Language Server through extensions. Install the **YAML Language Support** extension:

**Steps:**
1. Open Visual Studio
2. Go to **Extensions → Manage Extensions**
3. Search for "YAML" or "YAML Language Support"
4. Install the extension and restart Visual Studio
5. Create a `.vscode/settings.json` file in your project root (or use workspace settings)
6. Add the configuration from Method 2 above

Alternatively, use the inline directive (Method 1) which works across all IDEs.

## Schema Features

The JSON Schema provides:

### ✅ Validation
- Validates table structure and required fields
- Validates column type syntax and modifiers
- Validates C# identifiers (namespaces, class names, property names)
- Validates relation structures (parent/child keys)
- Validates grouped properties format
- Validates extended properties (XProperties)

### 🔍 Autocomplete
- Property name suggestions
- Enum value suggestions (e.g., `Type: Tuple` or `NewStruct`)
- Boolean value suggestions
- Column type examples

### 📖 Documentation
- Hover over any property to see its description
- Inline examples for complex properties
- Format requirements and constraints

### 🎯 Error Messages
- Clear validation errors with expected formats
- Pattern mismatch explanations
- Missing required field notifications

## Example YAML Files

### Minimal Example

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
  ExtraUsing:
    - using System.Collections.Generic;
    - using MyApp.Models;

PrimaryKey:
  - Id

Columns:
  Id: Int32
  Name: String | 256 | Index
  Description: String?
  Price: Decimal
  CategoryId: Int32
  CreatedDate: DateTime!
  ModifiedDate: DateTime?
  Tags: String[]
  PriceRange: Decimal<>
  IsActive: Boolean
  Metadata: (ProductMetadata)

ColumnOptions:
  Id:
    IsUnique: true
    AllowNull: false
    Auto: true
  Name:
    HasIndex: true
    MaxLength: 256
  Price:
    DefaultValue: "0.00"
  CreatedDate:
    DefaultValue: "GETDATE()"

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

Indexes:
  IX_Name_Category:
    Columns:
      - Name
      - CategoryId
    Unique: false

XProperties:
  DisplayOrder:
    Type: Int32
    Value: 1
  TableDescription:
    Type: String
    Value: "Product catalog table"

EnforceConstraints: true
```

## Column Type Syntax

Column types follow this pattern: `<BaseType>[?|!][[]|<>] [| <Options>]`

### Built-in Types
- **Numeric**: `Int32`, `Int64`, `UInt32`, `UInt64`, `Int16`, `UInt16`, `Byte`, `SByte`, `Single`, `Double`, `Decimal`
- **Date/Time**: `DateTime`, `DateTimeOffset`, `TimeSpan`
- **Text**: `String`, `Char`
- **Binary**: `Byte[]`
- **Other**: `Boolean`, `Guid`, `Object`

### Type Modifiers
- `?` - Nullable (e.g., `String?`)
- `!` - Explicitly non-null (e.g., `DateTime!`)
- `[]` - Array (e.g., `Int32[]`)
- `<>` - Range (e.g., `Int32<>`)

### Inline Options (pipe-separated)
- `| <number>` - Max length (e.g., `String | 256`)
- `| Index` - Create index
- `| Unique` - Unique constraint
- `| Complex` - Complex type (serialized)
- `| Class` - Class type (not struct)
- `| Auto` - Auto-generated
- `| Service` - Service column
- `| Nullable` - Allow null
- `| Not null` - Disallow null

### User-Defined Types
- **Struct**: `(TypeName)` - Parentheses indicate struct (e.g., `(Point2D)`)
- **Class**: `TypeName` or `TypeName | Class` - No parentheses or explicit Class modifier
- **Complex**: `TypeName | Complex` - Serialized complex type

### Examples
```yaml
Columns:
  Id: Int32                          # Non-nullable integer
  Name: String | 256                 # String with max length 256
  Email: String | 512 | Index        # Indexed string with max length
  Description: String?               # Nullable string
  CreatedDate: DateTime!             # Explicitly non-null datetime
  Tags: String[]                     # Array of strings
  PriceRange: Decimal<>              # Range of decimals
  Location: (Point2D)                # User-defined struct
  Metadata: MyClass | Complex        # Complex serialized class
```

## Versioning

The schema uses semantic versioning:
- **Major version**: Breaking changes to schema structure
- **Minor version**: New properties or non-breaking changes
- **Patch version**: Documentation updates or bug fixes

### Version URLs
- **Latest**: `https://brudixy.org/schemas/brudixy-table-schema.json`
- **Specific**: `https://brudixy.org/schemas/v1.0.0/brudixy-table-schema.json`

To pin to a specific version, use the versioned URL in your configuration.

## Troubleshooting

### Schema not working in VS Code

1. **Check YAML extension**: Ensure you have the "YAML" extension by Red Hat installed
2. **Check settings**: Verify your `settings.json` has the correct schema mapping
3. **Reload window**: Press `Ctrl+Shift+P` and run "Developer: Reload Window"
4. **Check file pattern**: Ensure your file matches the pattern (e.g., `*.st.brudixy.yaml`)
5. **Try inline directive**: Add the schema comment at the top of your file

### Schema not working in JetBrains IDEs

1. **Check plugin**: Ensure the YAML plugin is installed and enabled
2. **Check mapping**: Verify the schema mapping in Settings → JSON Schema Mappings
3. **Invalidate caches**: File → Invalidate Caches / Restart
4. **Check file pattern**: Ensure your file matches the pattern
5. **Try inline directive**: Add the schema comment at the top of your file

### Validation errors not showing

1. **Check language server**: Ensure the YAML language server is running
2. **Check file association**: Verify the file is recognized as YAML
3. **Check schema URL**: Ensure the schema URL is accessible (for remote URLs)
4. **Check syntax**: Ensure your YAML is valid (no syntax errors)

### Autocomplete not working

1. **Trigger manually**: Press `Ctrl+Space` to trigger autocomplete
2. **Check context**: Autocomplete works based on context (property names, values)
3. **Check schema**: Ensure the schema is loaded (check status bar in VS Code)

## Contributing

If you find issues with the schema or have suggestions for improvements:

1. Check existing issues in the repository
2. Create a new issue with:
   - Description of the problem
   - Example YAML that demonstrates the issue
   - Expected behavior vs actual behavior
   - IDE and version information
3. Submit a pull request with fixes or improvements

## License

This schema is part of the Brudixy project. See the main repository LICENSE file for details.

## Resources

- **Brudixy Repository**: [GitHub](https://github.com/brudixy/brudixy)
- **JSON Schema Specification**: [json-schema.org](https://json-schema.org/)
- **YAML Language Server**: [GitHub](https://github.com/redhat-developer/yaml-language-server)
- **JSON Schema Store**: [schemastore.org](https://www.schemastore.org/)

## Support

For questions or support:
- Open an issue in the Brudixy repository
- Check the documentation in the `docs/` directory
- Review example files in `Brudixy.Tests/TypedDs/`
