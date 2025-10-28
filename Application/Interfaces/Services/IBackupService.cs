using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IBackupService
    {
        Task<string> CreateBackupAsync();
        Task<bool> RestoreBackupAsync(string backupFilePath);
        Task<IEnumerable<BackupInfo>> GetBackupHistoryAsync();
        Task<bool> DeleteBackupAsync(string backupFileName);
        string GetBackupDirectory();
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
