using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DrMohamedWeb.Core.Entities
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المريض مطلوب")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<PatientVisit> Visits { get; set; } = new List<PatientVisit>();
    }
}
