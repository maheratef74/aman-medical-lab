using System;
using System.Collections.Generic;

namespace DrMohamedWeb.Core.Entities
{
    public class PatientVisit
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}
