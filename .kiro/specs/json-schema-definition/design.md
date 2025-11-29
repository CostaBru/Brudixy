# Design Document

## Overview

This design document describes a JSON Schema definition for Brudixy YAML schema files (`.brudixy.yaml`). The JSON Schema will provide IDE-level validation, autocomplete, and documentation for developers writing Brudixy table definitions. The schema will be designed for registration with the JSON Schema Store (schemastore.org), enabling automatic schema association in IDEs through the YAML Language Server.

The JSON Schema will mirror the structure defined in `DataTableObj` and related classes, providing comprehensive validation for all properties including tables, columns, relations, indexes, grouped properties, and code generation options.

## Architecture

The JSON Schema will be a single, self-contained JSON file that defines the complete structure of Brudixy YAML files. The architecture consists of:

1. **Root Schema**: Defines top-level properties (Table, Columns, Relations, etc.)
2. **Reusable Definitions**: Common patterns defined once and referenced throughout (e.g., XProperty structure, column type patterns)
3. **Pattern Validation**: Regular expressions for validating type syntax, identifiers, and other formatted strings
4. **Documentation**: Comprehensive descriptions and examples embedded in the schema

```
┌─────────────────────────────────────────┐
│     brudixy-schema.json                 │
│  ┌───────────────────────────────────┐  │
│  │  Root Schema                      │  │
│  │  - $schema, $id, title, desc      │  │
│  │  - properties (Table, Columns...) │  │
│  │  - required fields                │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  Definitions (Reusable)           │  │
│  │  - columnType                     │  │
│  │  - xProperty                      │  │
│  │  - relation                       │  │
│  │  - codeGenOptions                 │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  Pattern Validators               │  │
│  │  - C# identifier regex            │  │
│  │  - namespace regex                │  │
│  │  - column type regex              │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│   JSON Schema Store Registration        │
│   - File patterns: *.st.brudixy.yaml    │
│   -                *.dt.brudixy.yaml    │
│   -                *.ds.brudixy.yaml    │
│   - Schema URL                          │
└─────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│   IDE Integration (YAML Lang Server)    │
│   - Validation                          │
│   - Autocomplete                        │
│   - Hover documentation                 │
└─────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Root Schema Structure

The root schema defines the overall structure of a Brudixy YAML file:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://brudixy.org/schemas/brudixy-table-schema.json",
  "title": "Brudixy Table Schema",
  "description": "Schema for Brudixy TypeGenerator YAML files",
  "type": "object",
  "required": ["Table", "Columns"],
  "properties": {
    "Table": { ... },
    "CodeGenerationOptions": { ... },
    "Columns": { ... },
    "ColumnOptions": { ... },
    "Relations": { ... },
    "PrimaryKey": { ... },
    "GroupedProperties": { ... },
    "GroupedPropertyOptions": { ... },
    "RowSubTypes": { ... },
    "RowSubTypeOptions": { ... },
    "XProperties": { ... },
    "Indexes": { ... },
    "EnforceConstraints": { ... }
  }
}
```

### 2. Column Type Definition

Column types follow a specific pattern with optional modifiers:

**Pattern**: `<BaseType>[?|!][[]|<>] [| <MaxLength>] [| <Options>]`

**Examples**:
- `Int32` - Non-nullable integer
- `String?` - Nullable string
- `DateTime!` - Explicitly non-null datetime
- `Int32[]` - Array of integers
- `Int32<>` - Range of integers
- `String | 256` - String with max length 256
- `String | 256 | Index` - Indexed string with max length
- `(MyStruct)` - User-defined struct type
- `MyClass | Complex` - Complex user-defined class

**JSON Schema Definition**:

```json
{
  "columnType": {
    "type": "string",
    "pattern": "^[A-Za-z0-9_.<>()]+[?!]?(\\[\\]|<>)?( \\| [A-Za-z0-9_ ]+)*$",
    "description": "Column type specification...",
    "examples": [
      "Int32",
      "String?",
      "String | 256",
      "Int32[]",
      "DateTime!",
      "(Point2D)",
      "MyClass | Complex"
    ]
  }
}
```

### 3. CodeGenerationOptions Definition

```json
{
  "codeGenerationOptions": {
    "type": "object",
    "properties": {
      "Namespace": {
        "type": "string",
        "pattern": "^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*$",
        "description": "The C# namespace for the generated table class"
      },
      "Class": {
        "type": "string",
        "pattern": "^[A-Za-z_][A-Za-z0-9_]*$",
        "description": "The name of the generated table class"
      },
      "RowClass": {
        "type": "string",
        "pattern": "^[A-Za-z_][A-Za-z0-9_]*$",
        "description": "The name of the generated row class"
      },
      "Abstract": {
        "type": "boolean",
        "description": "Whether the generated class should be abstract"
      },
      "Sealed": {
        "type": "boolean",
        "description": "Whether the generated class should be sealed",
        "default": true
      },
      "BaseTableFileName": {
        "type": "string",
        "description": "Path to the base table YAML file for inheritance"
      },
      "ExtraUsing": {
        "type": "array",
        "items": {
          "type": "string",
          "pattern": "^using [A-Za-z_][A-Za-z0-9_.]*;$"
        },
        "description": "Additional using directives for the generated code"
      }
    }
  }
}
```

