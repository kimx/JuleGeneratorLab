using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading; // For SemaphoreSlim
using System.Threading.Tasks;
using JuleGeneratorLab.Models;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
// using System.Data.SqlClient; // For TestConnectionAsync - To be added if test functionality is implemented

namespace JuleGeneratorLab.Services
{
    public class DatabaseConnectionService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _connectionsFilePath;
        private List<DatabaseConnection> _connections = new List<DatabaseConnection>();
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public DatabaseConnectionService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            string dataDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Data");
            _connectionsFilePath = Path.Combine(dataDirectory, "database_connections.json");
        }

        private async Task EnsureDataDirectoryExistsAsync()
        {
            string? directoryPath = Path.GetDirectoryName(_connectionsFilePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"Info: Created data directory at '{directoryPath}' for database connections.");
            }
        }

        private async Task LoadConnectionsAsync()
        {
            if (!File.Exists(_connectionsFilePath))
            {
                _connections = new List<DatabaseConnection>();
                Console.WriteLine($"Info: Database connections file '{_connectionsFilePath}' not found. Initializing with an empty list.");
                return;
            }

            await _fileLock.WaitAsync();
            try
            {
                string jsonContent = await File.ReadAllTextAsync(_connectionsFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                     _connections = new List<DatabaseConnection>();
                     Console.WriteLine($"Info: Database connections file '{_connectionsFilePath}' is empty. Initializing with an empty list.");
                }
                else
                {
                    var connections = JsonSerializer.Deserialize<List<DatabaseConnection>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    _connections = connections ?? new List<DatabaseConnection>();
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error deserializing database connections from '{_connectionsFilePath}': {jsonEx.Message}. Initializing with an empty list.");
                _connections = new List<DatabaseConnection>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading database connections from '{_connectionsFilePath}': {ex.Message}. Initializing with an empty list.");
                _connections = new List<DatabaseConnection>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveConnectionsAsync()
        {
            await EnsureDataDirectoryExistsAsync();
            await _fileLock.WaitAsync();
            try
            {
                string jsonContent = JsonSerializer.Serialize(_connections, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_connectionsFilePath, jsonContent);
                Console.WriteLine($"Info: Database connections successfully saved to '{_connectionsFilePath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving database connections to '{_connectionsFilePath}': {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<DatabaseConnection>> GetConnectionsAsync()
        {
            await LoadConnectionsAsync();
            return _connections.OrderBy(c => c.Name).ToList();
        }

        public async Task<DatabaseConnection?> GetConnectionByIdAsync(Guid id)
        {
            await LoadConnectionsAsync();
            return _connections.FirstOrDefault(c => c.Id == id);
        }

        public async Task AddConnectionAsync(DatabaseConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            await LoadConnectionsAsync();

            if (_connections.Any(c => c.Name == connection.Name))
            {
                throw new InvalidOperationException($"A database connection with the name '{connection.Name}' already exists.");
            }

            connection.Id = Guid.NewGuid(); // Ensure new Id
            connection.CreatedAt = DateTime.UtcNow;
            connection.UpdatedAt = DateTime.UtcNow;
            _connections.Add(connection);
            await SaveConnectionsAsync();
        }

        public async Task UpdateConnectionAsync(DatabaseConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            await LoadConnectionsAsync();

            var existingConnection = _connections.FirstOrDefault(c => c.Id == connection.Id);
            if (existingConnection == null)
            {
                throw new KeyNotFoundException($"Database connection with Id '{connection.Id}' not found.");
            }

            if (existingConnection.Name != connection.Name && _connections.Any(c => c.Id != connection.Id && c.Name == connection.Name))
            {
                throw new InvalidOperationException($"Another database connection with the name '{connection.Name}' already exists.");
            }

            existingConnection.Name = connection.Name;
            existingConnection.ConnectionString = connection.ConnectionString;
            existingConnection.DatabaseType = connection.DatabaseType;
            existingConnection.UpdatedAt = DateTime.UtcNow;

            await SaveConnectionsAsync();
        }

        public async Task DeleteConnectionAsync(Guid id)
        {
            await LoadConnectionsAsync();

            var connectionToRemove = _connections.FirstOrDefault(c => c.Id == id);
            if (connectionToRemove != null)
            {
                _connections.Remove(connectionToRemove);
                await SaveConnectionsAsync();
            }
            else
            {
                throw new KeyNotFoundException($"Database connection with Id '{id}' not found for deletion.");
            }
        }

        // Optional: TestConnectionAsync - Can be implemented later
        // public async Task<bool> TestConnectionAsync(DatabaseConnection connection)
        // {
        //     if (connection == null) return false;
        //     try
        //     {
        //         // This is a simplified example for SQL Server.
        //         // Other database types would need different connection objects and logic.
        //         if (connection.DatabaseType == DatabaseType.SqlServer)
        //         {
        //             using (var sqlConnection = new SqlConnection(connection.ConnectionString))
        //             {
        //                 await sqlConnection.OpenAsync();
        //                 return true;
        //             }
        //         }
        //         // Add cases for other DB types if necessary
        //         return false; // Or throw NotSupportedException for other types
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Connection test failed for '{connection.Name}': {ex.Message}");
        //         return false;
        //     }
        // }
    }
}
