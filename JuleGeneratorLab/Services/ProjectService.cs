using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JuleGeneratorLab.Models;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace JuleGeneratorLab.Services
{
    public class ProjectService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _projectsFilePath;
        private List<Project> _projects = new List<Project>();
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public ProjectService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            // Define the path for projects.json in the Data directory
            string dataDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Data");
            _projectsFilePath = Path.Combine(dataDirectory, "projects.json");
            // Ensure data directory exists during service instantiation or before first write.
            // For now, LoadProjectsAsync will handle directory check during load,
            // and SaveProjectsAsync will handle it during save.
        }

        private async Task EnsureDataDirectoryExistsAsync()
        {
            string? directoryPath = Path.GetDirectoryName(_projectsFilePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"Info: Created data directory at '{directoryPath}' for projects.");
            }
        }

        private async Task LoadProjectsAsync()
        {
            if (!File.Exists(_projectsFilePath))
            {
                _projects = new List<Project>();
                Console.WriteLine($"Info: Projects file '{_projectsFilePath}' not found. Initializing with an empty list.");
                return;
            }

            await _fileLock.WaitAsync();
            try
            {
                string jsonContent = await File.ReadAllTextAsync(_projectsFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _projects = new List<Project>();
                    Console.WriteLine($"Info: Projects file '{_projectsFilePath}' is empty. Initializing with an empty list.");
                }
                else
                {
                    var projects = JsonSerializer.Deserialize<List<Project>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    _projects = projects ?? new List<Project>();
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error deserializing projects from '{_projectsFilePath}': {jsonEx.Message}. Initializing with an empty list.");
                _projects = new List<Project>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading projects from '{_projectsFilePath}': {ex.Message}. Initializing with an empty list.");
                _projects = new List<Project>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveProjectsAsync()
        {
            await EnsureDataDirectoryExistsAsync(); // Ensure directory exists before writing
            await _fileLock.WaitAsync();
            try
            {
                string jsonContent = JsonSerializer.Serialize(_projects, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_projectsFilePath, jsonContent);
                Console.WriteLine($"Info: Projects successfully saved to '{_projectsFilePath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving projects to '{_projectsFilePath}': {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            await LoadProjectsAsync(); // Ensure latest data is loaded
            return _projects.OrderBy(p => p.Name).ToList();
        }

        public async Task<Project?> GetProjectByIdAsync(Guid id)
        {
            await LoadProjectsAsync();
            return _projects.FirstOrDefault(p => p.Id == id);
        }

        public async Task AddProjectAsync(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            await LoadProjectsAsync(); // Load current projects first

            if (_projects.Any(p => p.Name == project.Name))
            {
                // Or handle as an update, or throw specific exception
                Console.WriteLine($"Warning: Project with name '{project.Name}' already exists. Overwriting not implemented, new project not added.");
                throw new InvalidOperationException($"A project with the name '{project.Name}' already exists.");
            }

            project.Id = Guid.NewGuid(); // Ensure new Id
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;
            _projects.Add(project);
            await SaveProjectsAsync();
        }

        public async Task UpdateProjectAsync(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            await LoadProjectsAsync(); // Load current projects

            var existingProject = _projects.FirstOrDefault(p => p.Id == project.Id);
            if (existingProject == null)
            {
                throw new KeyNotFoundException($"Project with Id '{project.Id}' not found.");
            }

            // Check for name collision if the name is being changed
            if (existingProject.Name != project.Name && _projects.Any(p => p.Id != project.Id && p.Name == project.Name))
            {
                 throw new InvalidOperationException($"Another project with the name '{project.Name}' already exists.");
            }

            existingProject.Name = project.Name;
            existingProject.Namespace = project.Namespace;
            existingProject.Description = project.Description;
            existingProject.DatabaseConnectionId = project.DatabaseConnectionId;
            existingProject.SelectedSnippetSetId = project.SelectedSnippetSetId;
            existingProject.UpdatedAt = DateTime.UtcNow;

            await SaveProjectsAsync();
        }

        public async Task DeleteProjectAsync(Guid id)
        {
            await LoadProjectsAsync(); // Load current projects

            var projectToRemove = _projects.FirstOrDefault(p => p.Id == id);
            if (projectToRemove != null)
            {
                _projects.Remove(projectToRemove);
                await SaveProjectsAsync();
            }
            else
            {
                 throw new KeyNotFoundException($"Project with Id '{id}' not found for deletion.");
            }
        }
    }
}
