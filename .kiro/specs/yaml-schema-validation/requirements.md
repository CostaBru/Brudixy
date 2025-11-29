# Requirements Document

## Introduction

This document defines the requirements for implementing a YAML schema validation system for the Brudixy TypeGenerator. The TypeGenerator currently parses YAML files (`.brudixy.yaml`) to generate strongly-typed DataTable and DataSet classes, but lacks formal schema validation. This feature will enforce schema correctness, provide clear error messages, and prevent invalid configurations from reaching the code generation phase.

## Glossary

- **TypeGenerator**: The Brudixy source generator that reads YAML schema files and generates C# code for strongly-typed data tables and datasets
- **YAML Schema**: A YAML file with `.brudixy.yaml` extension that defines table structure, columns, relations, and code generation options
- **YamlSchemaReader**: The component responsible for deserializing YAML content into DataTableObj instances
- **DataTableObj**: The internal representation of a table schema after YAML deserialization
- **ColumnInfo**: Metadata describing a column including type, constraints, and generation options
- **Schema Validation**: The process of verifying that a YAML file conforms to the expected structure and rules before code generation

## Requirements

### Requirement 1

**User Story:** As a developer using Brudixy, I want the TypeGenerator to validate my YAML schema files against a formal schema, so that I receive clear error messages when my schema is malformed.

#### Acceptance Criteria

1. WHEN a developer builds a project with YAML schema files THEN the TypeGenerator SHALL validate each YAML file against the formal schema before attempting code generation
2. WHEN a YAML file contains invalid structure or missing required fields THEN the TypeGenerator SHALL report specific validation errors with file name and line number
3. WHEN all YAML files pass validation THEN the TypeGenerator SHALL proceed with code generation
4. WHEN validation errors occur THEN the TypeGenerator SHALL prevent code generation and display all validation errors in the build output
5. WHEN a YAML file uses deprecated or unknown properties THEN the TypeGenerator SHALL emit warnings without blocking code generation

### Requirement 2

**User Story:** As a developer, I want the schema to validate column type definitions, so that I catch type errors before code generation.

#### Acceptance Criteria

1. WHEN a column type is specified THEN the system SHALL verify it matches a known built-in type or user-defined type pattern
2. WHEN a column uses type modifiers (nullable `?`, non-null `!`, array `[]`, range `<>`) THEN the system SHALL validate the modifier syntax is correct
3. WHEN a column specifies conflicting options (e.g., `AllowNull: false` with nullable type `string?`) THEN the system SHALL report a validation error
4. WHEN a column type includes inline options (e.g., `String | 256 | Index`) THEN the system SHALL parse and validate each option
5. WHEN a column references an enum type THEN the system SHALL verify the EnumType property is specified in ColumnOptions

### Requirement 3

**User Story:** As a developer, I want the schema to validate table relationships, so that I define correct foreign key constraints.

#### Acceptance Criteria

1. WHEN a relation is defined THEN the system SHALL verify both ParentKey and ChildKey arrays are specified and non-empty
2. WHEN a relation references columns THEN the system SHALL verify the referenced columns exist in the respective tables
3. WHEN a relation has mismatched key array lengths THEN the system SHALL report a validation error
4. WHEN a relation references a table THEN the system SHALL verify the table exists in the dataset or is the current table
5. WHEN a self-referencing relation is defined THEN the system SHALL validate both ParentTable and ChildTable are correctly specified

### Requirement 4

**User Story:** As a developer, I want the schema to validate code generation options, so that I avoid invalid namespace or class name configurations.

#### Acceptance Criteria

1. WHEN CodeGenerationOptions specifies a Namespace THEN the system SHALL verify it follows valid C# namespace naming conventions
2. WHEN CodeGenerationOptions specifies Class or RowClass names THEN the system SHALL verify they follow valid C# identifier rules
3. WHEN Abstract and Sealed are both set to true THEN the system SHALL report a validation error
4. WHEN BaseTableFileName is specified THEN the system SHALL verify the file exists and is readable
5. WHEN ExtraUsing contains entries THEN the system SHALL verify each entry is a valid using directive format

