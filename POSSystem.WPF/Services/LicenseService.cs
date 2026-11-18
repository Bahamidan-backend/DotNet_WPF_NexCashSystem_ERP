using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace POSSystem.WPF.Services
{
    public class LicenseService
    {
        private const string SecretKey = "NexCash_Secure_License_Key_Change_This_To_Something_Random"; 
        private const string LicenseFileName = "license.lic";

        public string GetMachineId()
        {
            try
            {
                string rawId = $"{Environment.MachineName}-{Environment.ProcessorCount}-{Environment.UserName}";
                return ComputeHash(rawId); // Returns Base64
            }
            catch
            {
                return "UNKNOWN-MACHINE-ID";
            }
        }

        public bool ValidateLicense(string licenseKey, out string message)
        {
            if (IsDebuggerAttached())
            {
                message = "تم اكتشاف محاولة تصحيح (Debugger Detect). يمنع تشغيل البرنامج.";
                return false;
            }

            if (IsTimeTampered())
            {
                message = "تم اكتشاف تلاعب في تاريخ النظام. يرجى ضبط الوقت والتاريخ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                message = "مفتاح الترخيص فارغ.";
                return false;
            }

            try
            {
                var parts = licenseKey.Split('|');
                if (parts.Length != 3)
                {
                    message = "صيغة مفتاح الترخيص غير صحيحة.";
                    return false;
                }

                string expirationDateStr = parts[0];
                string machineId = parts[1];
                string signature = parts[2];

                string currentMachineId = GetMachineId();
                if (machineId != currentMachineId)
                {
                    message = "هذا الترخيص غير مخصص لهذا الجهاز.";
                    return false;
                }

                string dataToSign = $"{expirationDateStr}|{machineId}";
                string expectedSignature = ComputeSignature(dataToSign);

                if (signature != expectedSignature)
                {
                    message = "توقيع الترخيص غير صالح. (تم التلاعب بالمفتاح)";
                    return false;
                }

                string[] dateFormats = { "yyyy-MM-dd", "yyyy/MM/dd" };
                
                DateTime expirationDate = DateTime.MinValue;
                bool parsed = false;
                
                var hijriCulture = new CultureInfo("ar-SA");
                hijriCulture.DateTimeFormat.Calendar = new HijriCalendar();
                bool isHijriYear = expirationDateStr.Length >= 4 &&
                                   int.TryParse(expirationDateStr.AsSpan(0, 4), out int yrPart) &&
                                   yrPart >= 1350 && yrPart <= 1600;

                foreach (var format in dateFormats)
                {
                    if (!isHijriYear && DateTime.TryParseExact(expirationDateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out expirationDate))
                    {
                        parsed = true;
                        break;
                    }
                    if (DateTime.TryParseExact(expirationDateStr, format, hijriCulture, DateTimeStyles.None, out expirationDate))
                    {
                        parsed = true;
                        break;
                    }
                    if (DateTime.TryParseExact(expirationDateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out expirationDate))
                    {
                        parsed = true;
                        break;
                    }
                }

                if (!parsed)
                {
                     message = "خطأ في قراءة تاريخ الانتهاء. الصيغة المطلوبة: yyyy-MM-dd";
                     return false;
                }

                if (DateTime.Now.Date > expirationDate.Date)
                {
                    message = "انتهت فترة صلاحية هذا الترخيص.";
                    return false;
                }

                message = "الترخيص صالح.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"خطأ غير متوقع: {ex.Message}";
                return false;
            }
        }

        public int GetDaysRemaining(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey)) return -1;

            try
            {
                var parts = licenseKey.Split('|');
                if (parts.Length != 3) return -1;

                string expirationDateStr = parts[0];
                string[] dateFormats = { "yyyy-MM-dd", "yyyy/MM/dd" };

                DateTime expirationDate = DateTime.MinValue;
                bool parsed = false;

                var hijriCulture = new CultureInfo("ar-SA");
                hijriCulture.DateTimeFormat.Calendar = new HijriCalendar();
                bool isHijriYear = expirationDateStr.Length >= 4 &&
                                   int.TryParse(expirationDateStr.AsSpan(0, 4), out int yrPart) &&
                                   yrPart >= 1350 && yrPart <= 1600;

                foreach (var format in dateFormats)
                {
                    if (!isHijriYear && DateTime.TryParseExact(expirationDateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out expirationDate))
                    {
                        parsed = true;
                        break;
                    }
                    if (DateTime.TryParseExact(expirationDateStr, format, hijriCulture, DateTimeStyles.None, out expirationDate))
                    {
                        parsed = true;
                        break;
                    }
                    if (DateTime.TryParseExact(expirationDateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out expirationDate))
                    {
                        parsed = true;
                        break;
                    }
                }

                if (!parsed) return -1;

                return (expirationDate.Date - DateTime.Now.Date).Days;
            }
            catch
            {
                return -1;
            }
        }

        public void SaveLicense(string licenseKey)
        {
            File.WriteAllText(LicenseFileName, licenseKey);
        }

        public string LoadLicense()
        {
            if (File.Exists(LicenseFileName))
            {
                return File.ReadAllText(LicenseFileName);
            }
            return null;
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }
        
        private byte[] GetSigningKey()
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(SecretKey));
            }
        }

        private string ComputeSignature(string data)
        {
            // Use the hashed key (derived from SecretKey string)
            using (var hmac = new HMACSHA256(GetSigningKey()))
            {
                var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(bytes);
            }
        }

        // --- ANTI-DEBUG & ANTI-TAMPER CHECKS ---

        private bool IsDebuggerAttached()
        {
            // 1. Managed Debugger
            if (Debugger.IsAttached) return true;

            // 2. Check for common debug environment variables (basic check)
            if (Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING") == "1") return true;

            return false;
        }

        private bool IsTimeTampered()
        {
            
            if (File.Exists(LicenseFileName))
            {
                try 
                {
                    DateTime fileTime = File.GetLastWriteTime(LicenseFileName);
                    if (fileTime > DateTime.Now.AddDays(1)) // Allow small discrepancy (1 day seems safe for timezones, but usually should be tighter)
                    {
                        return true;
                    }
                }
                catch { } 
            }
            return false;
        }
    }
}
