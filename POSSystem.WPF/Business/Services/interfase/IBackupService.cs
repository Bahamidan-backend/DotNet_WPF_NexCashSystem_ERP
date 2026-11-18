using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services.interfase
{
    public class BackupResult
    {
        public bool IsSuccess { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string FileSizeText
        {
            get
            {
                if (FileSizeBytes < 1024) return $"{FileSizeBytes} بايت";
                if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} كيلوبايت";
                return $"{FileSizeBytes / (1024.0 * 1024.0):N0} ميجابايت";
            }
        }

        public static BackupResult Success(string filePath, long size) => new()
        {
            IsSuccess = true,
            FilePath = filePath,
            FileSizeBytes = size,
            Timestamp = DateTime.Now
        };

        public static BackupResult Failed(string error) => new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            Timestamp = DateTime.Now
        };
    }

    public class RestoreResult
    {
        public bool IsSuccess { get; set; }
        public string? SafetyBackupPath { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsRolledBack { get; set; }

        public static RestoreResult Success(string safetyPath) => new()
        {
            IsSuccess = true,
            SafetyBackupPath = safetyPath
        };

        public static RestoreResult Failed(string error, string? safetyPath = null, bool isRolledBack = false) => new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            SafetyBackupPath = safetyPath,
            IsRolledBack = isRolledBack
        };
    }

    public class BackupMetadata
    {
        public DateTime BackupDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? ShiftId { get; set; }
        public string? ShiftNumber { get; set; }
        public string DatabaseFileName { get; set; } = "poss.db";
        public long DatabaseSizeBytes { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string AppVersion { get; set; } = "1.0.0";
        public string IntegrityStatus { get; set; } = "ok";
        public Dictionary<string, int>? TableRecordCounts { get; set; }
    }

    public interface IBackupService
    {
        /// <summary>
        /// إنشاء نسخة احتياطية مضغوطة من قاعدة البيانات باستخدام SQLite Online Backup API مع فحص السلامة
        /// </summary>
        Task<BackupResult> CreateBackupAsync(string reason = "Manual", int? shiftId = null, string? shiftNumber = null, string? customDestinationPath = null);

        /// <summary>
        /// استعادة قاعدة البيانات من ملف مضغوط (.zip) أو ملف قاعدة بيانات (.db) مع فحص شامل قبل وبعد الاستبدال مع أخذ نسخة أمان احترازية مؤكدة
        /// </summary>
        Task<RestoreResult> RestoreBackupAsync(string backupFilePath);

        /// <summary>
        /// جلب مسار مجلد النسخ الاحتياطي الافتراضي المعتمد
        /// </summary>
        string GetEffectiveBackupDirectory();

        /// <summary>
        /// جلب مسار قاعدة البيانات الحالية المعتمدة
        /// </summary>
        string GetDatabasePath();

        /// <summary>
        /// فحص سلامة وهيكل ملف النسخة الاحتياطية دون تطبيقه
        /// </summary>
        bool VerifyBackupFile(string backupFilePath, out string verificationMessage);
    }
}

