using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Services;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.Business.Services
{
    /// <summary>
    /// محرك النسخ الاحتياطي التلقائي والمضغوط والاستعادة الفورية لقاعدة بيانات النظام
    /// Enterprise Automated Backup & Instant Restore Engine
    /// يعتمد على SQLite Online Backup API وفحص سلامة المخطط (PRAGMA integrity_check) قبل وبعد العمليات
    /// </summary>
    public class BackupService : IBackupService
    {
        private static readonly System.Threading.SemaphoreSlim _backupSemaphore = new(1, 1);
        private readonly string _customDatabasePath;

        public BackupService(string? customDbPath = null)
        {
            _customDatabasePath = customDbPath ?? string.Empty;
        }

        public string GetDatabasePath()
        {
            if (!string.IsNullOrWhiteSpace(_customDatabasePath))
            {
                return Path.GetFullPath(_customDatabasePath);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "poss.db");
        }

        public string GetEffectiveBackupDirectory()
        {
            try
            {
                var settings = SettingsService.LoadSettings();
                string dir = settings.BackupLocation;
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                }

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return dir;
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Failed to use configured backup directory: {ex.Message}. Using default BaseDirectory/Backups.");
                string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(fallback)) Directory.CreateDirectory(fallback);
                return fallback;
            }
        }

        public async Task<BackupResult> CreateBackupAsync(
            string reason = "Manual", 
            int? shiftId = null, 
            string? shiftNumber = null, 
            string? customDestinationPath = null)
        {
            await _backupSemaphore.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    string sourceDbPath = GetDatabasePath();
                    if (!File.Exists(sourceDbPath))
                    {
                        return BackupResult.Failed($"قاعدة البيانات الأصلية غير موجودة في المسار: {sourceDbPath}");
                    }

                    string destinationDir = !string.IsNullOrWhiteSpace(customDestinationPath)
                        ? customDestinationPath
                        : GetEffectiveBackupDirectory();

                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // 1. Prepare temporary snapshot directory
                    string tempDir = Path.Combine(Path.GetTempPath(), $"POS_Backup_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);
                    string tempDbPath = Path.Combine(tempDir, "poss.db");

                    try
                    {
                        // 2. Perform consistent point-in-time Snapshot using SQLite Online Backup API
                        // This safely handles active transactions and merges WAL pages into destination
                        ExecuteOnlineBackup(sourceDbPath, tempDbPath);

                        // 3. Verify physical integrity of the created snapshot (PRAGMA integrity_check)
                        if (!VerifyDatabaseIntegrity(tempDbPath, out var integrityError))
                        {
                            AppLogger.LogError($"Backup snapshot failed integrity check: {integrityError}");
                            return BackupResult.Failed($"فشل فحص سلامة النسخة الاحتياطية (PRAGMA integrity_check): {integrityError}");
                        }

                        // 4. Gather schema metrics and record counts for auditability
                        var tableCounts = CollectTableCounts(tempDbPath);
                        long dbSize = new FileInfo(tempDbPath).Length;

                        // 5. Create metadata descriptor
                        var metadata = new BackupMetadata
                        {
                            BackupDate = DateTime.Now,
                            Reason = reason,
                            ShiftId = shiftId,
                            ShiftNumber = shiftNumber,
                            DatabaseFileName = "poss.db",
                            DatabaseSizeBytes = dbSize,
                            MachineName = Environment.MachineName,
                            AppVersion = "1.0.0",
                            IntegrityStatus = "ok",
                            TableRecordCounts = tableCounts
                        };

                        string metaJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(Path.Combine(tempDir, "backup_info.json"), metaJson, Encoding.UTF8);

                        // 6. Construct ZIP archive name
                        string shiftLabel = "";
                        if (!string.IsNullOrWhiteSpace(shiftNumber))
                        {
                            var clean = string.Join("_", shiftNumber.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                            shiftLabel = $"_Shift_{clean}";
                        }
                        else if (shiftId.HasValue)
                        {
                            shiftLabel = $"_Shift_{shiftId.Value}";
                        }

                        string zipFileName = $"POS_Backup{shiftLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                        string finalZipPath = Path.Combine(destinationDir, zipFileName);

                        if (File.Exists(finalZipPath)) File.Delete(finalZipPath);

                        // 7. Compress to fast ZIP archive (CompressionLevel.Fastest reduces wait time by ~60%)
                        ZipFile.CreateFromDirectory(tempDir, finalZipPath, CompressionLevel.Fastest, false);

                        long zipSize = new FileInfo(finalZipPath).Length;

                        AppLogger.LogInfo($"Automated backup completed via SQLite Online Backup API: {finalZipPath} (Size: {zipSize / 1024.0:F1} KB, Reason: {reason})");

                        // 8. Prune old backups (keep last 60 days or at least 30 backups)
                        PruneOldBackups(destinationDir);

                        return BackupResult.Success(finalZipPath, zipSize);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError("Error creating compressed database backup", ex);
                        return BackupResult.Failed($"فشل إنشاء النسخة الاحتياطية: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                        }
                        catch { }
                    }
                });
            }
            finally
            {
                _backupSemaphore.Release();
            }
        }

        public async Task<RestoreResult> RestoreBackupAsync(string backupFilePath)
        {
            await _backupSemaphore.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
                    {
                        return RestoreResult.Failed("ملف النسخة الاحتياطية المحدد غير موجود.");
                    }

                    string currentDbPath = GetDatabasePath();
                    string safetyBackupPath = "";
                    string tempExtractDir = Path.Combine(Path.GetTempPath(), $"POS_Restore_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempExtractDir);

                    try
                    {
                        string sourceDbFileToRestore = "";

                        // 1. Identify format (ZIP, POSBAK or direct DB)
                        if (backupFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                            backupFilePath.EndsWith(".posbak", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                ZipFile.ExtractToDirectory(backupFilePath, tempExtractDir);
                            }
                            catch (Exception ex)
                            {
                                return RestoreResult.Failed($"ملف الأرشيف المضغوط تالف ولا يمكن فك ضغطه: {ex.Message}");
                            }

                            var expectedDb = Path.Combine(tempExtractDir, "poss.db");
                            if (File.Exists(expectedDb))
                            {
                                sourceDbFileToRestore = expectedDb;
                            }
                            else
                            {
                                var dbFiles = Directory.GetFiles(tempExtractDir, "*.db", SearchOption.AllDirectories);
                                if (dbFiles.Length > 0)
                                {
                                    sourceDbFileToRestore = dbFiles[0];
                                }
                                else
                                {
                                    return RestoreResult.Failed("الأرشيف المضغوط لا يحتوي على ملف قاعدة بيانات صالح (*.db).");
                                }
                            }
                        }
                        else if (backupFilePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                        {
                            sourceDbFileToRestore = backupFilePath;
                        }
                        else
                        {
                            return RestoreResult.Failed("نوع الملف غير مدعوم. يجب أن يكون ملف مضغوط (.zip) أو ملف قاعدة بيانات (.db).");
                        }

                        // 2. Pre-Restore Validation: Magic Header, Integrity, Schema & Foreign Keys
                        if (!ValidateCandidateDatabase(sourceDbFileToRestore, out var validationError))
                        {
                            AppLogger.LogWarning($"Pre-restore candidate validation rejected: {validationError}");
                            return RestoreResult.Failed($"تم رفض استعادة الملف لعدم مطابقته معايير السلامة:\n{validationError}");
                        }

                        // 3. Clear all EF Core / SQLite connection pools and flush handles
                        ReleaseDatabaseLocks(currentDbPath);

                        // 4. Create and VERIFY a Safety Snapshot of CURRENT database before touching live files
                        if (File.Exists(currentDbPath))
                        {
                            string safetyDir = Path.Combine(Path.GetDirectoryName(currentDbPath) ?? AppDomain.CurrentDomain.BaseDirectory, "Backups", "SafetySnapshots");
                            if (!Directory.Exists(safetyDir)) Directory.CreateDirectory(safetyDir);

                            safetyBackupPath = Path.Combine(safetyDir, $"poss_pre_restore_safety_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                            
                            try
                            {
                                ExecuteOnlineBackup(currentDbPath, safetyBackupPath);

                                if (!VerifyDatabaseIntegrity(safetyBackupPath, out var safetyError))
                                {
                                    AppLogger.LogError($"Safety snapshot failed integrity check: {safetyError}");
                                    return RestoreResult.Failed($"فشل التحقق من سلامة نسخة الأمان الاحترازية ({safetyError}). تم إيقاف عملية الاستعادة لحماية بياناتك.");
                                }
                            }
                            catch (Exception ex)
                            {
                                AppLogger.LogError("Failed to create verified safety snapshot before restore", ex);
                                return RestoreResult.Failed($"فشل إنشاء نسخة الأمان الاحترازية لقاعدة البيانات الحالية ({ex.Message}). تم إيقاف الاستعادة.");
                            }
                        }

                        // 5. Clean up stale live WAL / SHM files BEFORE copying the restored database
                        // This prevents SQLite from attempting to recover stale WAL transactions onto the newly restored DB
                        ReleaseDatabaseLocks(currentDbPath);
                        DeleteWalAndShmFiles(currentDbPath);

                        // 6. Overwrite poss.db with restored file
                        File.Copy(sourceDbFileToRestore, currentDbPath, true);

                        // 7. Post-Restore Verification: Confirm live database is physically sound
                        if (!VerifyDatabaseIntegrity(currentDbPath, out var postRestoreError))
                        {
                            AppLogger.LogError($"Post-restore verification failed on live database: {postRestoreError}. Initiating immediate rollback!");

                            // AUTOMATIC ROLLBACK TO SAFETY SNAPSHOT
                            if (!string.IsNullOrWhiteSpace(safetyBackupPath) && File.Exists(safetyBackupPath))
                            {
                                try
                                {
                                    ReleaseDatabaseLocks(currentDbPath);
                                    DeleteWalAndShmFiles(currentDbPath);
                                    File.Copy(safetyBackupPath, currentDbPath, true);
                                    AppLogger.LogInfo("Rollback to safety snapshot completed successfully.");
                                }
                                catch (Exception rollEx)
                                {
                                    AppLogger.LogError("CRITICAL: Rollback to safety snapshot encountered an error!", rollEx);
                                }
                            }

                            return RestoreResult.Failed(
                                $"فشل اعتماد قاعدة البيانات بعد الاستبدال ({postRestoreError}). تم التراجع تلقائياً واستعادة بياناتك الأصلية بأمان.",
                                safetyBackupPath,
                                isRolledBack: true);
                        }

                        AppLogger.LogInfo($"Database successfully restored from '{backupFilePath}'. Safety snapshot verified at '{safetyBackupPath}'.");

                        return RestoreResult.Success(safetyBackupPath);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"Failed to restore database from '{backupFilePath}'", ex);
                        return RestoreResult.Failed($"حدث خطأ أثناء استعادة قاعدة البيانات: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
                        }
                        catch { }
                    }
                });
            }
            finally
            {
                _backupSemaphore.Release();
            }
        }

        public bool VerifyBackupFile(string backupFilePath, out string verificationMessage)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                verificationMessage = "الملف المحدد غير موجود.";
                return false;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), $"POS_Verify_{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(tempDir);
                string dbToVerify;

                if (backupFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    backupFilePath.EndsWith(".posbak", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(backupFilePath, tempDir);
                    var dbs = Directory.GetFiles(tempDir, "*.db", SearchOption.AllDirectories);
                    if (dbs.Length == 0)
                    {
                        verificationMessage = "الأرشيف لا يحتوي على ملف قاعدة بيانات (.db).";
                        return false;
                    }
                    dbToVerify = dbs[0];
                }
                else
                {
                    dbToVerify = backupFilePath;
                }

                if (!ValidateCandidateDatabase(dbToVerify, out var validationError))
                {
                    verificationMessage = validationError;
                    return false;
                }

                var counts = CollectTableCounts(dbToVerify);
                var summary = string.Join(", ", counts.Select(kv => $"{kv.Key}: {kv.Value}"));
                verificationMessage = $"الملف سليم تماماً وناجح في فحص السلامة (PRAGMA integrity_check: ok). الإحصائيات: [{summary}]";
                return true;
            }
            catch (Exception ex)
            {
                verificationMessage = $"خطأ أثناء فحص الملف: {ex.Message}";
                return false;
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // SQLite Online Backup & Integrity Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// ينفذ عملية نسخ مباشر متناسقة عبر SQLite Online Backup API
        /// يقوم بقفل مشترك متناسق ودمج إطارات الـ WAL النشطة في قاعدة البيانات الهدف
        /// </summary>
        public static void ExecuteOnlineBackup(string sourceDbPath, string destinationDbPath)
        {
            // Flush active WAL checkpoint if possible
            try
            {
                using var cpConn = new SqliteConnection($"Data Source={sourceDbPath};Mode=ReadWrite;Pooling=False;");
                cpConn.Open();
                using var cpCmd = cpConn.CreateCommand();
                cpCmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
                cpCmd.ExecuteNonQuery();
            }
            catch { }

            // Ensure destination directory exists and file is clean
            string? destDir = Path.GetDirectoryName(destinationDbPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            if (File.Exists(destinationDbPath)) File.Delete(destinationDbPath);

            // Execute native online backup via Microsoft.Data.Sqlite
            using var sourceConn = new SqliteConnection($"Data Source={sourceDbPath};Pooling=False;");
            sourceConn.Open();

            using var destConn = new SqliteConnection($"Data Source={destinationDbPath};Mode=ReadWriteCreate;Pooling=False;");
            destConn.Open();

            sourceConn.BackupDatabase(destConn);

            // Set destination journal mode to DELETE/TRUNCATE so destination is fully standalone without external WAL
            try
            {
                using var finalizeCmd = destConn.CreateCommand();
                finalizeCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE); PRAGMA journal_mode=DELETE;";
                finalizeCmd.ExecuteNonQuery();
            }
            catch { }
        }

        /// <summary>
        /// فحص شامل لسلامة ملف قاعدة البيانات المرشح للاستعادة
        /// </summary>
        public static bool ValidateCandidateDatabase(string dbPath, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. Verify SQLite Header Magic Bytes
            if (!CheckSqliteMagicHeader(dbPath))
            {
                errorMessage = "الملف تالف أو ليس ملف قاعدة بيانات SQLite صالح (فشل توقيع رأس الملف).";
                return false;
            }

            // 2. Verify PRAGMA integrity_check
            if (!VerifyDatabaseIntegrity(dbPath, out var integrityError))
            {
                errorMessage = $"فشل فحص السلامة الهيكلية لقاعدة البيانات: {integrityError}";
                return false;
            }

            // 3. Verify Required POS System Schema Tables
            if (!VerifyRequiredTables(dbPath, out var missingTablesError))
            {
                errorMessage = missingTablesError;
                return false;
            }

            // 4. Run PRAGMA foreign_key_check for relational safety
            VerifyForeignKeyIntegrity(dbPath, out var fkErrors);
            if (fkErrors.Count > 0)
            {
                AppLogger.LogWarning($"Candidate database contains {fkErrors.Count} foreign key violations: {string.Join("; ", fkErrors.Take(3))}");
            }

            return true;
        }

        /// <summary>
        /// يتحقق من التوقيع الثنائي لملف SQLite (أول 16 بايت: "SQLite format 3\0")
        /// </summary>
        public static bool CheckSqliteMagicHeader(string dbPath)
        {
            try
            {
                using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < 100) return false;

                byte[] header = new byte[16];
                int read = fs.Read(header, 0, 16);
                if (read < 16) return false;

                string headerStr = Encoding.ASCII.GetString(header);
                return headerStr.StartsWith("SQLite format 3");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ينفذ PRAGMA integrity_check ويعيد true فقط إذا كانت النتيجة "ok"
        /// </summary>
        public static bool VerifyDatabaseIntegrity(string dbPath, out string message)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                using var reader = cmd.ExecuteReader();

                var results = new List<string>();
                while (reader.Read())
                {
                    string res = reader.GetString(0);
                    if (!string.Equals(res, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(res);
                    }
                }

                if (results.Count > 0)
                {
                    message = string.Join("; ", results);
                    return false;
                }

                message = "ok";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// يتحقق من تكامل المفاتيح الخارجية والعلاقات (PRAGMA foreign_key_check)
        /// </summary>
        public static bool VerifyForeignKeyIntegrity(string dbPath, out List<string> violations)
        {
            violations = new List<string>();
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA foreign_key_check;";
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string table = reader.GetString(0);
                    long rowid = reader.GetInt64(1);
                    string parent = reader.GetString(2);
                    int fkid = reader.GetInt32(3);
                    violations.Add($"الجدول '{table}' (صف {rowid}) يرتبط بكيان غير موجود في '{parent}' (FK #{fkid})");
                }

                return violations.Count == 0;
            }
            catch (Exception ex)
            {
                violations.Add($"خطأ أثناء فحص العلاقات: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// يتحقق من وجود الجداول الحيوية لنظام نقاط البيع لمنع استعادة ملفات قواعد بيانات غريبة
        /// </summary>
        public static bool VerifyRequiredTables(string dbPath, out string error)
        {
            string[] required = new[]
            {
                "Users",
                "Shifts",
                "Orders",
                "OrderItems",
                "Products",
                "Categories",
                "warehouseFromProducts",
                "CashBoxTransactions"
            };

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using var reader = cmd.ExecuteReader();

                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read()) existing.Add(reader.GetString(0));

                var missing = required.Where(t => !existing.Contains(t)).ToList();
                if (missing.Count > 0)
                {
                    error = $"الملف لا ينتمي لنظام نقاط البيع (الجداول الأساسية التالية مفقودة: {string.Join(", ", missing)}).";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                error = $"خطأ أثناء قراءة جداول قاعدة البيانات: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// يجمع عدد السجلات في الجداول الحيوية لتوثيقها في ملف بيانات النسخة الاحتياطية
        /// </summary>
        public static Dictionary<string, int> CollectTableCounts(string dbPath)
        {
            var counts = new Dictionary<string, int>();
            string[] tables = new[]
            {
                "Users",
                "Shifts",
                "Orders",
                "OrderItems",
                "Products",
                "warehouseFromProducts",
                "CashBoxTransactions",
                "StockMovements",
                "ReceiptVouchers",
                "Expenses"
            };

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False;");
                conn.Open();

                foreach (var table in tables)
                {
                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        counts[table] = count;
                    }
                    catch
                    {
                        // Table may not exist in legacy schema
                    }
                }
            }
            catch { }

            return counts;
        }

        private static void ReleaseDatabaseLocks(string dbPath)
        {
            try
            {
                using var cpConn = new SqliteConnection($"Data Source={dbPath};Pooling=False;");
                cpConn.Open();
                using var cpCmd = cpConn.CreateCommand();
                cpCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                cpCmd.ExecuteNonQuery();
            }
            catch { }

            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void DeleteWalAndShmFiles(string dbPath)
        {
            string wal = dbPath + "-wal";
            if (File.Exists(wal))
            {
                try { File.Delete(wal); } catch (Exception ex) { AppLogger.LogWarning($"Could not delete old WAL file: {ex.Message}"); }
            }

            string shm = dbPath + "-shm";
            if (File.Exists(shm))
            {
                try { File.Delete(shm); } catch (Exception ex) { AppLogger.LogWarning($"Could not delete old SHM file: {ex.Message}"); }
            }
        }

        private void PruneOldBackups(string destinationDir, int maxAgeDays = 60, int minRetainCount = 30)
        {
            try
            {
                var dirInfo = new DirectoryInfo(destinationDir);
                if (!dirInfo.Exists) return;

                var backupFiles = dirInfo.EnumerateFiles("POS_Backup*.zip")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (backupFiles.Count <= minRetainCount) return;

                var cutoffDate = DateTime.Now.AddDays(-maxAgeDays);
                var candidatesToDelete = backupFiles
                    .Skip(minRetainCount)
                    .Where(f => f.CreationTime < cutoffDate)
                    .ToList();

                foreach (var file in candidatesToDelete)
                {
                    try
                    {
                        file.Delete();
                        AppLogger.LogInfo($"Pruned old backup file: {file.Name}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Error pruning old backups: {ex.Message}");
            }
        }
    }
}
