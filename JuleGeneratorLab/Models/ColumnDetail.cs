namespace JuleGeneratorLab.Models
{
    public class ColumnDetail
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        // Add other properties as needed, e.g., MaxLength
    }
}
