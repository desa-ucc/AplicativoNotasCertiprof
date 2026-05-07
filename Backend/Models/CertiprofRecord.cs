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

        public decimal? Grade { get; set; } // Represented as string in CSV, parsed to decimal

        public string? Percentage { get; set; }
        public string? Status { get; set; }
        public string? Cedula { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to upload history
        public int UploadHistoryId { get; set; }

        [ForeignKey(nameof(UploadHistoryId))]
        public UploadHistory? UploadHistory { get; set; }
    }
}
