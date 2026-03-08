using System;
using System.Collections.Generic;

namespace DrMohamedWeb.Core.Entities
{
    public class Patient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<PatientVisit> Visits { get; set; } = new List<PatientVisit>();
    }
}
