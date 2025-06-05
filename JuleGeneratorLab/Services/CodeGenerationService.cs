using JuleGeneratorLab.Models; // For CodeSnippet and ColumnDetail
using System.Text;
using System.Text.RegularExpressions; // For more advanced placeholder parsing if needed

namespace JuleGeneratorLab.Services
{
    public class CodeGenerationService
    {
        public string GenerateCode(string tableName, List<ColumnDetail> selectedColumns, CodeSnippet snippet)
        {
            if (string.IsNullOrEmpty(tableName) || !selectedColumns.Any() || snippet == null)
            {
                return "// Error: Table name, columns, or snippet not provided.";
            }

            StringBuilder output = new StringBuilder(snippet.Template);
            string className = NormalizeClassName(tableName);

            // Simple global replacements
            output.Replace("{ClassName}", className);
            output.Replace("{ClassName}s", className + "s"); // Simple pluralization
            output.Replace("{TableName}", tableName);
            // Add more global placeholders as needed e.g. {Namespace}

            // Handle property/field generation (example for C# Model or Blazor Inputs)
            // This is a common pattern that might need more sophisticated parsing for different snippet types.
            // The current snippets.json uses "Helper Snippets" in comments, which this basic version doesn't parse.
            // This example directly replaces placeholders like {Properties} or {Fields}.

            if (snippet.Name == "C# Model Class")
            {
                StringBuilder properties = new StringBuilder();
                foreach (var col in selectedColumns)
                {
                    properties.AppendLine($"    public {MapColumnTypeToCSharp(col.DataType)} {NormalizePropertyName(col.ColumnName)} {{ get; set; }}");
                }
                output.Replace("{Properties}", properties.ToString().TrimEnd());
            }
            else if (snippet.Name == "Blazor EditForm Inputs")
            {
                StringBuilder fields = new StringBuilder();
                foreach (var col in selectedColumns)
                {
                    string propertyName = NormalizePropertyName(col.ColumnName);
                    string propertyNameLower = propertyName.ToLowerInvariant();
                    fields.AppendLine($"    <div class=\"form-group\">");
                    fields.AppendLine($"        <label for=\"{propertyNameLower}\">{propertyName}:</label>");
                    fields.AppendLine($"        <InputText id=\"{propertyNameLower}\" class=\"form-control\" @bind-Value=\"Model.{propertyName}\" />");
                    fields.AppendLine($"        <ValidationMessage For=\"@(() => Model.{propertyName})\" />");
                    fields.AppendLine($"    </div>");
                }
                output.Replace("{Fields}", fields.ToString().TrimEnd());
            }
            else if (snippet.Name == "Simple Console Output")
            {
                // Example of a loop structure based on placeholders
                // This requires the template to have {ColumnLoopStart} and {ColumnLoopEnd}
                // And the content between them is the template for each column.
                Regex loopRegex = new Regex(@"{ColumnLoopStart}(.*?){ColumnLoopEnd}", RegexOptions.Singleline);
                Match loopMatch = loopRegex.Match(output.ToString());

                if (loopMatch.Success)
                {
                    string loopTemplate = loopMatch.Groups[1].Value;
                    StringBuilder allColumnsOutput = new StringBuilder();
                    foreach (var col in selectedColumns)
                    {
                        StringBuilder singleColumnOutput = new StringBuilder(loopTemplate);
                        singleColumnOutput.Replace("{ColumnName}", col.ColumnName);
                        singleColumnOutput.Replace("{ColumnDataType}", col.DataType);
                        // Add more column-specific placeholders as needed
                        allColumnsOutput.Append(singleColumnOutput.ToString());
                    }
                    output = new StringBuilder(loopRegex.Replace(output.ToString(), allColumnsOutput.ToString()));
                }
            }
            // Add more complex logic here for different snippet types and placeholders.
            // This might involve parsing "sub-templates" or more structured placeholder definitions.

            return output.ToString();
        }

        private string NormalizeClassName(string tableName)
        {
            // Basic normalization: remove spaces, underscores, and capitalize parts
            string[] parts = tableName.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Concat(parts);
        }

        private string NormalizePropertyName(string columnName)
        {
            // Similar to class name, but typically starts with uppercase in C# properties
            string[] parts = columnName.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Concat(parts);
        }

        private string MapColumnTypeToCSharp(string dbType)
        {
            // This is a simplified mapping. Needs to be comprehensive.
            dbType = dbType.ToLowerInvariant();
            switch (dbType)
            {
                case "int":
                case "integer":
                case "smallint":
                case "tinyint":
                    return "int";
                case "bigint":
                    return "long";
                case "nvarchar":
                case "varchar":
                case "char":
                case "text":
                case "ntext":
                    return "string";
                case "datetime":
                case "smalldatetime":
                case "date":
                case "time":
                    return "DateTime";
                case "bit":
                    return "bool";
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    return "decimal";
                case "float":
                    return "double"; // float in SQL is double precision
                case "real":
                    return "float";  // real in SQL is single precision
                case "uniqueidentifier":
                    return "Guid";
                case "varbinary":
                case "binary":
                case "image":
                    return "byte[]";
                // Add more mappings as needed
                default:
                    return "object"; // Fallback
            }
        }
    }
}
