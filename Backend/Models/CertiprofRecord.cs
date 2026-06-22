using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class CertiprofRecord
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cert_email")]
        public string? Email { get; set; }

        [Column("cert_first_name")]
        public string? FirstName { get; set; }

        [Column("cert_last_name")]
        public string? LastName { get; set; }

        [Required]
        [Column("cert_certification_name")]
        public string CertificationName { get; set; } = string.Empty;

        [NotMapped]
        public decimal? Grade { get; set; } // Represented as string in CSV, parsed to decimal

        [Column("cert_percentage")]
        public string? Percentage { get; set; }

        [Column("cert_status")]
        public string? Status { get; set; }

        [Column("cert_cedula")]
        public string? Cedula { get; set; }

        [Column("cert_created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Foreign key to upload history
        [NotMapped]
        public int UploadHistoryId { get; set; }

        [NotMapped]
        public UploadHistory? UploadHistory { get; set; }
    }
}
