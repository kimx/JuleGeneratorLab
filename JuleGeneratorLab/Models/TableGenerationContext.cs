using System.Collections.Generic; // Required for List
// Potentially: using JuleGeneratorLab.Models; if ColumnDetail is in a different namespace (it's in the same, Models)

namespace JuleGeneratorLab.Models
{
    public class TableGenerationContext
    {
        public string TableName { get; set; } = string.Empty;
        public string UserAlias { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty; // This will be populated by the service or Generator.razor
        public List<ColumnDetail> SelectedColumns { get; set; } = new List<ColumnDetail>();

        // Optional: Could be used by snippets to identify a 'main' or 'master' table in a list.
        // Default to false. UI/Logic in Generator.razor would set this if needed.
        public bool IsPrimaryInContext { get; set; } = false;
    }
}
