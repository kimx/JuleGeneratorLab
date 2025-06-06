using Microsoft.VisualStudio.TestTools.UnitTesting;
using JuleGeneratorLab.Services;
using JuleGeneratorLab.Models;
using System.Collections.Generic;

namespace JuleGeneratorLab.Tests.Services
{
    [TestClass]
    public class CodeGenerationServiceTests
    {
        [TestMethod]
        public void GenerateCode_WithNamespaceAndProgramName_AreAvailableInTemplate()
        {
            // Arrange
            var codeGenService = new CodeGenerationService();
            var tableName = "TestTable";
            var columns = new List<ColumnDetail> { new ColumnDetail { ColumnName = "DummyColumn", DataType = "int" } }; // Needs at least one column
            var snippet = new CodeSnippet
            {
                Name = "TestSnippet",
                Template = @"namespace {{ NameSpace }};
// Program: {{ ProgramName }}
public class {{ ClassName }} { }"
            };
            var namespaceValue = "MyTest.Namespace";
            var programNameValue = "MyTestGenerator";

            // Act
            string result = codeGenService.GenerateCode(tableName, columns, snippet, namespaceValue, programNameValue);

            // Assert
            StringAssert.Contains(result, "namespace MyTest.Namespace;");
            StringAssert.Contains(result, "// Program: MyTestGenerator");
            StringAssert.Contains(result, "public class Testtable { }"); // Corrected to align with NormalizeClassName behavior
        }

        [TestMethod]
        public void GenerateCode_WithEmptyNamespaceAndProgramName_RendersEmpty()
        {
            // Arrange
            var codeGenService = new CodeGenerationService();
            var tableName = "TestTable";
            var columns = new List<ColumnDetail> { new ColumnDetail { ColumnName = "DummyColumn", DataType = "int" } }; // Needs at least one column
            var snippet = new CodeSnippet
            {
                Name = "TestSnippet",
                Template = @"namespace {{ NameSpace }};
// Program: {{ ProgramName }}
public class {{ ClassName }} { }"
            };
            var namespaceValue = "";
            var programNameValue = "";

            // Act
            string result = codeGenService.GenerateCode(tableName, columns, snippet, namespaceValue, programNameValue);

            // Assert
            StringAssert.Contains(result, "namespace ;");
            StringAssert.Contains(result, "// Program: ");
        }

        [TestMethod]
        public void GenerateCode_WithNullNamespaceAndProgramName_RendersEmpty()
        {
            // Arrange
            var codeGenService = new CodeGenerationService();
            var tableName = "TestTable";
            var columns = new List<ColumnDetail> { new ColumnDetail { ColumnName = "DummyColumn", DataType = "int" } }; // Needs at least one column
            var snippet = new CodeSnippet
            {
                Name = "TestSnippet",
                Template = @"namespace {{ NameSpace }};
// Program: {{ ProgramName }}
public class {{ ClassName }} { }"
            };
            // namespaceValue and programNameValue are null by default when calling the method

            // Act
            string result = codeGenService.GenerateCode(tableName, columns, snippet, null, null);

            // Assert
            StringAssert.Contains(result, "namespace ;");
            StringAssert.Contains(result, "// Program: ");
        }

        // Placeholder for a more comprehensive test that was in the original thought process
        // This test would ensure basic generation still works with the new parameters added.
        [TestMethod]
        public void GenerateCode_BasicScenario_WithNewParametersAsEmpty()
        {
            // Arrange
            var service = new CodeGenerationService();
            string tableName = "SampleTable";
            var columns = new List<ColumnDetail>
            {
                new ColumnDetail { ColumnName = "Id", DataType = "int", IsPrimaryKey = true, IsNullable = false },
                new ColumnDetail { ColumnName = "Name", DataType = "nvarchar", IsPrimaryKey = false, IsNullable = true }
            };
            var snippet = new CodeSnippet
            {
                Name = "CSharpModel",
                Template = "public class {{ ClassName }}\n{\n    {{ for col in SelectedColumns }}\n    public {{ map_db_type_to_csharp col.DataType }}{{ if col.IsNullable && map_db_type_to_csharp col.DataType != \"string\" && map_db_type_to_csharp col.DataType != \"byte[]\"}}?{{end}} {{ normalize_property_name col.ColumnName }} { get; set; }\n    {{ end }}\n}"
            };
            string namespaceValue = ""; // New parameter
            string programNameValue = ""; // New parameter

            // Act
            string result = service.GenerateCode(tableName, columns, snippet, namespaceValue, programNameValue);

            // Assert
            StringAssert.Contains(result, "public class Sampletable"); // Corrected to align with NormalizeClassName behavior
            StringAssert.Contains(result, "public int Id { get; set; }");
            StringAssert.Contains(result, "public string? Name { get; set; }"); // nvarchar maps to string, nullable
        }
    }
}
