# Implementation Plan

- [x] 1. Create JSON Schema file structure





  - Create `schemas/brudixy-table-schema.json` file with basic metadata ($schema, $id, title, description)
  - Define root schema type as object with required fields (Table, Columns)
  - Set up definitions section for reusable schema components
  - _Requirements: 1.1, 1.3, 8.1, 8.2_
-

- [x] 2. Define column type validation patterns



  - [x] 2.1 Create regex pattern for built-in types (Int32, String, DateTime, etc.)


    - Define pattern that matches all built-in type names
    - Add support for type modifiers (?, !, [], <>)
    - Add support for inline options (| MaxLength | Index | Unique, etc.)
    - Include comprehensive examples in description
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 2.2 Write property test for column type pattern validation


    - **Property 2: Column type pattern matching**
    - **Validates: Requirements 2.1, 2.2, 2.3**

- [x] 3. Define CodeGenerationOptions schema





  - [x] 3.1 Create schema for CodeGenerationOptions object


    - Define properties: Namespace, Class, RowClass, Abstract, Sealed, BaseTableFileName, ExtraUsing, AppendRowMethodName
    - Add C# namespace pattern validation for Namespace property
    - Add C# identifier pattern validation for Class, RowClass properties
    - Add boolean type for Abstract and Sealed properties
    - Add array of strings for ExtraUsing with using directive pattern
    - Include descriptions for each property
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2_

  - [x] 3.2 Write property test for C# identifier validation


    - **Property 3: C# identifier validation**
    - **Validates: Requirements 4.1, 4.2**

- [x] 4. Define Columns and ColumnOptions schemas




  - [x] 4.1 Create schema for Columns object


    - Define as object with patternProperties for column names
    - Use columnType definition from step 2 for values
    - Add description explaining column definitions
    - _Requirements: 1.1, 2.1_

  - [x] 4.2 Create schema for ColumnOptions object


    - Define patternProperties for column names
    - Define nested properties: Type, AllowNull, IsUnique, HasIndex, IsReadOnly, IsService, Auto, MaxLength, DataType, Expression, DefaultValue, DisplayName, CodeProperty, EnumType, XProperties
    - Add type validation for each property (boolean, string, integer)
    - Add C# identifier pattern for CodeProperty
    - Reference XProperties definition
    - Include descriptions for each property
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [x] 4.3 Write property test for ColumnOptions validation


    - **Property 4: ColumnOptions structure validation**
    - **Validates: Requirements 4.3, 4.4, 4.5**

- [x] 5. Define Relations schema





  - [x] 5.1 Create schema for Relations object


    - Define patternProperties for relation names
    - Define required properties: ParentKey, ChildKey
    - Define optional properties: ParentTable, ChildTable
    - Validate ParentKey and ChildKey as non-empty arrays of strings
    - Validate ParentTable and ChildTable as strings
    - Include descriptions explaining parent/child relationships
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 5.2 Write property test for Relations validation


    - **Property 5: Relation structure validation**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.5_

- [x] 6. Define XProperties schema




  - [x] 6.1 Create reusable XProperty definition


    - Define properties: Type, DataType, Value, CodePropertyName, EnumType
    - Add anyOf constraint requiring either Type or DataType
    - Add C# identifier pattern for CodePropertyName
    - Allow any type for Value property
    - Include description explaining extended properties
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 6.2 Create XPropertiesMap definition


    - Define as object with patternProperties for property names
    - Reference XProperty definition for values
    - _Requirements: 7.4, 7.5_

  - [x] 6.3 Write property test for XProperties validation


    - **Property 6: XProperty structure validation**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**

- [ ] 7. Define GroupedProperties schemas




  - [x] 7.1 Create schema for GroupedProperties object


    - Define patternProperties for group names
    - Add pattern for pipe-separated column lists (minimum 2 columns)
    - Include description and examples
    - _Requirements: 6.1, 6.5_

  - [x] 7.2 Create schema for GroupedPropertyOptions object


    - Define patternProperties for group names
    - Define properties: Type, IsReadOnly, StructName
    - Add enum constraint for Type (Tuple, NewStruct)
    - Add boolean type for IsReadOnly
    - Add C# identifier pattern for StructName
    - Include descriptions
    - _Requirements: 6.2, 6.3, 6.4_

  - [x] 7.3 Write property test for GroupedProperties validation


    - **Property 7: GroupedProperty format validation**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
-

