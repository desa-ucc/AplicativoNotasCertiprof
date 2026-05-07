using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class CertiprofRecord
    {
        [Key]
        [Column("cert_id")]
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

        // Foreign key to upload history
        [Column("cert_upload_history_id")]
        public int UploadHistoryId { get; set; }

        [ForeignKey(nameof(UploadHistoryId))]
        public UploadHistory? UploadHistory { get; set; }
    }
}
