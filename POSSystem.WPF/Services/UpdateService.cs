using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace POSSystem.WPF.Services
{

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string BuildDate { get; set; } = "";
        public string Changelog { get; set; } = "";
        public bool Mandatory { get; set; } = false;
        public string MinVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }


    public class LocalVersionInfo
    {
        public string Version { get; set; } = "0.0.0";
        public string BuildDate { get; set; } = "";
    }

    public class UpdateService
    {
      
        private const string VERSION_INFO_URL = "https://www.dropbox.com/scl/fi/cbun4s4oe294wbkxwtmx3/remote_version.json?rlkey=nv1pr2drc5xe59u8t70kbi6im&st=bpn5nfye&dl=1";

        private const string LOCAL_VERSION_FILE = "version.json";
        private const string UPDATE_TEMP_DIR = "update_temp";
        private const string UPDATE_ZIP_NAME = "update.zip";
        private const string UPDATER_SCRIPT = "Updater.bat";

        private static readonly HttpClient _httpClient;

        static UpdateService()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                AllowAutoRedirect = true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromHours(1)
            };

            _httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

       
        public string GetCurrentVersion()
        {
            try
            {
                if (File.Exists(LOCAL_VERSION_FILE))
                {
                    string json = File.ReadAllText(LOCAL_VERSION_FILE);
                    var versionInfo = JsonSerializer.Deserialize<LocalVersionInfo>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return versionInfo?.Version ?? "0.0.0";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading local version: {ex.Message}");
            }
            return "0.0.0";
        }

        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
           

            try
            {
                var response = await _httpClient.GetAsync(VERSION_INFO_URL);
                response.EnsureSuccessStatusCode();

                string jsonString = await response.Content.ReadAsStringAsync();
                
                var remoteInfo = JsonSerializer.Deserialize<UpdateInfo>(jsonString, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (remoteInfo == null || string.IsNullOrEmpty(remoteInfo.Version))
                    return null;

                // مقارنة الإصدارات
                string currentVersion = GetCurrentVersion();
                if (IsNewerVersion(remoteInfo.Version, currentVersion))
                {
                    return remoteInfo;
                }

                return null; // البرنامج محدّث
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"فشل الاتصال بالسيرفر: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"خطأ في قراءة بيانات التحديث (تأكد من صيغة ملف version.json): {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التحقق من التحديثات: {ex.Message}");
            }
        }

        /// <summary>
        /// يحمّل ملف التحديث مع تتبع نسبة التحميل
        /// </summary>
        /// <param name="downloadUrl">رابط التحميل</param>
        /// <param name="progress">تقرير التقدم (0-100)</param>
        /// <returns>مسار الملف المحمّل</returns>
        public async Task<string> DownloadUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(downloadUrl))
                throw new Exception("لا يوجد رابط تحميل للتحديث. تأكد من توفر ملف التحديث.");

            // تصحيح روابط Dropbox تلقائياً لضمان التحميل المباشر
            if (downloadUrl.Contains("dropbox.com", StringComparison.OrdinalIgnoreCase) && downloadUrl.Contains("dl=0"))
            {
                downloadUrl = downloadUrl.Replace("dl=0", "dl=1");
            }

            // إنشاء مجلد مؤقت بجانب ملف التشغيل الفعلي
            string appDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string tempDir = Path.Combine(appDir, UPDATE_TEMP_DIR);
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, UPDATE_ZIP_NAME);
            string partPath = zipPath + ".part";

            const int maxRetries = 6;
            int attempt = 0;
            long totalBytes = -1;

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    long existingBytes = 0;
                    if (File.Exists(partPath))
                    {
                        existingBytes = new FileInfo(partPath).Length;
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                    if (existingBytes > 0)
                    {
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
                    }

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    bool isPartial = response.StatusCode == System.Net.HttpStatusCode.PartialContent;
                    if (!response.IsSuccessStatusCode && !isPartial)
                    {
                        response.EnsureSuccessStatusCode();
                    }

                    if (isPartial)
                    {
                        if (response.Content.Headers.ContentRange?.Length != null)
                        {
                            totalBytes = response.Content.Headers.ContentRange.Length.Value;
                        }
                    }
                    else
                    {
                        // إذا أعاد السيرفر 200 OK، نبدأ من البداية
                        existingBytes = 0;
                        totalBytes = response.Content.Headers.ContentLength ?? -1;
                    }

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(
                        partPath,
                        existingBytes > 0 ? FileMode.Append : FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        65536,
                        true))
                    {
                        long downloadedBytes = existingBytes;
                        var buffer = new byte[65536];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                int percentComplete = (int)((downloadedBytes * 100) / totalBytes);
                                progress?.Report(Math.Min(99, percentComplete));
                            }
                        }
                    }

                    // التحميل اكتمل بنجاح، نقل الملف المؤقت إلى ملف التحديث النهائي
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);
                    File.Move(partPath, zipPath);

                    progress?.Report(100);
                    return zipPath;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        if (File.Exists(partPath))
                        {
                            try { File.Delete(partPath); } catch { }
                        }
                        throw new Exception($"فشل تحميل التحديث بعد عدة محاولات (تأكد من استقرار الإنترنت): {ex.Message}");
                    }

                    // انتظار قصير قبل استئناف التحميل تلقائياً من النقطة التي توقف عندها
                    await Task.Delay(2000);
                }
            }

            throw new Exception("تعذر إكمال تحميل ملف التحديث.");
        }

        /// <summary>
        /// يطبّق التحديث: يشغّل Updater.bat ويغلق التطبيق
        /// </summary>
        /// <param name="zipPath">مسار ملف update.zip المحمّل</param>
        public void ApplyUpdate(string zipPath)
        {
            string appDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string updaterPath = Path.Combine(appDir, UPDATER_SCRIPT);

            if (!File.Exists(updaterPath))
                throw new Exception($"ملف التحديث (Updater.bat) غير موجود في مسار: {updaterPath}");

            if (!File.Exists(zipPath))
                throw new Exception($"ملف التحديث المحمّل غير موجود في مسار: {zipPath}");

            // التحقق من سلامة أرشيف التحديث المضغوط قبل التنفيذ (SEC-0003)
            try
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
                if (archive.Entries.Count == 0)
                    throw new InvalidDataException("أرشيف التحديث فارغ.");
            }
            catch (Exception ex)
            {
                throw new Exception($"ملف التحديث المحمّل تالف أو غير صالح: {ex.Message}");
            }

            // تشغيل سكربت التحديث من مجلد مؤقت (Temp) لتجنب أخطاء قفل الملفات (File Lock)
            // إذا قام المستخدم بضغط Updater.bat داخل التحديث، فإن محاولة استبداله أثناء تشغيله ستسبب فشل العملية
            string tempUpdaterPath = Path.Combine(Path.GetTempPath(), "POS_Updater.bat");
            File.Copy(updaterPath, tempUpdaterPath, true);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{tempUpdaterPath}\" \"{zipPath}\" \"{appDir}\"\"",
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = appDir
            };

            Process.Start(startInfo);

            // إغلاق التطبيق الحالي
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }

        /// <summary>
        /// يقارن إصدارين ويرجع true إذا كان الإصدار الجديد أحدث
        /// </summary>
        private bool IsNewerVersion(string remoteVersion, string localVersion)
        {
            try
            {
                var remote = new Version(remoteVersion);
                var local = new Version(localVersion);
                return remote > local;
            }
            catch
            {
                // في حالة خطأ في صيغة الإصدار، نقارن كنص
                return string.Compare(remoteVersion, localVersion, StringComparison.Ordinal) > 0;
            }
        }

        /// <summary>
        /// تحديث ملف version.json المحلي بالإصدار الجديد
        /// </summary>
        public void UpdateLocalVersion(string newVersion)
        {
            try
            {
                var versionInfo = new LocalVersionInfo
                {
                    Version = newVersion,
                    BuildDate = DateTime.Now.ToString("yyyy-MM-dd")
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(versionInfo, options);
                File.WriteAllText(LOCAL_VERSION_FILE, json);
                
                // Write to a temporary file that Updater.bat will copy over after extracting the zip
                // This prevents the old version.json inside the zip from overwriting the new version
                string appDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string newVersionFile = Path.Combine(appDir, "version.json.new");
                File.WriteAllText(newVersionFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating local version file: {ex.Message}");
            }
        }
    }
}
