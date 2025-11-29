# Requirements Document

## Introduction

This document defines the requirements for creating a JSON Schema definition for Brudixy YAML schema files (`.brudixy.yaml`). The JSON Schema will enable IDE-level validation, autocomplete, and documentation for developers writing Brudixy table definitions. The schema will be designed for registration with the JSON Schema Store, providing automatic schema association in popular IDEs through the YAML Language Server.

## Glossary

- **JSON Schema**: A vocabulary that allows you to annotate and validate JSON/YAML documents, providing structure definition and validation rules
- **JSON Schema Store**: A public registry (schemastore.org) that catalogs JSON and YAML schemas and enables automatic schema association in IDEs
- **YAML Language Server**: An IDE component that provides validation, autocomplete, and hover documentation for YAML files based on associated JSON Schemas
- **Schema Association**: The mechanism by which YAML files are linked to their corresponding JSON Schema, either through file patterns, inline directives, or IDE configuration
- **Brudixy YAML Schema**: YAML files with `.brudixy.yaml` extension that define table structure, columns, relations, and code generation options for the Brudixy TypeGenerator
- **File Pattern**: A glob pattern (e.g., `*.st.brudixy.yaml`, `*.ds.brudixy.yaml`) used to automatically associate files with their schema

## Requirements

### Requirement 1

**User Story:** As a developer writing Brudixy YAML schemas, I want a JSON Schema definition that describes the complete structure, so that my IDE can provide validation and autocomplete.

#### Acceptance Criteria

1. WHEN the JSON Schema is created THEN it SHALL define all top-level properties (Table, CodeGenerationOptions, Columns, ColumnOptions, Relations, PrimaryKey, GroupedProperties, GroupedPropertyOptions, XProperties)
2. WHEN the JSON Schema defines a property THEN it SHALL include a description explaining the property's purpose and usage
3. WHEN the JSON Schema defines required properties THEN it SHALL mark Table and Columns as required
4. WHEN the JSON Schema is used by an IDE THEN it SHALL provide autocomplete suggestions for property names
5. WHEN the JSON Schema is used by an IDE THEN it SHALL validate the YAML structure and show errors for invalid configurations

### Requirement 2

**User Story:** As a developer, I want the JSON Schema to validate column type definitions, so that I receive immediate feedback on type syntax errors.

#### Acceptance Criteria

1. WHEN a column type is specified THEN the schema SHALL validate it matches the pattern for built-in types (Int32, String, DateTime, etc.) or user-defined types
2. WHEN a column type includes modifiers THEN the schema SHALL validate the modifier syntax (nullable `?`, non-null `!`, array `[]`, range `<>`, complex)
3. WHEN a column type includes inline options THEN the schema SHALL validate the pipe-separated format (e.g., `String | 256 | Index`)
4. WHEN the schema validates column types THEN it SHALL provide examples of valid type specifications in the description
5. WHEN an invalid column type is entered THEN the IDE SHALL display an error message with the expected format

### Requirement 3

**User Story:** As a developer, I want the JSON Schema to provide autocomplete for CodeGenerationOptions, so that I can discover available configuration options.

#### Acceptance Criteria

1. WHEN typing in CodeGenerationOptions THEN the IDE SHALL suggest property names (Namespace, Class, RowClass, Abstract, Sealed, BaseTableFileName, ExtraUsing)
2. WHEN Abstract or Sealed properties are typed THEN the IDE SHALL provide boolean value autocomplete (true/false)
3. WHEN ExtraUsing is typed THEN the IDE SHALL indicate it expects an array of strings
4. WHEN hovering over a CodeGenerationOptions property THEN the IDE SHALL display documentation describing the property's effect
5. WHEN invalid combinations are entered (Abstract and Sealed both true) THEN the IDE SHALL display a validation error

### Requirement 4

**User Story:** As a developer, I want the JSON Schema to validate ColumnOptions, so that I define column constraints correctly.

#### Acceptance Criteria

1. WHEN ColumnOptions are specified THEN the schema SHALL validate property names (Type, AllowNull, IsUnique, HasIndex, DefaultValue, EnumType, CodeProperty, XProperties)
2. WHEN IsUnique or HasIndex are typed THEN the IDE SHALL provide boolean autocomplete
3. WHEN AllowNull is specified THEN the schema SHALL validate it is a boolean value
4. WHEN EnumType is specified THEN the schema SHALL validate it is a string representing a C# type name
5. WHEN XProperties are nested under ColumnOptions THEN the schema SHALL validate their structure recursively

### Requirement 5

**User Story:** As a developer, I want the JSON Schema to validate Relations, so that I define foreign key constraints with correct structure.

#### Acceptance Criteria

1. WHEN a relation is defined THEN the schema SHALL require ParentKey and ChildKey properties
2. WHEN ParentKey or ChildKey are specified THEN the schema SHALL validate they are arrays of strings
3. WHEN ParentTable or ChildTable are specified THEN the schema SHALL validate they are strings
4. WHEN hovering over relation properties THEN the IDE SHALL display documentation explaining parent/child key relationships
5. WHEN a relation is missing required properties THEN the IDE SHALL display validation errors