### Requirement 5

**User Story:** As a developer, I want the schema to validate primary keys and indexes, so that I define valid constraints.

#### Acceptance Criteria

1. WHEN PrimaryKey is specified THEN the system SHALL verify all referenced columns exist in the Columns definition
2. WHEN a column is marked with IsUnique THEN the system SHALL verify AllowNull is not true
3. WHEN an Index is defined THEN the system SHALL verify all columns in the index exist
4. WHEN a column has both HasIndex and IsUnique set to true THEN the system SHALL create a unique index
5. WHEN PrimaryKey contains duplicate column names THEN the system SHALL report a validation error

### Requirement 6

**User Story:** As a developer, I want the schema to validate grouped properties, so that I correctly define tuple or struct groupings.

#### Acceptance Criteria

1. WHEN GroupedProperties defines a group THEN the system SHALL verify all referenced columns exist
2. WHEN GroupedPropertyOptions specifies a Type THEN the system SHALL verify it is either "Tuple" or "NewStruct"
3. WHEN a grouped property has fewer than two columns THEN the system SHALL emit a warning
4. WHEN GroupedPropertyOptions specifies StructName with Type "Tuple" THEN the system SHALL emit a warning about unused property
5. WHEN a grouped property references non-existent columns THEN the system SHALL report a validation error

### Requirement 7

**User Story:** As a developer, I want the schema to validate XProperties (extended properties), so that I define metadata correctly.

#### Acceptance Criteria

1. WHEN an XProperty is defined THEN the system SHALL verify it has either a Type or DataType specified
2. WHEN an XProperty specifies a Type THEN the system SHALL verify it is a valid C# type name
3. WHEN an XProperty specifies a Value THEN the system SHALL verify it is compatible with the specified Type
4. WHEN XProperties are defined at table level THEN the system SHALL validate their structure
5. WHEN XProperties are defined at column level THEN the system SHALL validate their structure independently

### Requirement 8

**User Story:** As a developer, I want the schema validator to support inheritance from base tables, so that I can validate derived table schemas correctly.

#### Acceptance Criteria

1. WHEN a table specifies BaseTableFileName THEN the system SHALL load and validate the base table schema first
2. WHEN a derived table defines columns THEN the system SHALL verify they do not conflict with base table columns
3. WHEN a derived table overrides ColumnOptions THEN the system SHALL validate the overrides are compatible with base definitions
4. WHEN circular base table references are detected THEN the system SHALL report a validation error
5. WHEN a base table file cannot be loaded THEN the system SHALL report a clear error with the file path

### Requirement 9

**User Story:** As a developer, I want comprehensive error messages from schema validation, so that I can quickly fix issues.

#### Acceptance Criteria

1. WHEN a validation error occurs THEN the system SHALL include the YAML file path in the error message
2. WHEN a validation error relates to a specific property THEN the system SHALL include the property path (e.g., "ColumnOptions.id.AllowNull")
3. WHEN multiple validation errors exist THEN the system SHALL report all errors, not just the first one
4. WHEN a validation error occurs THEN the system SHALL suggest possible corrections when applicable
5. WHEN validation succeeds THEN the system SHALL not emit any validation-related messages

### Requirement 10

**User Story:** As a developer, I want the schema validator to be extensible, so that custom validation rules can be added in the future.

#### Acceptance Criteria

1. WHEN the validation system is designed THEN it SHALL use a plugin or rule-based architecture
2. WHEN new validation rules are added THEN they SHALL not require changes to the core validation engine
3. WHEN validation rules are executed THEN they SHALL run in a deterministic order
4. WHEN a validation rule throws an exception THEN the system SHALL catch it and report it as a validation error
5. WHEN custom validators are registered THEN they SHALL be able to access the full DataTableObj context
