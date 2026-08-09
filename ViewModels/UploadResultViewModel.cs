using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DrMohamedWeb.ViewModels
{
    public class UploadResultViewModel
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }

        [Required(ErrorMessage = "اسم التحليل مطلوب")]
        public string TestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى اختيار ملف (PDF) واحد على الأقل")]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}