### 4. ColumnOptions Definition

```json
{
  "columnOptions": {
    "type": "object",
    "patternProperties": {
      "^[A-Za-z_][A-Za-z0-9_]*$": {
        "type": "object",
        "properties": {
          "Type": {
            "type": "string",
            "description": "Override the column type"
          },
          "AllowNull": {
            "type": "boolean",
            "description": "Whether the column allows null values"
          },
          "IsUnique": {
            "type": "boolean",
            "description": "Whether the column has a unique constraint"
          },
          "HasIndex": {
            "type": "boolean",
            "description": "Whether to create an index on this column"
          },
          "IsReadOnly": {
            "type": "boolean",
            "description": "Whether the column is read-only"
          },
          "IsService": {
            "type": "boolean",
            "description": "Whether the column is a service column"
          },
          "Auto": {
            "type": "boolean",
            "description": "Whether the column is auto-generated"
          },
          "MaxLength": {
            "type": "integer",
            "minimum": 1,
            "description": "Maximum length for string/array columns"
          },
          "DataType": {
            "type": "string",
            "description": "The data type for user-defined types"
          },
          "Expression": {
            "type": "string",
            "description": "Expression for computed columns"
          },
          "DefaultValue": {
            "type": "string",
            "description": "Default value for the column"
          },
          "DisplayName": {
            "type": "string",
            "description": "Display name for the column"
          },
          "CodeProperty": {
            "type": "string",
            "pattern": "^[A-Za-z_][A-Za-z0-9_]*$",
            "description": "Name of the generated property in code"
          },
          "EnumType": {
            "type": "string",
            "description": "C# enum type name for enum columns"
          },
          "XProperties": {
            "$ref": "#/definitions/xPropertiesMap"
          }
        }
      }
    }
  }
}
```

### 5. Relations Definition

```json
{
  "relations": {
    "type": "object",
    "patternProperties": {
      "^[A-Za-z_][A-Za-z0-9_]*$": {
        "type": "object",
        "required": ["ParentKey", "ChildKey"],
        "properties": {
          "ParentTable": {
            "type": "string",
            "description": "Name of the parent table"
          },
          "ChildTable": {
            "type": "string",
            "description": "Name of the child table"
          },
          "ParentKey": {
            "type": "array",
            "items": { "type": "string" },
            "minItems": 1,
            "description": "Array of parent table column names"
          },
          "ChildKey": {
            "type": "array",
            "items": { "type": "string" },
            "minItems": 1,
            "description": "Array of child table column names"
          }
        }
      }
    }
  }
}
```

### 6. XProperties Definition

XProperties are extended metadata properties that can be attached to tables, columns, or row subtypes:

```json
{
  "xProperty": {
    "type": "object",
    "properties": {
      "Type": {
        "type": "string",
        "description": "C# type name for the property"
      },
      "DataType": {
        "type": "string",
        "description": "Alternative data type specification"
      },
      "Value": {
        "description": "Default value for the property (any JSON type)"
      },
      "CodePropertyName": {
        "type": "string",
        "pattern": "^[A-Za-z_][A-Za-z0-9_]*$",
        "description": "Name of the generated property in code"
      },
      "EnumType": {
        "type": "string",
        "description": "C# enum type name if the property is an enum"
      }
    },
    "anyOf": [
      { "required": ["Type"] },
      { "required": ["DataType"] }
    ]
  },
  "xPropertiesMap": {
    "type": "object",
    "patternProperties": {
      "^[A-Za-z_][A-Za-z0-9_]*$": {
        "$ref": "#/definitions/xProperty"
      }
    }
  }
}
```

### 7. GroupedProperties Definition

```json
{
  "groupedProperties": {
    "type": "object",
    "patternProperties": {
      "^[A-Za-z_][A-Za-z0-9_]*$": {
        "type": "string",
        "pattern": "^[A-Za-z_][A-Za-z0-9_]*(\\|[A-Za-z_][A-Za-z0-9_]*)+$",
        "description": "Pipe-separated list of column names to group"
      }
    }
  },
  "groupedPropertyOptions": {
    "type": "object",
    "patternProperties": {
      "^[A-Za-z_][A-Za-z0-9_]*$": {
        "type": "object",
        "properties": {
          "Type": {
            "type": "string",
            "enum": ["Tuple", "NewStruct"],
            "description": "Whether to generate a Tuple or a new struct"
          },
          "IsReadOnly": {
            "type": "boolean",
            "description": "Whether the grouped property is read-only"
          },
          "StructName": {
            "type": "string",
            "pattern": "^[A-Za-z_][A-Za-z0-9_]*$",
            "description": "Name of the struct (only used when Type is NewStruct)"
          }
        }
      }
    }
  }
}
```

