using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DrMohamedWeb.Application.Interfaces
{
    public interface IFileUploadService
    {
        Task<string> UploadPdfAsync(IFormFile file);
    }
}
