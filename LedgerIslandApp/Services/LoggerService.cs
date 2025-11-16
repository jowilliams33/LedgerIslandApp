using System.Diagnostics;

namespace LedgerIslandApp.Services;

public class LoggerService 
{
    public void Info(string message) => Debug.WriteLine($"[INFO] {message}");
    public void Error(string message, Exception? ex = null)
        => Debug.WriteLine($"[ERROR] {message} {(ex is null ? "" : ex.ToString())}");
}
