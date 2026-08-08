using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DrMohamedWeb.ViewModels
{
    public class AddVisitViewModel
    {
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "اسم المريض")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "تاريخ الزيارة مطلوب")]
        [Display(Name = "تاريخ الزيارة")]
        public DateTime VisitDate { get; set; } = DateTime.Today;

        [Display(Name = "ملاحظات الزيارة")]
        public string? Notes { get; set; }

        [Display(Name = "متاحة للمريض (تظهر في الموقع)")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "اسم التحليل")]
        public string? TestName { get; set; }

        [Display(Name = "ملفات النتائج (PDF)")]
        public List<IFormFile>? Files { get; set; }
    }
}
