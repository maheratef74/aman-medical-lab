using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DrMohamedWeb.ViewModels
{
    public class AddVisitByPhoneViewModel
    {
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الزيارة مطلوب")]
        [Display(Name = "تاريخ الزيارة")]
        public DateTime VisitDate { get; set; } = DateTime.Today;

        [Display(Name = "ملاحظات الزيارة")]
        public string? Notes { get; set; }

        [Display(Name = "متاحة للمريض (تظهر في الموقع)")]
        public bool IsAvailable { get; set; } = false;

        [Required(ErrorMessage = "اسم التحليل مطلوب")]
        [Display(Name = "اسم التحليل")]
        public string TestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى اختيار ملف (PDF) واحد على الأقل")]
        [Display(Name = "ملفات التحاليل (PDF)")]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}
