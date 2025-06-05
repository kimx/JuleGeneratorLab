using System;
using System.ComponentModel.DataAnnotations;

namespace JuleGeneratorLab.Models
{
    public enum DatabaseType
    {
        SqlServer,
        PostgreSql,
        MySql
        // Add other types as needed
    }

    public class DatabaseConnection
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Connection name is required.")]
        [StringLength(100, ErrorMessage = "Connection name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Connection string is required.")]
        public string ConnectionString { get; set; } = string.Empty;

        [Required(ErrorMessage = "Database type is required.")]
        public DatabaseType DatabaseType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DatabaseConnection()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
