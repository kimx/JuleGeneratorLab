using JuleGeneratorLab.Models; // For CodeSnippet and ColumnDetail
using System.Text;
// using System.Text.RegularExpressions; // No longer needed for the core GenerateCode logic
using System.Linq; // Keep for List.Any() and template.Messages.Select()
using System.Collections.Generic; // Keep for List<object>
using System; // Keep for Func, Exception, StringSplitOptions, DBNull etc.
using Scriban;
using Scriban.Runtime; // For ScriptObject and MemberRenamerDelegate

namespace JuleGeneratorLab.Services
{
    public class CodeGenerationService
    {
        public string GenerateCode(TableGenerationContext mainTableContext, CodeSnippet snippet, string namespaceValue, string programNameValue, TableGenerationContext? detailTableContext = null)
        {
            if (mainTableContext == null)
            {
                return "// Error: Main table context is null.";
            }
            if (snippet == null || string.IsNullOrWhiteSpace(snippet.Template))
            {
                return "// Error: Snippet or snippet template not provided or empty.";
            }

            try
            {
                var template = Template.Parse(snippet.Template);

                if (template.HasErrors)
                {
                    var errors = template.Messages.Select(m => m.ToString()).ToList();
                    return $"// Snippet Template Parsing Error(s):\n// {string.Join("\n// ", errors)}";
                }

                var scriptObject = new ScriptObject();
                // Add the main table context
                scriptObject.Add("MainTable", mainTableContext);

                // Conditionally add the detail table context
                if (detailTableContext != null)
                {
                    scriptObject.Add("DetailTable", detailTableContext);
                }

                // Reconstruct a Tables list for backward compatibility or general use
                var tablesListForContext = new List<TableGenerationContext>();
                tablesListForContext.Add(mainTableContext);
                if (detailTableContext != null)
                {
                    tablesListForContext.Add(detailTableContext);
                }
                scriptObject.Add("Tables", tablesListForContext);

                // Add namespace and program name
                scriptObject.Add("NameSpace", namespaceValue ?? "");
                scriptObject.Add("ProgramName", programNameValue ?? "");

                // Note: Old TableName, ClassName, and SelectedColumns are removed from direct scriptObject addition.
                // These are now accessible via the 'Tables' list, e.g., {{ Tables[0].ClassName }}
                // The ClassName in each TableGenerationContext should be pre-normalized by the caller.

                var context = new TemplateContext();
                context.MemberRenamer = member => member.Name;
                context.PushGlobal(scriptObject);

                var utilityFunctions = new ScriptObject();
                utilityFunctions.Import("normalize_property_name", new Func<string, string>(NormalizePropertyName));
                utilityFunctions.Import("map_db_type_to_csharp", new Func<string, string>(MapColumnTypeToCSharp));

                context.PushGlobal(utilityFunctions);

                string result = template.Render(context);
                return result;
            }
            catch (Exception ex)
            {
                return $"// Error during code generation: {ex.Message}\n// StackTrace: {ex.StackTrace}";
            }
        }

        public string NormalizeClassName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) return "DefaultClassName";
            string[] parts = tableName.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
            }
            return string.Concat(parts);
        }

        public string NormalizePropertyName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return "DefaultPropertyName";
            string[] parts = columnName.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                // PascalCase for property names
                sb.Append(char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant());
            }
            return sb.ToString();
        }

        public string MapColumnTypeToCSharp(string dbType)
        {
            if (string.IsNullOrWhiteSpace(dbType)) return "object"; // Fallback for empty or null dbType
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
                    return "double"; // SQL float is double precision
                case "real":
                    return "float";  // SQL real is single precision
                case "uniqueidentifier":
                    return "Guid";
                case "varbinary":
                case "binary":
                case "image":
                case "rowversion": // Common SQL Server type for optimistic concurrency
                case "timestamp":  // Often an alias for rowversion
                    return "byte[]";
                // Add more specific mappings as identified
                default:
                    // Consider logging unknown db types if necessary
                    Console.WriteLine($"Warning: Unknown DB type '{dbType}' mapped to 'object'.");
                    return "object"; // Default fallback
            }
        }
    }
}
