using System;
using System.IO;
using System.Text.Json;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.Services
{
    public class SettingsService
    {
        private const string SettingsFileName = "settings.json";

        public static event Action OnSettingsChanged;

        public static void SaveSettings(SettingsProberty settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFileName, jsonString);
                OnSettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save settings: {ex.Message}");
            }
        }

        public static SettingsProberty LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    string jsonString = File.ReadAllText(SettingsFileName);
                    var settings = JsonSerializer.Deserialize<SettingsProberty>(jsonString);
                    if (settings != null)
                    {
                        // Migrate legacy untouched template (SAR + 15% tax) to default 0% tax and YER
                        if (settings.Currency == "SAR" && settings.TaxRate == 15.0m)
                        {
                            settings.TaxRate = 0.0m;
                            settings.Currency = "YER";
                            try
                            {
                                var options = new JsonSerializerOptions { WriteIndented = true };
                                File.WriteAllText(SettingsFileName, JsonSerializer.Serialize(settings, options));
                            }
                            catch { }
                        }
                        return settings;
                    }
                }
            }
            catch (Exception)
            {
                // Log error or ignore to use defaults
            }

            return new SettingsProberty();
        }
    }
}
