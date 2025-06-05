namespace JuleGeneratorLab.Models
{
    public class CodeSnippet
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        // Optional: List of identified placeholders, could be populated during loading
        // public List<string> Placeholders { get; set; } = new List<string>();
    }
}
