using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class UploadHistory
    {
        [Key]
        public int Id { get; set; }

        public string UploadedBy { get; set; } = string.Empty; // User who uploaded (RBAC context)

        public string CourseCode { get; set; } = string.Empty; // e.g. "CE0501" for AVATAR

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public int ProcessedRecordsCount { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public ICollection<CertiprofRecord> Records { get; set; } = new List<CertiprofRecord>();
    }
}
