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
        private readonly string _userSnippetsFilePath;

        public List<CodeSnippet> Snippets { get; private set; } = new List<CodeSnippet>();
        private List<CodeSnippet> _defaultSnippets = new List<CodeSnippet>();
        private List<CodeSnippet> _userSnippets = new List<CodeSnippet>();

        private bool _isInitialized = false;
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public CodeSnippetService(IWebHostEnvironment hostingEnvironment, IJSRuntime jsRuntime)
        {
            _hostingEnvironment = hostingEnvironment;
            _jsRuntime = jsRuntime;

            // Define the path for user_snippets.json
            // Directory creation will be handled in the SaveUserSnippetsAsync method if/when it writes to this path.
            string dataDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Data");
            _userSnippetsFilePath = Path.Combine(dataDirectory, "user_snippets.json");
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
            if (File.Exists(_userSnippetsFilePath))
            {
                try
                {
                    string jsonContent = await File.ReadAllTextAsync(_userSnippetsFilePath);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
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
                            _userSnippets = new List<CodeSnippet>();
                            // Log warning: Deserializing user snippets resulted in null.
                            Console.WriteLine($"Warning: Deserializing user snippets from file '{_userSnippetsFilePath}' resulted in null. Initializing with an empty list.");
                        }
                    }
                    else
                    {
                        _userSnippets = new List<CodeSnippet>(); // File exists but is empty or whitespace.
                         Console.WriteLine($"Info: User snippets file '{_userSnippetsFilePath}' exists but is empty or whitespace. Initializing with an empty list.");
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"Error deserializing user snippets from file '{_userSnippetsFilePath}': {jsonEx.Message}. Initializing with an empty list.");
                    _userSnippets = new List<CodeSnippet>();
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"Error reading user snippets file '{_userSnippetsFilePath}': {ioEx.Message}. Initializing with an empty list.");
                    _userSnippets = new List<CodeSnippet>();
                }
                catch (Exception ex) // Catch any other unexpected errors
                {
                    Console.WriteLine($"Unexpected error loading user snippets from file '{_userSnippetsFilePath}': {ex.Message}. Initializing with an empty list.");
                    _userSnippets = new List<CodeSnippet>();
                }
            }
            else
            {
                _userSnippets = new List<CodeSnippet>(); // File doesn't exist, initialize to empty.
                // Log info: User snippets file not found, initializing with an empty list. This is normal on first run.
                Console.WriteLine($"Info: User snippets file '{_userSnippetsFilePath}' not found. Initializing with an empty list. This is normal on first run or if no user snippets have been saved yet.");
            }
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
            try
            {
                // Ensure the directory exists
                string? directoryPath = Path.GetDirectoryName(_userSnippetsFilePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Console.WriteLine($"Info: Created directory '{directoryPath}' for user snippets.");
                }

                // Serialize the current _userSnippets list
                string jsonContent = JsonSerializer.Serialize(_userSnippets, new JsonSerializerOptions
                {
                    WriteIndented = true
                    // DefaultJsonIgnoreCondition = JsonIgnoreCondition.Never (default) is appropriate
                });

                // Write the JSON to the file
                await File.WriteAllTextAsync(_userSnippetsFilePath, jsonContent);
                Console.WriteLine($"Info: User snippets successfully saved to '{_userSnippetsFilePath}'.");
            }
            catch (Exception ex) // Catch a general exception for file I/O, serialization, or directory creation
            {
                Console.WriteLine($"Error saving user snippets to file '{_userSnippetsFilePath}': {ex.Message}");
                // Depending on requirements, you might want to re-throw or handle more specifically
                // For example, if unauthorized, it might indicate a deployment/permissions issue.
            }
            finally
            {
                // Ensure MergeSnippets is called to keep the public Snippets list consistent
                // with the in-memory _userSnippets, regardless of save success/failure.
                MergeSnippets();
            }
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

        public async Task<bool> LoadUserSnippetsFromJsonAsync(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                // Treat empty or whitespace JSON as a request to clear user snippets.
                _userSnippets = new List<CodeSnippet>();
                await SaveUserSnippetsAsync(); // Persist the cleared list and merge.
                Console.WriteLine($"Info: Loaded empty/whitespace JSON. User snippets cleared and saved.");
                return true;
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
                    // If deserialization results in null (e.g., JSON was "null"), treat as empty.
                    _userSnippets = new List<CodeSnippet>();
                    Console.WriteLine($"Info: Deserialized JSON content resulted in null. User snippets initialized as empty.");
                }

                await SaveUserSnippetsAsync(); // Persist the loaded/updated snippets and merge.
                Console.WriteLine($"Info: User snippets successfully loaded from JSON and saved.");
                return true;
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
