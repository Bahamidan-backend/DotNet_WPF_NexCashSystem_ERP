using System.Diagnostics;
class P { 
    static void Main() { 
        var psi = new ProcessStartInfo { 
            FileName = @"C:\Users\AlShaheen\Desktop\nexSys\Updater.bat", 
            Arguments = "\"A\" \"B\"", 
            UseShellExecute = true 
        }; 
        Process.Start(psi); 
    } 
}
