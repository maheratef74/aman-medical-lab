using DrMohamedWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DrMohamedWeb.Infrastructure.Services
{
    /// <summary>
    /// Monthly cleanup job: on the 1st of every month, permanently deletes result PDFs
    /// (physical files + TestResult records) uploaded on or before the 30th of the
    /// previous month. Patient visits are never touched.
    /// </summary>
    public class ResultFileCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ResultFileCleanupService> _logger;

        private int? _lastExecutedMonth = null;

        public ResultFileCleanupService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            ILogger<ResultFileCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Result file cleanup service started. Cutoff rule: every 1st of month, delete files uploaded on/before the 30th of the previous month.");

            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TryRunCleanupAsync(stoppingToken);
            }
        }

        private async Task TryRunCleanupAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            // Only run on the 1st of the month, once per month
            if (now.Day != 1)
            {
                return;
            }

            var monthKey = now.Year * 100 + now.Month;
            if (_lastExecutedMonth == monthKey)
            {
                return;
            }

            _lastExecutedMonth = monthKey;
            await CleanupAsync(now, cancellationToken);
        }

        private async Task CleanupAsync(DateTime now, CancellationToken cancellationToken)
        {
            // On 1/9 this produces 30/7 — files uploaded on or before it get removed
            var cutoff = new DateTime(now.Year, now.Month, 1).AddMonths(-1).AddDays(-1);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AmanDbContext>();
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var staleResults = await context.TestResults
                    .Where(t => t.UploadedAt <= cutoff)
                    .ToListAsync(cancellationToken);

                if (staleResults.Count == 0)
                {
                    _logger.LogInformation("Cleanup on {Date}: no result files older than cutoff {Cutoff:yyyy-MM-dd}.", now.ToString("yyyy-MM-dd"), cutoff);
                    return;
                }

                int filesDeleted = 0;

                foreach (var result in staleResults)
                {
                    if (!string.IsNullOrWhiteSpace(result.FilePath))
                    {
                        try
                        {
                            var fullPath = ResolveFilePath(webRoot, result.FilePath);
                            if (fullPath != null && File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                                filesDeleted++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete physical file for TestResult #{Id} ({Path}); DB row will still be removed.", result.Id, result.FilePath);
                        }
                    }
                }

                context.TestResults.RemoveRange(staleResults);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cleanup completed on {Date}: removed {Rows} result record(s) (cutoff {Cutoff:yyyy-MM-dd}), deleted {Files} physical file(s).",
                    now.ToString("yyyy-MM-dd"), staleResults.Count, cutoff, filesDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Result file cleanup failed on {Date}.", now.ToString("yyyy-MM-dd"));
            }
        }

        /// <summary>
        /// Resolves a stored relative URL (e.g. /results/2026/07/xxx.pdf) into a safe
        /// absolute path inside the web root. Returns null for out-of-bounds paths.
        /// </summary>
        private static string? ResolveFilePath(string webRoot, string relativePath)
        {
            var relative = relativePath.TrimStart('/', '\\');
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative));
            var rootFull = Path.GetFullPath(webRoot);

            if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }
    }
}