## Data Models

### Built-in Types

The schema will recognize these built-in types:
- Numeric: `Int32`, `Int64`, `UInt32`, `UInt64`, `Int16`, `UInt16`, `Byte`, `SByte`, `Single`, `Double`, `Decimal`
- Date/Time: `DateTime`, `DateTimeOffset`, `TimeSpan`
- Text: `String`, `Char`
- Binary: `Byte[]`
- Other: `Boolean`, `Guid`, `Object`

### Type Modifiers

- `?` - Nullable (reference types and value types)
- `!` - Explicitly non-null
- `[]` - Array
- `<>` - Range
- `| Complex` - Complex type (serialized)
- `| Class` - Class type (not struct)
- `| Index` - Create index
- `| Unique` - Unique constraint
- `| Auto` - Auto-generated
- `| Service` - Service column
- `| Nullable` - Allow null
- `| Not null` - Disallow null
- `| <number>` - Max length

### User-Defined Types

- Struct: `(TypeName)` - Parentheses indicate struct
- Class: `TypeName` or `TypeName | Class` - No parentheses or explicit Class modifier
- Complex: `TypeName | Complex` - Serialized complex type

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Schema validation completeness
*For any* valid Brudixy YAML file, the JSON Schema should validate all top-level properties and nested structures without false positives
**Validates: Requirements 1.1, 1.3, 1.5**

### Property 2: Column type pattern matching
*For any* column type string that matches the Brudixy type syntax, the JSON Schema pattern should accept it, and for any invalid type string, the schema should reject it
**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**

### Property 3: C# identifier validation
*For any* property that represents a C# identifier (Class, RowClass, Namespace, CodeProperty), the JSON Schema should validate it matches C# identifier rules
**Validates: Requirements 3.1, 3.2, 4.1, 4.2**

### Property 4: Required field enforcement
*For any* Brudixy YAML file missing the Table or Columns property, the JSON Schema should report a validation error
**Validates: Requirements 1.1**

### Property 5: Relation structure validation
*For any* relation definition, the JSON Schema should require both ParentKey and ChildKey as non-empty arrays
**Validates: Requirements 5.1, 5.2, 5.3**

### Property 6: XProperty structure validation
*For any* XProperty definition, the JSON Schema should require either Type or DataType to be specified
**Validates: Requirements 7.1, 7.2, 7.3**

### Property 7: GroupedProperty format validation
*For any* grouped property value, the JSON Schema should validate it contains at least two pipe-separated column names
**Validates: Requirements 6.1, 6.2**

### Property 8: Boolean property validation
*For any* boolean property (Abstract, Sealed, AllowNull, IsUnique, HasIndex, etc.), the JSON Schema should only accept true or false values
**Validates: Requirements 3.2, 4.3**

### Property 9: Array property validation
*For any* array property (PrimaryKey, ParentKey, ChildKey, ExtraUsing), the JSON Schema should validate each element matches the expected type
**Validates: Requirements 5.1, 11.1, 11.3**

### Property 10: Enum value validation
*For any* property with enumerated values (GroupedPropertyOptions.Type), the JSON Schema should only accept the defined enum values
**Validates: Requirements 6.2**

### Property 11: Pattern property validation
*For any* object using patternProperties (ColumnOptions, Relations, GroupedProperties), the JSON Schema should validate property names match the identifier pattern
**Validates: Requirements 4.1, 4.2**

### Property 12: Documentation completeness
*For any* property defined in the JSON Schema, it should include a description field with clear explanation
**Validates: Requirements 9.1, 9.2, 9.3, 9.4**

### Property 13: Example validity
*For any* example provided in the JSON Schema, it should be a valid instance according to the schema rules
**Validates: Requirements 12.1, 12.2, 12.3, 12.5**

### Property 14: Schema URL accessibility
*For any* request to the schema URL, it should return the JSON Schema with appropriate content-type and CORS headers
**Validates: Requirements 10.1, 10.4**

### Property 15: File pattern matching
*For any* file matching the patterns `*.st.brudixy.yaml`, `*.dt.brudixy.yaml`, or `*.ds.brudixy.yaml`, IDEs with YAML Language Server should automatically apply the schema
**Validates: Requirements 8.3, 8.4**

## Error Handling

### Schema Validation Errors

The JSON Schema will provide clear error messages through the IDE:

1. **Missing Required Fields**:
   ```
   Error: Missing required property 'Table'
   ```