- [x] 8. Define remaining top-level properties



  - [x] 8.1 Create schema for PrimaryKey property


    - Define as array of strings
    - Allow empty array (optional primary key)
    - Include description explaining primary key column references
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [x] 8.2 Create schema for RowSubTypes and RowSubTypeOptions


    - Define RowSubTypes as object with patternProperties (name to expression)
    - Define RowSubTypeOptions with XProperties support
    - Include descriptions
    - _Requirements: 1.1_

  - [x] 8.3 Create schema for Indexes property


    - Define as object with patternProperties for index names
    - Define index structure with columns array
    - Include description
    - _Requirements: 1.1_

  - [x] 8.4 Create schema for EnforceConstraints property


    - Define as boolean type
    - Include description
    - _Requirements: 1.1_

  - [x] 8.5 Write property test for PrimaryKey validation


    - **Property 9: Array property validation**
    - **Validates: Requirements 11.1, 11.3**

- [x] 9. Add comprehensive examples to schema





  - [x] 9.1 Add complete example in schema metadata


    - Create example showing minimal valid YAML
    - Create example showing complex YAML with all features
    - Add examples array to root schema
    - _Requirements: 12.1, 12.2, 12.5_

  - [x] 9.2 Add inline examples to complex properties


    - Add examples to column type definition
    - Add examples to CodeGenerationOptions
    - Add examples to Relations
    - Add examples to GroupedProperties
    - Add examples to XProperties
    - _Requirements: 12.2, 12.5_

  - [x] 9.3 Write property test to validate examples


    - **Property 13: Example validity**
    - **Validates: Requirements 12.5**

- [x] 10. Add comprehensive documentation




  - [x] 10.1 Add descriptions to all properties


    - Ensure every property has a description field
    - Include format requirements in descriptions
    - Add examples where helpful
    - _Requirements: 1.2, 9.1, 9.2, 9.4_

  - [x] 10.2 Write property test for documentation completeness


    - **Property 12: Documentation completeness**
    - **Validates: Requirements 1.2, 9.1, 9.4**

- [x] 11. Validate schema structure





  - [x] 11.1 Validate schema is valid JSON Schema Draft 7


    - Use JSON Schema validator to validate the schema itself
    - Ensure all $ref references resolve correctly
    - Ensure all regex patterns compile
    - _Requirements: 8.1_

  - [x] 11.2 Write unit tests for schema structure


    - Test schema has required metadata ($schema, $id, title)
    - Test all definitions are present
    - Test all patterns are valid regex
    - Test all references resolve
    - _Requirements: 8.1, 8.2_

- [x] 12. Test schema validation behavior





  - [x] 12.1 Write property tests for schema validation


    - **Property 1: Schema validation completeness**
    - **Validates: Requirements 1.1, 1.3, 1.5**
    - Generate random valid YAML and verify schema accepts it
    - Generate random invalid YAML and verify schema rejects it

  - [x] 12.2 Write integration tests with real YAML files


    - Test schema validates TestBaseTable.st.brudixy.yaml
    - Test schema validates TestGroupTable.st.brudixy.yaml
    - Test schema rejects known invalid YAML files
    - _Requirements: 1.5_

- [x] 13. Create schema hosting setup




  - [x] 13.1 Create schemas directory in repository


    - Create `schemas/` directory in repository root
    - Copy schema file to `schemas/brudixy-table-schema.json`
    - Create versioned copies (e.g., `schemas/v1.0.0/brudixy-table-schema.json`)
    - _Requirements: 10.1, 10.5_

  - [x] 13.2 Create documentation for schema usage


    - Document how to use schema with inline directive
    - Document how to configure schema in VS Code
    - Document how to configure schema in JetBrains IDEs
    - Document how to configure schema in Visual Studio
    - Create README.md in schemas directory
    - _Requirements: 10.2, 10.3_

- [x] 14. Prepare JSON Schema Store submission




  - [x] 14.1 Create catalog entry for JSON Schema Store


    - Create catalog.json entry with name, description, fileMatch, and url
    - Document file patterns: *.st.brudixy.yaml, *.dt.brudixy.yaml, *.ds.brudixy.yaml
    - Document submission process in CONTRIBUTING.md
    - _Requirements: 8.3_

  - [x] 14.2 Create pull request template for schema store


    - Document what to include in PR description
    - Include testing checklist
    - Include example YAML files for testing
    - _Requirements: 8.3_

- [x] 15. Final checkpoint - Ensure all tests pass





  - Ensure all tests pass, ask the user if questions arise.
