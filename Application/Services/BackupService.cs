using Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class BackupService : IBackupService
    {
        private readonly IConfiguration _configuration;
        private readonly string _backupDirectory;

        public BackupService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Use C:\KidsStoreBackups - accessible by SQL Server service
            _backupDirectory = @"C:\KidsStoreBackups";

            // Ensure backup directory exists
            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);
        }
        
        public string GetBackupDirectory() => _backupDirectory;

        public async Task<string> CreateBackupAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = builder.InitialCatalog;

                // Create a timestamp for the backup file
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_Backup_{timestamp}.bak";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

                // SQL backup command
                var backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = N'{backupFilePath}' 
                    WITH FORMAT, INIT, 
                    NAME = N'{databaseName}-Full Database Backup', 
                    SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(backupQuery, connection);
                command.CommandTimeout = 300; // 5 minutes timeout
                await command.ExecuteNonQueryAsync();

                // Compress the backup to ZIP
                var zipFileName = $"{databaseName}_Backup_{timestamp}.zip";
                var zipFilePath = Path.Combine(_backupDirectory, zipFileName);

                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    zipArchive.CreateEntryFromFile(backupFilePath, backupFileName);
                }

                // Delete the uncompressed .bak file
                if (File.Exists(backupFilePath))
                    File.Delete(backupFilePath);

                return zipFileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Backup failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupFilePath)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = builder.InitialCatalog;

                // Extract .bak file from ZIP
                string? extractedBakFile = null;
                var tempDirectory = Path.Combine(_backupDirectory, "temp");

                if (!Directory.Exists(tempDirectory))
                    Directory.CreateDirectory(tempDirectory);

                using (var zipArchive = ZipFile.OpenRead(backupFilePath))
                {
                    var bakEntry = zipArchive.Entries.FirstOrDefault(e => e.Name.EndsWith(".bak"));
                    if (bakEntry == null)
                        throw new Exception("No .bak file found in the ZIP archive");

                    extractedBakFile = Path.Combine(tempDirectory, bakEntry.Name);
                    bakEntry.ExtractToFile(extractedBakFile, overwrite: true);
                }

                // Get logical file names from backup
                var logicalDataName = $"{databaseName}";
                var logicalLogName = $"{databaseName}_log";

                // Get physical file paths
                var dataFilePath = string.Empty;
                var logFilePath = string.Empty;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Get physical file paths
                    var getFilePathsQuery = $@"
                        SELECT physical_name 
                        FROM sys.master_files 
                        WHERE database_id = DB_ID('{databaseName}')";

                    using var getFilesCommand = new SqlCommand(getFilePathsQuery, connection);
                    using var reader = await getFilesCommand.ExecuteReaderAsync();

                    var filePaths = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        filePaths.Add(reader.GetString(0));
                    }

                    if (filePaths.Count >= 1)
                        dataFilePath = filePaths[0];
                    if (filePaths.Count >= 2)
                        logFilePath = filePaths[1];
                }

                // Restore database with REPLACE option
                var restoreQuery = $@"
                    USE master;
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    
                    RESTORE DATABASE [{databaseName}] 
                    FROM DISK = N'{extractedBakFile}'
                    WITH FILE = 1,
                    MOVE N'{logicalDataName}' TO N'{dataFilePath}',
                    MOVE N'{logicalLogName}' TO N'{logFilePath}',
                    NOUNLOAD, REPLACE, STATS = 10;
                    
                    ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    connection.ChangeDatabase("master");

                    using var command = new SqlCommand(restoreQuery, connection);
                    command.CommandTimeout = 300; // 5 minutes timeout
                    await command.ExecuteNonQueryAsync();
                }

                // Clean up temporary files
                if (File.Exists(extractedBakFile))
                    File.Delete(extractedBakFile);

                if (Directory.Exists(tempDirectory) && !Directory.EnumerateFileSystemEntries(tempDirectory).Any())
                    Directory.Delete(tempDirectory);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Restore failed: {ex.Message}", ex);
            }
        }

        public Task<IEnumerable<BackupInfo>> GetBackupHistoryAsync()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "*.zip")
                    .Select(filePath => new FileInfo(filePath))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new BackupInfo
                    {
                        FileName = f.Name,
                        FilePath = f.FullName,
                        FileSize = f.Length,
                        FileSizeFormatted = FormatFileSize(f.Length),
                        CreatedDate = f.CreationTime
                    });

                return Task.FromResult(backupFiles);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get backup history: {ex.Message}", ex);
            }
        }

        public Task<bool> DeleteBackupAsync(string backupFileName)
        {
            try
            {
                var filePath = Path.Combine(_backupDirectory, backupFileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete backup: {ex.Message}", ex);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
