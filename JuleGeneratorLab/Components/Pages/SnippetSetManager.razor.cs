using JuleGeneratorLab.Models;
using JuleGeneratorLab.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JuleGeneratorLab.Components.Pages
{
    public partial class SnippetSetManager
    {
        [Inject]
        private SnippetSetService SnippetSetSvc { get; set; } = default!;
        [Inject]
        private CodeSnippetService CodeSnippetSvc { get; set; } = default!;

        private List<SnippetSet>? snippetSets;
        private List<CodeSnippet>? allAvailableSnippets;
        private SnippetSet currentSnippetSet = new();
        private HashSet<string> selectedSnippetNamesInModal = new HashSet<string>(); // For managing checkbox state in modal

        private bool showModal = false;
        private string? errorMessage;
        private string modalTitle = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadSnippetSets();
            await LoadAllAvailableSnippets();
        }

        private async Task LoadSnippetSets()
        {
            try
            {
                snippetSets = await SnippetSetSvc.GetSnippetSetsAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading snippet sets: {ex.Message}";
                snippetSets = new List<SnippetSet>();
            }
        }

        private async Task LoadAllAvailableSnippets()
        {
            try
            {
                await CodeSnippetSvc.EnsureInitializedAsync(); // Make sure snippets are loaded
                allAvailableSnippets = CodeSnippetSvc.Snippets;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading code snippets: {ex.Message}";
                allAvailableSnippets = new List<CodeSnippet>();
            }
        }

        private void ShowAddModal()
        {
            currentSnippetSet = new SnippetSet { Id = Guid.Empty }; // Important: new Guid for new item
            selectedSnippetNamesInModal = new HashSet<string>();
            modalTitle = "Add New Snippet Set";
            errorMessage = null;
            showModal = true;
        }

        private async Task ShowEditModal(Guid setId)
        {
            var setToEdit = await SnippetSetSvc.GetSnippetSetByIdAsync(setId);
            if (setToEdit != null)
            {
                currentSnippetSet = new SnippetSet // Create a copy for editing
                {
                    Id = setToEdit.Id,
                    Name = setToEdit.Name,
                    Description = setToEdit.Description,
                    SnippetNames = new List<string>(setToEdit.SnippetNames ?? new List<string>()), // Ensure copy
                    CreatedAt = setToEdit.CreatedAt,
                    UpdatedAt = setToEdit.UpdatedAt
                };
                selectedSnippetNamesInModal = new HashSet<string>(currentSnippetSet.SnippetNames);
                modalTitle = "Edit Snippet Set";
                errorMessage = null;
                showModal = true;
            }
            else
            {
                errorMessage = "Could not find the snippet set to edit.";
            }
        }

        private void CloseModal()
        {
            showModal = false;
            currentSnippetSet = new(); // Reset
            selectedSnippetNamesInModal.Clear();
            errorMessage = null;
        }

        private async Task HandleSave()
        {
            errorMessage = null;
            currentSnippetSet.SnippetNames = selectedSnippetNamesInModal.ToList(); // Update from HashSet

            try
            {
                if (currentSnippetSet.Id == Guid.Empty)
                {
                    await SnippetSetSvc.AddSnippetSetAsync(currentSnippetSet);
                }
                else
                {
                    await SnippetSetSvc.UpdateSnippetSetAsync(currentSnippetSet);
                }
                CloseModal();
                await LoadSnippetSets(); // Refresh list
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving snippet set: {ex.Message}";
                // Keep modal open if error
            }
        }

        private SnippetSet? setPendingDelete;

        private async Task ShowDeleteConfirmModal(Guid setId)
        {
            setPendingDelete = await SnippetSetSvc.GetSnippetSetByIdAsync(setId);
            if (setPendingDelete == null)
            {
                 errorMessage = "Could not find the snippet set to delete.";
                 StateHasChanged(); // Update UI with error
            }
            // The actual modal display will be handled by a boolean flag in Razor,
            // similar to ProjectManager or DbConnectionManager. For this subtask,
            // we assume such a flag (e.g., `showDeleteConfirmModal`) would be set here.
            // For now, we'll just prepare `setPendingDelete`.
            // A dedicated delete confirmation modal is good practice.
            // Let's add it for completeness of logic.
            StateHasChanged(); // To ensure UI reflects that setPendingDelete is populated
        }

        private async Task ConfirmDelete()
        {
            if (setPendingDelete == null) return;
            try
            {
                await SnippetSetSvc.DeleteSnippetSetAsync(setPendingDelete.Id);
                setPendingDelete = null; // Clear it
                await LoadSnippetSets(); // Refresh
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting snippet set: {ex.Message}";
            }
            // Assume showDeleteConfirmModal = false; would be called here.
            StateHasChanged();
        }

        private void ToggleSnippetSelectionInModal(string snippetName)
        {
            if (selectedSnippetNamesInModal.Contains(snippetName))
            {
                selectedSnippetNamesInModal.Remove(snippetName);
            }
            else
            {
                selectedSnippetNamesInModal.Add(snippetName);
            }
        }
    }
}