### Requirement 6

**User Story:** As a developer, I want the JSON Schema to validate GroupedProperties, so that I define tuple or struct groupings correctly.

#### Acceptance Criteria

1. WHEN GroupedProperties are defined THEN the schema SHALL validate the format as a map of group names to pipe-separated column lists
2. WHEN GroupedPropertyOptions are defined THEN the schema SHALL validate Type property values (Tuple or NewStruct)
3. WHEN GroupedPropertyOptions specify IsReadOnly THEN the schema SHALL validate it is a boolean
4. WHEN GroupedPropertyOptions specify StructName THEN the schema SHALL validate it is a valid C# identifier
5. WHEN hovering over GroupedProperties THEN the IDE SHALL display examples of valid group definitions

### Requirement 7

**User Story:** As a developer, I want the JSON Schema to validate XProperties (extended properties), so that I define metadata with correct structure.

#### Acceptance Criteria

1. WHEN an XProperty is defined THEN the schema SHALL validate it has Type or DataType specified
2. WHEN an XProperty specifies Type THEN the schema SHALL validate it is a string representing a C# type
3. WHEN an XProperty specifies Value THEN the schema SHALL allow any JSON value type
4. WHEN XProperties are defined at table level THEN the schema SHALL validate their structure
5. WHEN XProperties are defined at column level THEN the schema SHALL validate their structure identically

### Requirement 8

**User Story:** As a developer, I want the JSON Schema to be registered with the JSON Schema Store, so that my IDE automatically associates it with `.brudixy.yaml` files.

#### Acceptance Criteria

1. WHEN the JSON Schema is created THEN it SHALL include a `$schema` property pointing to the JSON Schema draft version
2. WHEN the JSON Schema is created THEN it SHALL include an `$id` property with a unique URI
3. WHEN the JSON Schema is submitted to JSON Schema Store THEN it SHALL include file pattern mappings for `*.st.brudixy.yaml`, `*.dt.brudixy.yaml`, and `*.ds.brudixy.yaml`
4. WHEN the schema is registered THEN IDEs using YAML Language Server SHALL automatically apply the schema to matching files
5. WHEN the schema is updated THEN the JSON Schema Store SHALL serve the latest version to IDEs

### Requirement 9

**User Story:** As a developer, I want comprehensive documentation in the JSON Schema, so that I understand each property without consulting external documentation.

#### Acceptance Criteria

1. WHEN any property is defined in the schema THEN it SHALL include a description field with clear explanation
2. WHEN a property has specific format requirements THEN the description SHALL include examples
3. WHEN a property has constraints THEN the description SHALL explain the constraints
4. WHEN hovering over a property in the IDE THEN the description SHALL be displayed as hover documentation
5. WHEN a property is deprecated THEN the schema SHALL mark it with a deprecation notice

### Requirement 10

**User Story:** As a developer, I want the JSON Schema to support schema validation without requiring a file pattern, so that I can use custom file naming conventions.

#### Acceptance Criteria

1. WHEN the JSON Schema is created THEN it SHALL be accessible via a stable public URL
2. WHEN a developer wants to use custom file naming THEN they SHALL be able to add a YAML comment directive (`# yaml-language-server: $schema=<url>`)
3. WHEN a developer wants to use custom file naming THEN they SHALL be able to configure schema association in their IDE settings
4. WHEN the schema URL is accessed THEN it SHALL return the JSON Schema with appropriate CORS headers
5. WHEN the schema is hosted THEN it SHALL be versioned to allow pinning to specific schema versions

### Requirement 11

**User Story:** As a developer, I want the JSON Schema to validate PrimaryKey definitions, so that I define primary keys correctly.

#### Acceptance Criteria

1. WHEN PrimaryKey is specified THEN the schema SHALL validate it is an array of strings
2. WHEN PrimaryKey is empty THEN the schema SHALL allow it (primary key is optional)
3. WHEN PrimaryKey contains values THEN the schema SHALL validate each value is a string
4. WHEN hovering over PrimaryKey THEN the IDE SHALL display documentation explaining primary key column references
5. WHEN PrimaryKey is defined THEN the IDE SHALL provide autocomplete for column names if possible

### Requirement 12

**User Story:** As a developer, I want the JSON Schema to include examples, so that I can quickly understand the expected format.

#### Acceptance Criteria

1. WHEN the JSON Schema is created THEN it SHALL include a complete example in the schema metadata
2. WHEN the JSON Schema defines complex structures THEN it SHALL include inline examples in property descriptions
3. WHEN a developer views the schema documentation THEN they SHALL see examples of minimal and complete configurations
4. WHEN the schema is used in an IDE THEN example snippets SHALL be available through IDE snippet features
5. WHEN examples are provided THEN they SHALL represent realistic use cases from the Brudixy project
