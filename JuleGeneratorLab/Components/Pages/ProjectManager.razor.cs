using JuleGeneratorLab.Models;
using JuleGeneratorLab.Services; // Added for SnippetSetService
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop; // For potential JS calls, though not strictly needed for this basic version
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JuleGeneratorLab.Components.Pages
{
    public partial class ProjectManager
    {
        [Inject]
        private SnippetSetService SnippetSetSvc { get; set; } = default!; // Injected SnippetSetService

        private List<Project>? projects;
        private List<DatabaseConnection>? availableConnections;
        private List<SnippetSet>? availableSnippetSets; // Added to hold available snippet sets
        private Project currentProject = new(); // For Add/Edit form
        private Project? projectToDelete; // For delete confirmation
        // private string currentProjectSnippetSetIdString = string.Empty; // Removed

        private bool showProjectModal = false;
        private bool showDeleteConfirmModal = false;
        private string? errorMessage;

        protected override async Task OnInitializedAsync()
        {
            await LoadProjects();
            await LoadAvailableConnections();
            await LoadAvailableSnippetSets(); // Added call
        }

        private async Task LoadAvailableSnippetSets() // Added new method
        {
            try
            {
                availableSnippetSets = await SnippetSetSvc.GetSnippetSetsAsync();
            }
            catch (Exception ex)
            {
                // Log or set an error message if necessary
                Console.WriteLine($"Error loading snippet sets: {ex.Message}");
                availableSnippetSets = new List<SnippetSet>(); // Initialize to empty list on error
            }
        }

        private async Task LoadProjects()
        {
            try
            {
                projects = await ProjectSvc.GetProjectsAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading projects: {ex.Message}";
                projects = new List<Project>(); // Ensure projects is not null
            }
            StateHasChanged(); // Ensure UI updates if projects load after initial render
        }

        private async Task LoadAvailableConnections()
        {
            try
            {
                availableConnections = await DbConnectionSvc.GetConnectionsAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading database connections: {ex.Message}";
                availableConnections = new List<DatabaseConnection>();
            }
        }

        private string GetConnectionName(Guid? connectionId)
        {
            if (!connectionId.HasValue || availableConnections == null)
                return "N/A";

            var conn = availableConnections.FirstOrDefault(c => c.Id == connectionId.Value);
            return conn?.Name ?? "Unknown";
        }

        private void ShowAddProjectModal()
        {
            currentProject = new Project { Id = Guid.Empty, SelectedSnippetSetId = null }; // Ensure a new project object for add, explicitly nullify SelectedSnippetSetId
            // currentProjectSnippetSetIdString = string.Empty; // Removed
            errorMessage = null;
            showProjectModal = true;
        }

        private async Task ShowEditProjectModal(Guid projectId)
        {
            var project = await ProjectSvc.GetProjectByIdAsync(projectId);
            if (project != null)
            {
                // Create a copy for editing to avoid modifying the list directly until save
                currentProject = new Project
                {
                    Id = project.Id,
                    Name = project.Name,
                    Namespace = project.Namespace,
                    Description = project.Description,
                    DatabaseConnectionId = project.DatabaseConnectionId,
                    SelectedSnippetSetId = project.SelectedSnippetSetId, // This is correct
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt
                };
                // currentProjectSnippetSetIdString = project.SelectedSnippetSetId?.ToString() ?? string.Empty; // Removed
                errorMessage = null;
                showProjectModal = true;
            }
            else
            {
                errorMessage = "Could not find the project to edit.";
            }
        }

        private void CloseProjectModal()
        {
            showProjectModal = false;
            currentProject = new(); // Reset
            errorMessage = null;
        }

        private async Task HandleSaveProject()
        {
            errorMessage = null;

            // Removed Guid parsing logic for currentProjectSnippetSetIdString
            // currentProject.SelectedSnippetSetId is now directly bound by InputSelect<Guid?>

            try
            {
                if (currentProject.Id == Guid.Empty) // New project
                {
                    await ProjectSvc.AddProjectAsync(currentProject);
                }
                else // Existing project
                {
                    await ProjectSvc.UpdateProjectAsync(currentProject);
                }
                CloseProjectModal();
                await LoadProjects(); // Refresh the list
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving project: {ex.Message}";
                // Keep modal open if error to show message
            }
        }

        private async Task ShowDeleteConfirmModal(Guid projectId)
        {
            projectToDelete = await ProjectSvc.GetProjectByIdAsync(projectId);
            if (projectToDelete != null)
            {
                errorMessage = null;
                showDeleteConfirmModal = true;
            }
            else
            {
                errorMessage = "Could not find the project to delete.";
            }
        }

        private void CloseDeleteConfirmModal()
        {
            showDeleteConfirmModal = false;
            projectToDelete = null;
            errorMessage = null;
        }

        private async Task ConfirmDeleteProject()
        {
            errorMessage = null;
            if (projectToDelete != null)
            {
                try
                {
                    await ProjectSvc.DeleteProjectAsync(projectToDelete.Id);
                    CloseDeleteConfirmModal();
                    await LoadProjects(); // Refresh list
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error deleting project: {ex.Message}";
                    // Keep modal open
                }
            }
        }
    }
}
