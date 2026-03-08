using System;

namespace DrMohamedWeb.Core.Entities
{
    public class TestResult
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public PatientVisit? Visit { get; set; }
    }
}
