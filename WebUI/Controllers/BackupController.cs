using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BackupController : Controller
    {
        private readonly IBackupService _backupService;

        public BackupController(IBackupService backupService)
        {
            _backupService = backupService;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Backup & Restore";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBackupHistory()
        {
            try
            {
                var backups = await _backupService.GetBackupHistoryAsync();
                return Json(new
                {
                    success = true,
                    data = backups.Select(b => new
                    {
                        fileName = b.FileName,
                        fileSize = b.FileSizeFormatted,
                        createdDate = b.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var backupFileName = await _backupService.CreateBackupAsync();
                var backupDirectory = _backupService.GetBackupDirectory();
                var fullPath = Path.Combine(backupDirectory, backupFileName);
                
                return Json(new
                {
                    success = true,
                    message = "Database backup created successfully",
                    fileName = backupFileName,
                    filePath = fullPath,
                    directory = backupDirectory
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Backup failed: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestoreBackup(IFormFile backupFile)
        {
            try
            {
                if (backupFile == null || backupFile.Length == 0)
                    return Json(new { success = false, message = "No file uploaded" });

                if (!backupFile.FileName.EndsWith(".zip"))
                    return Json(new { success = false, message = "Only ZIP files are allowed" });

                // Save uploaded file temporarily
                var tempPath = Path.Combine(Path.GetTempPath(), backupFile.FileName);
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await backupFile.CopyToAsync(stream);
                }

                // Restore the backup
                var result = await _backupService.RestoreBackupAsync(tempPath);

                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);

                if (result)
                    return Json(new { success = true, message = "Database restored successfully. Please restart the application." });
                else
                    return Json(new { success = false, message = "Restore failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Restore failed: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadBackup(string fileName)
        {
            try
            {
                var backups = await _backupService.GetBackupHistoryAsync();
                var backup = backups.FirstOrDefault(b => b.FileName == fileName);

                if (backup == null || !System.IO.File.Exists(backup.FilePath))
                    return NotFound();

                var fileBytes = await System.IO.File.ReadAllBytesAsync(backup.FilePath);
                return File(fileBytes, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBackup(string fileName)
        {
            try
            {
                var result = await _backupService.DeleteBackupAsync(fileName);

                if (result)
                    return Json(new { success = true, message = "Backup deleted successfully" });
                else
                    return Json(new { success = false, message = "Backup file not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Delete failed: {ex.Message}" });
            }
        }
    }
}
