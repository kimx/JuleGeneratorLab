using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JuleGeneratorLab.Models
{
    public class SnippetSet
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Snippet set name is required.")]
        [StringLength(100, ErrorMessage = "Snippet set name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the list of CodeSnippet names that belong to this set.
        /// </summary>
        public List<string> SnippetNames { get; set; } = new List<string>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public SnippetSet()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            SnippetNames = new List<string>();
        }
    }
}
