# Implementation Plan

- [x] 1. Create validation infrastructure and core types


  - Create ValidationResult, ValidationError, and ValidationWarning classes
  - Create ValidationContext class to hold validation state
  - Create IValidationRule interface for validation rules
  - Create SchemaValidationEngine class to orchestrate validation
  - _Requirements: 1.1, 9.1, 9.2_



- [x] 1.1 Write property test for validation completeness




  - **Property 1: Validation completeness**
  - **Validates: Requirements 1.1, 1.3, 1.4, 9.3**



- [x] 2. Implement column type parsing and validation

  - Create ColumnTypeInfo class to represent parsed column types



  - Implement Parse method to extract base type and modifiers from type strings
  - Implement validation for built-in types vs user-defined types
  - Implement validation for type modifier syntax and mutual exclusivity
  - _Requirements: 2.1, 2.2, 2.4_

- [x] 2.1 Write property test for column type validity

  - **Property 3: Column type validity**
  - **Validates: Requirements 2.1, 2.2, 2.4**

- [x] 3. Implement column reference validation rules

  - Create ColumnReferenceValidator to check column existence
  - Implement validation for PrimaryKey column references
  - Implement validation for Relation column references
  - Implement validation for Index column references
  - Implement validation for GroupedProperty column references
  - Handle inherited columns from base tables
  - _Requirements: 3.2, 5.1, 5.3, 6.1_

- [x] 3.1 Write property test for column reference existence

  - **Property 2: Column reference existence**
  - **Validates: Requirements 3.2, 5.1, 5.3, 6.1, 6.5**

- [x] 4. Implement constraint validation rules



  - Create ConstraintValidator for primary key and unique constraints
  - Implement validation that primary key columns are non-nullable
  - Implement validation that unique columns are non-nullable
  - Implement validation for conflicting AllowNull and type modifiers
  - Implement validation for duplicate column names in PrimaryKey
  - _Requirements: 2.3, 5.2, 5.5_

- [x] 4.1 Write property test for type constraint consistency

  - **Property 4: Type constraint consistency**
  - **Validates: Requirements 2.3, 5.2**

- [x] 4.2 Write property test for primary key uniqueness

  - **Property 11: Primary key uniqueness**
  - **Validates: Requirements 5.5**

- [ ] 5. Implement relation validation rules
  - Create RelationValidator for foreign key validation
  - Implement validation that ParentKey and ChildKey are non-empty
  - Implement validation that key array lengths match
  - Implement validation that referenced tables exist
  - Implement validation for self-referencing relations
  - _Requirements: 3.1, 3.3, 3.4, 3.5_

- [ ] 5.1 Write property test for relation key symmetry
  - **Property 5: Relation key symmetry**
  - **Validates: Requirements 3.1, 3.3**

- [x] 6. Implement code generation option validation rules



  - Create CodeGenOptionsValidator for generation options
  - Implement C# namespace naming convention validation
  - Implement C# identifier validation for Class and RowClass
  - Implement validation that Abstract and Sealed are not both true
  - Implement validation for ExtraUsing directive format
  - _Requirements: 4.1, 4.2, 4.3, 4.5_


- [ ] 6.1 Write property test for code generation option validity
  - **Property 6: Code generation option validity**
  - **Validates: Requirements 4.1, 4.2, 4.3**

- [ ] 7. Implement base table validation and cycle detection
  - Create BaseTableValidator for inheritance validation
  - Implement base table file existence and readability checks
  - Implement cycle detection algorithm for base table chains
  - Implement base table loading precedence logic
  - Implement derived table column conflict detection
  - _Requirements: 4.4, 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 7.1 Write property test for base table acyclic dependency
  - **Property 7: Base table acyclic dependency**
  - **Validates: Requirements 8.4**

- [ ] 7.2 Write property test for base table loading precedence
  - **Property 14: Base table loading precedence**
  - **Validates: Requirements 8.1, 8.5**

- [ ] 7.3 Write property test for derived table column compatibility
  - **Property 15: Derived table column compatibility**
  - **Validates: Requirements 8.2, 8.3**

- [ ] 8. Implement XProperty validation rules
  - Create XPropertyValidator for extended property validation
  - Implement validation that Type or DataType is specified
  - Implement C# type name validation for Type field
  - Implement type compatibility validation for Value field
  - Handle both table-level and column-level XProperties
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 8.1 Write property test for XProperty structure validity
  - **Property 8: XProperty structure validity**
  - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**

- [ ] 9. Implement grouped property validation rules
  - Create GroupedPropertyValidator for tuple/struct validation
  - Implement validation that all referenced columns exist
  - Implement validation that Type is "Tuple" or "NewStruct"
  - Implement warning for grouped properties with fewer than 2 columns
  - Implement warning for unused StructName with Tuple type
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [ ] 9.1 Write property test for warning non-blocking behavior
  - **Property 10: Warning non-blocking behavior**
  - **Validates: Requirements 1.5, 6.3, 6.4**

- [ ] 10. Implement error message formatting and reporting
  - Implement error message builder with file path inclusion
  - Implement property path generation in dot notation
  - Implement suggested fix generation for common errors
  - Implement error collection without early termination
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 10.1 Write property test for validation error message completeness
  - **Property 9: Validation error message completeness**
  - **Validates: Requirements 1.2, 9.1, 9.2**

- [ ] 11. Integrate validation engine with SchemaValidationEngine
  - Register all validation rules with the engine
  - Implement rule execution orchestration
  - Implement exception handling for validation rules
  - Implement deterministic rule ordering
  - Implement ValidationResult aggregation
  - _Requirements: 10.3, 10.4_

- [ ] 11.1 Write property test for validation rule exception handling
  - **Property 12: Validation rule exception handling**
  - **Validates: Requirements 10.4**

- [ ] 11.2 Write property test for validation rule execution order determinism
  - **Property 13: Validation rule execution order determinism**
  - **Validates: Requirements 10.3**

- [ ] 12. Integrate validation with YamlSchemaReader
  - Modify YamlSchemaReader to accept validation flag
  - Add ValidateTable method to YamlSchemaReader
  - Integrate SchemaValidationEngine into deserialization pipeline
  - Handle validation errors during schema loading
  - _Requirements: 1.1, 1.3_

- [x] 13. Integrate validation with TypeGenerator



  - Modify TypeGenerator to call validation before code generation
  - Implement diagnostic reporting for validation errors
  - Implement diagnostic reporting for validation warnings
  - Implement MSBuild property for disabling validation
  - Implement MSBuild property for strict validation mode
  - Block code generation when validation errors exist
  - _Requirements: 1.1, 1.3, 1.4, 1.5_

- [ ] 14. Create test fixtures and example schemas
  - Create valid minimal schema example
  - Create valid complex schema with all features
  - Create invalid schemas for each error category
  - Create edge case schemas (empty tables, single column, etc.)
  - Create base table inheritance test scenarios
  - _Requirements: All_

- [ ] 14.1 Write integration tests for complete validation pipeline
  - Test validation with real YAML files from fixtures
  - Test error message format matches expected output
  - Test base table inheritance validation
  - Test file system error handling
  - _Requirements: All_

- [ ] 15. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Add documentation and examples
  - Document validation error codes and messages
  - Create examples of common validation errors and fixes
  - Document MSBuild properties for validation configuration
  - Update README with validation feature description
  - _Requirements: 9.4_
