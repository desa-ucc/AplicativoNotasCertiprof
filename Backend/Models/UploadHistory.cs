using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("cert_historial_cargas")]
    public class UploadHistory
    {
        [Key]
        public int Id { get; set; }

        public string UploadedBy { get; set; } = string.Empty; // User who uploaded (RBAC context)

        public string CourseCode { get; set; } = string.Empty; // e.g. "CE0501" for AVATAR

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public int ProcessedRecordsCount { get; set; }

        public ICollection<CertiprofRecord> Records { get; set; } = new List<CertiprofRecord>();
    }
}
