using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading; // For SemaphoreSlim
using System.Threading.Tasks;
using JuleGeneratorLab.Models;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace JuleGeneratorLab.Services
{
    public class SnippetSetService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _snippetSetsFilePath;
        private List<SnippetSet> _snippetSets = new List<SnippetSet>();
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public SnippetSetService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            string dataDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Data");
            _snippetSetsFilePath = Path.Combine(dataDirectory, "snippet_sets.json");
        }

        private async Task EnsureDataDirectoryExistsAsync()
        {
            string? directoryPath = Path.GetDirectoryName(_snippetSetsFilePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"Info: Created data directory at '{directoryPath}' for snippet sets.");
            }
        }

        private async Task LoadSnippetSetsAsync()
        {
            if (!File.Exists(_snippetSetsFilePath))
            {
                _snippetSets = new List<SnippetSet>();
                Console.WriteLine($"Info: Snippet sets file '{_snippetSetsFilePath}' not found. Initializing with an empty list.");
                return;
            }

            await _fileLock.WaitAsync();
            try
            {
                string jsonContent = await File.ReadAllTextAsync(_snippetSetsFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                     _snippetSets = new List<SnippetSet>();
                     Console.WriteLine($"Info: Snippet sets file '{_snippetSetsFilePath}' is empty. Initializing with an empty list.");
                }
                else
                {
                    var sets = JsonSerializer.Deserialize<List<SnippetSet>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    _snippetSets = sets ?? new List<SnippetSet>();
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error deserializing snippet sets from '{_snippetSetsFilePath}': {jsonEx.Message}. Initializing with an empty list.");
                _snippetSets = new List<SnippetSet>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading snippet sets from '{_snippetSetsFilePath}': {ex.Message}. Initializing with an empty list.");
                _snippetSets = new List<SnippetSet>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveSnippetSetsAsync()
        {
            await EnsureDataDirectoryExistsAsync();
            await _fileLock.WaitAsync();
            try
            {
                string jsonContent = JsonSerializer.Serialize(_snippetSets, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_snippetSetsFilePath, jsonContent);
                Console.WriteLine($"Info: Snippet sets successfully saved to '{_snippetSetsFilePath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving snippet sets to '{_snippetSetsFilePath}': {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<SnippetSet>> GetSnippetSetsAsync()
        {
            await LoadSnippetSetsAsync();
            return _snippetSets.OrderBy(s => s.Name).ToList();
        }

        public async Task<SnippetSet?> GetSnippetSetByIdAsync(Guid id)
        {
            await LoadSnippetSetsAsync();
            return _snippetSets.FirstOrDefault(s => s.Id == id);
        }

        public async Task AddSnippetSetAsync(SnippetSet snippetSet)
        {
            if (snippetSet == null) throw new ArgumentNullException(nameof(snippetSet));

            await LoadSnippetSetsAsync();

            if (_snippetSets.Any(s => s.Name == snippetSet.Name))
            {
                throw new InvalidOperationException($"A snippet set with the name '{snippetSet.Name}' already exists.");
            }

            snippetSet.Id = Guid.NewGuid(); // Ensure new Id
            snippetSet.CreatedAt = DateTime.UtcNow;
            snippetSet.UpdatedAt = DateTime.UtcNow;
            _snippetSets.Add(snippetSet);
            await SaveSnippetSetsAsync();
        }

        public async Task UpdateSnippetSetAsync(SnippetSet snippetSet)
        {
            if (snippetSet == null) throw new ArgumentNullException(nameof(snippetSet));

            await LoadSnippetSetsAsync();

            var existingSet = _snippetSets.FirstOrDefault(s => s.Id == snippetSet.Id);
            if (existingSet == null)
            {
                throw new KeyNotFoundException($"Snippet set with Id '{snippetSet.Id}' not found.");
            }

            if (existingSet.Name != snippetSet.Name && _snippetSets.Any(s => s.Id != snippetSet.Id && s.Name == snippetSet.Name))
            {
                throw new InvalidOperationException($"Another snippet set with the name '{snippetSet.Name}' already exists.");
            }

            existingSet.Name = snippetSet.Name;
            existingSet.Description = snippetSet.Description;
            existingSet.SnippetNames = snippetSet.SnippetNames ?? new List<string>(); // Ensure list is not null
            existingSet.UpdatedAt = DateTime.UtcNow;

            await SaveSnippetSetsAsync();
        }

        public async Task DeleteSnippetSetAsync(Guid id)
        {
            await LoadSnippetSetsAsync();

            var setToRemove = _snippetSets.FirstOrDefault(s => s.Id == id);
            if (setToRemove != null)
            {
                _snippetSets.Remove(setToRemove);
                await SaveSnippetSetsAsync();
            }
            else
            {
                throw new KeyNotFoundException($"Snippet set with Id '{id}' not found for deletion.");
            }
        }
    }
}