2. **Invalid Type**:
   ```
   Error: Property 'Abstract' must be a boolean, got string
   ```

3. **Pattern Mismatch**:
   ```
   Error: Property 'Namespace' must match pattern '^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*$'
   Expected: Valid C# namespace (e.g., 'MyApp.Data.Tables')
   ```

4. **Invalid Enum Value**:
   ```
   Error: Property 'Type' must be one of: Tuple, NewStruct
   ```

5. **Array Validation**:
   ```
   Error: Property 'ParentKey' must be a non-empty array
   ```

### IDE Integration

The schema will integrate with IDEs through:

1. **YAML Language Server**: Automatic validation and autocomplete
2. **Hover Documentation**: Display property descriptions on hover
3. **Error Squiggles**: Real-time validation errors
4. **Autocomplete**: Context-aware suggestions

## Testing Strategy

### Unit Testing

Unit tests will verify the JSON Schema structure:

- Test schema is valid JSON Schema Draft 7
- Test all required properties are defined
- Test all patterns compile as valid regex
- Test all references resolve correctly
- Test schema metadata ($schema, $id, title) is present

### Property-Based Testing

Property-based tests will verify schema validation behavior:

- Generate random valid YAML files and verify schema accepts them
- Generate random invalid YAML files and verify schema rejects them
- Generate random column type strings and verify pattern matching
- Generate random C# identifiers and verify validation
- Generate random relation definitions and verify structure validation

The property-based testing library for C# will be **FsCheck** (version 2.16.6 or later).

Each property-based test will be tagged with:
```csharp
// Feature: json-schema-definition, Property 1: Schema validation completeness
[FsCheck.NUnit.Property(MaxTest = 100)]
public Property SchemaValidatesAllProperties() { ... }
```

### Integration Testing

Integration tests will validate IDE integration:

- Load schema in a test YAML Language Server instance
- Validate known good YAML files pass validation
- Validate known bad YAML files fail with expected errors
- Test autocomplete suggestions are provided
- Test hover documentation is displayed
- Test file pattern association works

### Manual Testing

Manual testing will verify:

- Schema works in VS Code with YAML extension
- Schema works in JetBrains IDEs
- Schema works in Visual Studio
- Autocomplete provides useful suggestions
- Error messages are clear and actionable
- Hover documentation is helpful

## Implementation Notes

### Schema Hosting

The JSON Schema will be hosted at:
- Primary: `https://brudixy.org/schemas/brudixy-table-schema.json`
- GitHub: `https://raw.githubusercontent.com/brudixy/brudixy/main/schemas/brudixy-table-schema.json`

### Versioning

The schema will use semantic versioning:
- Major version: Breaking changes to schema structure
- Minor version: New properties or non-breaking changes
- Patch version: Documentation updates or bug fixes

Versioned URLs:
- Latest: `https://brudixy.org/schemas/brudixy-table-schema.json`
- Specific: `https://brudixy.org/schemas/v1.0.0/brudixy-table-schema.json`

### JSON Schema Store Registration

To register with schemastore.org:

1. Fork the schemastore repository
2. Add schema file to `src/schemas/json/`
3. Add entry to `src/api/json/catalog.json`:
   ```json
   {
     "name": "Brudixy Table Schema",
     "description": "Schema for Brudixy TypeGenerator YAML files",
     "fileMatch": [
       "*.st.brudixy.yaml",
       "*.dt.brudixy.yaml",
       "*.ds.brudixy.yaml"
     ],
     "url": "https://brudixy.org/schemas/brudixy-table-schema.json"
   }
   ```
4. Submit pull request

### Alternative Schema Association

For users who don't want to use file patterns:

1. **Inline directive** (add to top of YAML file):
   ```yaml
   # yaml-language-server: $schema=https://brudixy.org/schemas/brudixy-table-schema.json
   ```

2. **VS Code settings.json**:
   ```json
   {
     "yaml.schemas": {
       "https://brudixy.org/schemas/brudixy-table-schema.json": "**/*.brudixy.yaml"
     }
   }
   ```

3. **JetBrains IDE settings**:
   Settings → Languages & Frameworks → Schemas and DTDs → JSON Schema Mappings

### Schema Maintenance

The schema should be:
- Kept in sync with `DataTableObj` structure
- Updated when new properties are added to Brudixy
- Versioned appropriately for breaking changes
- Tested against real-world YAML files
- Documented with examples from actual usage

### Performance Considerations

- Keep schema file size reasonable (< 100KB)
- Use `$ref` to avoid duplication
- Avoid overly complex regex patterns
- Cache schema in IDEs for performance

### Backward Compatibility

- New properties should be optional by default
- Deprecated properties should be marked but not removed
- Breaking changes should increment major version
- Provide migration guide for major version changes
