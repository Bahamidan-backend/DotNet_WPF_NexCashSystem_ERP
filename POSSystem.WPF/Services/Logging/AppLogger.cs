using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace POSSystem.WPF.Services.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal,
        Audit
    }

    public sealed class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string CallerMember { get; set; } = string.Empty;
        public string CallerFile { get; set; } = string.Empty;
        public int CallerLine { get; set; }
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// نظام تسجيل وتدقيق مكتبي متكامل فائق السرعة وموثوق للإنتاج (Production-Grade Zero-Dependency Logger).
    /// يدعم الكتابة الآمنة متعددة الخيوط (Thread-Safe Non-Blocking Queue)، التدوير اليومي، وحفظ سجلات الأعطال والتدقيق المحاسبي.
    /// </summary>
    public static class AppLogger
    {
        private static readonly BlockingCollection<LogEntry> _logQueue = new(new ConcurrentQueue<LogEntry>());
        private static readonly Thread _loggingThread;
        private static readonly object _fileLock = new();
        private static readonly string _logDirectory;
        private static volatile bool _isDisposed;

        public static string LogDirectory => _logDirectory;

        static AppLogger()
        {
            // تحديد مسار مجلد السجلات (يفضل بجوار البرنامج، أو في LocalAppData عند عدم توفر صلاحيات كتابة)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string primaryDir = Path.Combine(baseDir, "logs");

            try
            {
                if (!Directory.Exists(primaryDir))
                {
                    Directory.CreateDirectory(primaryDir);
                }
                // اختبار إمكانية الكتابة
                string testFile = Path.Combine(primaryDir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                _logDirectory = primaryDir;
            }
            catch
            {
                string fallbackDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NexSystem",
                    "logs");
                Directory.CreateDirectory(fallbackDir);
                _logDirectory = fallbackDir;
            }

            // خيط خلفي مخصص لكتابة السجلات دون إعاقة واجهة المستخدم أو العمليات المالية
            _loggingThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "AppLoggerBackgroundWorker",
                Priority = ThreadPriority.BelowNormal
            };
            _loggingThread.Start();

            // تنظيف السجلات القديمة تلقائياً (الاحتفاظ بآخر 30 يوماً فقط)
            Task.Run(CleanOldLogsSilently);

            // تسجيل حدث الإغلاق لضمان إفراغ السجلات المعلقة
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
        }

        #region Public Logging API

        public static void LogDebug(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Enqueue(LogLevel.Debug, message, null, caller, file, line);
        }

        public static void LogInfo(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Enqueue(LogLevel.Info, message, null, caller, file, line);
        }

        public static void LogWarning(string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Enqueue(LogLevel.Warning, message, null, caller, file, line);
        }

        public static void LogError(string message,
            Exception? ex = null,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Enqueue(LogLevel.Error, message, ex, caller, file, line);
        }

        public static void LogError(Exception ex,
            string message = "",
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Enqueue(LogLevel.Error, string.IsNullOrWhiteSpace(message) ? ex.Message : message, ex, caller, file, line);
        }

        public static void LogFatal(Exception ex,
            string message = "FATAL UNHANDLED EXCEPTION",
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Enqueue(LogLevel.Fatal, message, ex, caller, file, line);
            // في حالة الخطأ القاتل، اكتب مباشرة وتزامنياً لضمان حفظ الملف فوراً قبل انهيار العملية
            WriteSynchronously(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Fatal,
                Message = message,
                Exception = ex,
                CallerMember = caller,
                CallerFile = Path.GetFileName(file),
                CallerLine = line
            });
        }

        /// <summary>
        /// تسجيل العمليات الحساسة وتدقيق الأمان (Audit Logging): فتح/إغلاق الوردية، إلغاء الفواتير، الخصومات الكبرى، الحذف.
        /// </summary>
        public static void LogAudit(string user, string action, string details,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            string auditMessage = $"[USER: {user}] [ACTION: {action}] {details}";
            Enqueue(LogLevel.Audit, auditMessage, null, caller, file, line);
        }

        public static void OpenLogFolder()
        {
            try
            {
                if (Directory.Exists(_logDirectory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _logDirectory,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to open log directory in explorer");
            }
        }

        #endregion

        #region Internal Implementation

        private static void Enqueue(LogLevel level, string message, Exception? ex, string caller, string file, int line)
        {
            if (_isDisposed) return;

            try
            {
                _logQueue.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Message = message,
                    Exception = ex,
                    CallerMember = caller,
                    CallerFile = Path.GetFileName(file),
                    CallerLine = line
                });
            }
            catch
            {
                // Fallback to avoid throwing in logger
            }
        }

        private static void ProcessLogQueue()
        {
            try
            {
                foreach (var entry in _logQueue.GetConsumingEnumerable())
                {
                    WriteEntryToFile(entry);
                }
            }
            catch
            {
                // Background worker safe exit
            }
        }

        private static void WriteEntryToFile(LogEntry entry)
        {
            lock (_fileLock)
            {
                try
                {
                    string dateStr = entry.Timestamp.ToString("yyyy-MM-dd");
                    string logFileName = Path.Combine(_logDirectory, $"app_{dateStr}.log");
                    string formattedLine = FormatLogEntry(entry);

                    File.AppendAllText(logFileName, formattedLine, Encoding.UTF8);

                    // إذا كانت عملية تدقيق أمان أو محاسبية (Audit)، اكتبها أيضاً في ملف التدقيق المنفصل
                    if (entry.Level == LogLevel.Audit)
                    {
                        string auditFileName = Path.Combine(_logDirectory, $"audit_{dateStr}.log");
                        File.AppendAllText(auditFileName, formattedLine, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Ignore file write errors in queue loop to prevent cascading crashes
                }
            }
        }

        private static void WriteSynchronously(LogEntry entry)
        {
            lock (_fileLock)
            {
                try
                {
                    string dateStr = entry.Timestamp.ToString("yyyy-MM-dd");
                    string logFileName = Path.Combine(_logDirectory, $"crash_{dateStr}.log");
                    string formattedLine = FormatLogEntry(entry);
                    File.AppendAllText(logFileName, formattedLine, Encoding.UTF8);

                    string appLog = Path.Combine(_logDirectory, $"app_{dateStr}.log");
                    File.AppendAllText(appLog, formattedLine, Encoding.UTF8);
                }
                catch
                {
                    // Fallback attempt
                }
            }
        }

        private static string FormatLogEntry(LogEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ");
            sb.Append('[').Append(entry.Level.ToString().ToUpperInvariant().PadRight(5)).Append("] ");
            sb.Append("[T:").Append(entry.ThreadId).Append("] ");

            if (!string.IsNullOrEmpty(entry.CallerFile))
            {
                sb.Append('[').Append(entry.CallerFile);
                if (!string.IsNullOrEmpty(entry.CallerMember))
                {
                    sb.Append('.').Append(entry.CallerMember);
                }
                if (entry.CallerLine > 0)
                {
                    sb.Append(':').Append(entry.CallerLine);
                }
                sb.Append("] ");
            }

            sb.Append(entry.Message);

            if (entry.Exception != null)
            {
                sb.AppendLine();
                sb.Append("   Exception: ").Append(entry.Exception.GetType().FullName).Append(": ").Append(entry.Exception.Message).AppendLine();
                sb.Append("   StackTrace: ").Append(entry.Exception.StackTrace);
                if (entry.Exception.InnerException != null)
                {
                    sb.AppendLine();
                    sb.Append("   --> Inner Exception: ").Append(entry.Exception.InnerException.GetType().FullName).Append(": ").Append(entry.Exception.InnerException.Message).AppendLine();
                    sb.Append("       Inner StackTrace: ").Append(entry.Exception.InnerException.StackTrace);
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static void CleanOldLogsSilently()
        {
            try
            {
                if (!Directory.Exists(_logDirectory)) return;

                var cutoffDate = DateTime.Now.AddDays(-30);
                var directoryInfo = new DirectoryInfo(_logDirectory);

                foreach (var file in directoryInfo.GetFiles("*.log"))
                {
                    if (file.LastWriteTime < cutoffDate)
                    {
                        try { file.Delete(); } catch { }
                    }
                }
            }
            catch
            {
                // Silently swallow cleanup exceptions
            }
        }

        public static void Flush()
        {
            // Give queue worker time to drain
            int waitCount = 0;
            while (_logQueue.Count > 0 && waitCount < 10)
            {
                Thread.Sleep(50);
                waitCount++;
            }
        }

        public static void Shutdown()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _logQueue.CompleteAdding();
                Flush();
            }
            catch
            {
            }
        }

        #endregion
    }
}

