using System.Text.Json;
using JuleGeneratorLab.Models; // Assuming CodeSnippet is in Models

namespace JuleGeneratorLab.Services
{
    public class CodeSnippetService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        public List<CodeSnippet> Snippets { get; private set; } = new List<CodeSnippet>();

        public CodeSnippetService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            LoadSnippets();
        }

        private void LoadSnippets()
        {
            // Path for development (running from project directory)
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "snippets.json");

            // Path for deployed environment (content root)
            if (!File.Exists(filePath))
            {
                 filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Data", "snippets.json");
            }

            if (File.Exists(filePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var snippets = JsonSerializer.Deserialize<List<CodeSnippet>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (snippets != null)
                    {
                        Snippets = snippets;
                    }
                }
                catch (Exception ex)
                {
                    // Log error or handle appropriately
                    Console.WriteLine($"Error loading snippets: {ex.Message}");
                    Snippets = new List<CodeSnippet> {
                        new CodeSnippet { Name = "Error", Description = "Could not load snippets.", Template = ex.Message }
                    };
                }
            }
            else
            {
                 Snippets = new List<CodeSnippet> {
                     new CodeSnippet { Name = "Not Found", Description = "snippets.json not found.", Template = $"Path checked: {filePath}" }
                 };
            }
        }
    }
}
