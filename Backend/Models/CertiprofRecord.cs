using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class CertiprofRecord
    {
        [Key]
        public int Id { get; set; }

        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required]
        public string CertificationName { get; set; } = string.Empty;

        public string? Grade { get; set; } // Represented as string in CSV, might be parsed later

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to upload history
        public int UploadHistoryId { get; set; }

        [ForeignKey(nameof(UploadHistoryId))]
        public UploadHistory? UploadHistory { get; set; }
    }
}
