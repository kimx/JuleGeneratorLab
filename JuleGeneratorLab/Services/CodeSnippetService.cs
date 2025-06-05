using System.Text.Json;
using JuleGeneratorLab.Models;
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment
using Microsoft.JSInterop; // IJSRuntime
using System.Linq;
using System.Threading; // For SemaphoreSlim

namespace JuleGeneratorLab.Services
{
    public class CodeSnippetService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IJSRuntime _jsRuntime;

        public List<CodeSnippet> Snippets { get; private set; } = new List<CodeSnippet>();
        private List<CodeSnippet> _defaultSnippets = new List<CodeSnippet>();
        private List<CodeSnippet> _userSnippets = new List<CodeSnippet>();

        private bool _isInitialized = false;
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public CodeSnippetService(IWebHostEnvironment hostingEnvironment, IJSRuntime jsRuntime)
        {
            _hostingEnvironment = hostingEnvironment;
            _jsRuntime = jsRuntime;
        }

        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized) return; // Re-check after lock acquired

                LoadDefaultSnippets(); // Synchronous part
                await LoadUserSnippetsAsync(); // Asynchronous part
                MergeSnippets(); // Synchronous part

                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private void LoadDefaultSnippets()
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
                        _defaultSnippets = snippets.Select(s => { s.IsUserDefined = false; return s; }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    // Log error or handle appropriately
                    Console.WriteLine($"Error loading default snippets: {ex.Message}");
                    _defaultSnippets.Add(new CodeSnippet { Name = "Error Default", Description = "Could not load default snippets.", Template = ex.Message, IsUserDefined = false });
                }
            }
            else
            {
                 _defaultSnippets.Add(new CodeSnippet { Name = "Not Found Default", Description = "snippets.json not found.", Template = $"Path checked: {filePath}", IsUserDefined = false });
            }
        }

        private async Task LoadUserSnippetsAsync()
        {
            // User snippets will be loaded via upload or other file-based mechanisms.
            // For now, initialize as an empty list.
            _userSnippets = new List<CodeSnippet>();
            // Ensure await is used if any async operations were to be added here in the future.
            await Task.CompletedTask;
        }

        private void MergeSnippets()
        {
            // Simple merge: User snippets can override default snippets with the same name.
            // Create a dictionary from default snippets for easy lookup.
            var mergedSnippets = _defaultSnippets.ToDictionary(s => s.Name, s => s);

            foreach (var userSnippet in _userSnippets)
            {
                userSnippet.IsUserDefined = true; // Ensure flag is set
                mergedSnippets[userSnippet.Name] = userSnippet; // Add or overwrite
            }
            Snippets = mergedSnippets.Values.OrderBy(s => s.Name).ToList();
        }

        public async Task SaveUserSnippetsAsync()
        {
            // This method will be repurposed or used for download functionality.
            // For now, it does nothing or could trigger a download if implemented here.
            MergeSnippets(); // Refresh the public Snippets list after any in-memory changes.
            // Ensure await is used if any async operations were to be added here in the future.
            await Task.CompletedTask;
        }

        public async Task AddUserSnippetAsync(CodeSnippet snippet)
        {
            if (snippet == null || string.IsNullOrWhiteSpace(snippet.Name)) return;
            snippet.IsUserDefined = true;
            // Optional: Check for name collision with existing user snippets explicitly, or let it overwrite
            _userSnippets.RemoveAll(s => s.Name == snippet.Name); // Remove if exists to ensure update
            _userSnippets.Add(snippet);
            await SaveUserSnippetsAsync();
        }

        public async Task UpdateUserSnippetAsync(string originalName, CodeSnippet snippet)
        {
             if (snippet == null || string.IsNullOrWhiteSpace(snippet.Name)) return;
            snippet.IsUserDefined = true;
            _userSnippets.RemoveAll(s => s.Name == originalName); // Remove old one by original name
            _userSnippets.Add(snippet); // Add new one (potentially with new name)
            await SaveUserSnippetsAsync();
        }

        public async Task DeleteUserSnippetAsync(string snippetName)
        {
            _userSnippets.RemoveAll(s => s.Name == snippetName);
            await SaveUserSnippetsAsync();
        }

        public Task<string> GetUserSnippetsAsJsonAsync()
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(_userSnippets, new JsonSerializerOptions { WriteIndented = true });
                return Task.FromResult(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing user snippets: {ex.Message}");
                return Task.FromResult("[]"); // Return empty JSON array on error
            }
        }

        public Task<bool> LoadUserSnippetsFromJsonAsync(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                // Consider if this should clear existing user snippets or be a silent no-op
                // For now, let's assume an empty/whitespace string means "clear user snippets"
                _userSnippets = new List<CodeSnippet>();
                MergeSnippets();
                return Task.FromResult(true); // Successfully "loaded" empty set
            }

            try
            {
                var snippets = JsonSerializer.Deserialize<List<CodeSnippet>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (snippets != null)
                {
                    _userSnippets = snippets.Select(s => { s.IsUserDefined = true; return s; }).ToList();
                }
                else
                {
                    // If deserialization results in null (e.g. JSON was "null")
                    _userSnippets = new List<CodeSnippet>();
                }
                MergeSnippets();
                return Task.FromResult(true);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing user snippets from JSON: {ex.Message}");
                // Do not change _userSnippets on error
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while loading user snippets from JSON: {ex.Message}");
                // Do not change _userSnippets on error
                return Task.FromResult(false);
            }
        }
    }
}
