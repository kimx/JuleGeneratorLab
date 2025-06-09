using System.ComponentModel.DataAnnotations;

namespace JuleGeneratorLab.Models
{
    public enum SnippetApplicability
    {
        Any,
        Main,
        Detail
    }

    public class CodeSnippet
    {
        [Required(ErrorMessage = "Snippet name is required.")]
        [StringLength(100, ErrorMessage = "Snippet name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Snippet template cannot be empty.")]
        public string Template { get; set; } = string.Empty;

        public SnippetApplicability Applicability { get; set; } = SnippetApplicability.Any;
        public string OutputFileExtension { get; set; } = ".txt";
        public bool IsUserDefined { get; set; } = false;
        // Optional: List of identified placeholders, could be populated during loading
        // public List<string> Placeholders { get; set; } = new List<string>();
    }
}
