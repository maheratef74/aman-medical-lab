using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DrMohamedWeb.ViewModels
{
    public class UploadResultViewModel
    {
        public int VisitId { get; set; }

        [Required]
        public string TestName { get; set; } = string.Empty;

        [Required]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}
