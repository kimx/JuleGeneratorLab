using JuleGeneratorLab.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JuleGeneratorLab.Components.Pages
{
    public partial class DbConnectionManager
    {
        private List<DatabaseConnection>? connections;
        private DatabaseConnection currentConnection = new(); // For Add/Edit form
        private DatabaseConnection? connectionToDelete; // For delete confirmation

        private bool showConnectionModal = false;
        private bool showDeleteConfirmModal = false;
        private string? errorMessage;
        private string? testMessage; // For test connection results
        private bool testSuccess = false;


        protected override async Task OnInitializedAsync()
        {
            await LoadConnections();
        }

        private async Task LoadConnections()
        {
            try
            {
                connections = await DbConnectionSvc.GetConnectionsAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading connections: {ex.Message}";
                connections = new List<DatabaseConnection>();
            }
            StateHasChanged();
        }

        private void ShowAddConnectionModal()
        {
            currentConnection = new DatabaseConnection { Id = Guid.Empty, DatabaseType = DatabaseType.SqlServer }; // Default to SqlServer
            errorMessage = null;
            testMessage = null;
            showConnectionModal = true;
        }

        private async Task ShowEditConnectionModal(Guid connectionId)
        {
            var conn = await DbConnectionSvc.GetConnectionByIdAsync(connectionId);
            if (conn != null)
            {
                currentConnection = new DatabaseConnection // Create a copy for editing
                {
                    Id = conn.Id,
                    Name = conn.Name,
                    ConnectionString = conn.ConnectionString,
                    DatabaseType = conn.DatabaseType,
                    CreatedAt = conn.CreatedAt,
                    UpdatedAt = conn.UpdatedAt
                };
                errorMessage = null;
                testMessage = null;
                showConnectionModal = true;
            }
            else
            {
                errorMessage = "Could not find the connection to edit.";
            }
        }

        private void CloseConnectionModal()
        {
            showConnectionModal = false;
            currentConnection = new(); // Reset
            errorMessage = null;
            testMessage = null;
        }

        private async Task HandleSaveConnection()
        {
            errorMessage = null;
            testMessage = null;
            try
            {
                if (currentConnection.Id == Guid.Empty) // New connection
                {
                    await DbConnectionSvc.AddConnectionAsync(currentConnection);
                }
                else // Existing connection
                {
                    await DbConnectionSvc.UpdateConnectionAsync(currentConnection);
                }
                CloseConnectionModal();
                await LoadConnections(); // Refresh the list
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving connection: {ex.Message}";
            }
        }

        private async Task ShowDeleteConfirmModal(Guid connectionId)
        {
            connectionToDelete = await DbConnectionSvc.GetConnectionByIdAsync(connectionId);
             if (connectionToDelete != null)
            {
                errorMessage = null;
                showDeleteConfirmModal = true;
            }
            else
            {
                errorMessage = "Could not find the connection to delete.";
            }
        }

        private void CloseDeleteConfirmModal()
        {
            showDeleteConfirmModal = false;
            connectionToDelete = null;
            errorMessage = null;
        }

        private async Task ConfirmDeleteConnection()
        {
            errorMessage = null;
            if (connectionToDelete != null)
            {
                try
                {
                    // Check if any project is using this connection
                    // This is a simplified check. In a real app, you might want to prevent deletion
                    // or offer to reassign projects.
                    // For now, we are not implementing this check to keep it simple.
                    // var projects = await ProjectSvc.GetProjectsAsync(); // Assuming ProjectSvc is injected
                    // if (projects.Any(p => p.DatabaseConnectionId == connectionToDelete.Id))
                    // {
                    //     errorMessage = $"Cannot delete connection '{connectionToDelete.Name}' as it is being used by one or more projects.";
                    //     return;
                    // }

                    await DbConnectionSvc.DeleteConnectionAsync(connectionToDelete.Id);
                    CloseDeleteConfirmModal();
                    await LoadConnections(); // Refresh list
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error deleting connection: {ex.Message}";
                }
            }
        }

        // Placeholder for Test Connection functionality
        // private async Task TestConnection(Guid connectionId)
        // {
        //     errorMessage = null;
        //     testMessage = null;
        //     var connToTest = await DbConnectionSvc.GetConnectionByIdAsync(connectionId);
        //     if (connToTest != null)
        //     {
        //         try
        //         {
        //             // bool success = await DbConnectionSvc.TestConnectionAsync(connToTest); // Uncomment when service method is active
        //             bool success = false; // Replace with actual call
        //             if (success)
        //             {
        //                 testMessage = $"Connection to '{connToTest.Name}' successful!";
        //                 testSuccess = true;
        //             }
        //             else
        //             {
        //                 testMessage = $"Failed to connect to '{connToTest.Name}'. Check configuration and network.";
        //                 testSuccess = false;
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             testMessage = $"Error testing connection '{connToTest.Name}': {ex.Message}";
        //             testSuccess = false;
        //         }
        //     }
        //     else
        //     {
        //         testMessage = "Could not find the connection to test.";
        //         testSuccess = false;
        //     }
        //     // This is a simple feedback. For modal, you might want to show this inside the edit modal or a separate one.
        //     // For now, it will appear at the top of the page if not in a modal.
        //     StateHasChanged(); // To show the testMessage
        // }
    }
}
