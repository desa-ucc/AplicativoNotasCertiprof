using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("cert_registros")]
    public class CertiprofRecord
    {
        [Key]
        [Column("id")]
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

        [Column("status")]
        public string? Status { get; set; }

        [Column("percentage")]
        public decimal? Percentage { get; set; }

        [Column("cedula")]
        public string? Cedula { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to upload history
        [Column("upload_history_id")]
        public int UploadHistoryId { get; set; }

        [ForeignKey(nameof(UploadHistoryId))]
        public UploadHistory? UploadHistory { get; set; }
    }
}
