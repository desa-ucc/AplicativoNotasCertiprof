using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class CertiprofRecord
    {
        [Key]
        [NotMapped]
        public int Id { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Required]
        [Column("certification_name")]
        public string CertificationName { get; set; } = string.Empty;

        [NotMapped]
        public decimal? Grade { get; set; } // Represented as string in CSV, parsed to decimal

        [Column("percentage")]
        public string? Percentage { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("cedula")]
        public string? Cedula { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to upload history
        [NotMapped]
        public int UploadHistoryId { get; set; }

        [NotMapped]
        public UploadHistory? UploadHistory { get; set; }
    }
}
