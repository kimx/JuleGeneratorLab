using System;
using System.ComponentModel.DataAnnotations;

namespace JuleGeneratorLab.Models
{
    public class Project
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100, ErrorMessage = "Project name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Namespace cannot be longer than 100 characters.")]
        public string? Namespace { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        public string? Description { get; set; }

        public Guid? DatabaseConnectionId { get; set; } // Foreign key for DatabaseConnection
        // public virtual DatabaseConnection? DatabaseConnection { get; set; } // Navigation property

        public List<Guid> SelectedSnippetSetIds { get; set; } = new List<Guid>();
        // public virtual SnippetSet? SelectedSnippetSet { get; set; } // Navigation property

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Project()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            // SelectedSnippetSetIds is already initialized by property initializer
        }
    }
}
