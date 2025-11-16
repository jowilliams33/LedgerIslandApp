namespace LedgerIslandApp.Services;

public class FileUploadService 
{
    public Task<(string FileName, long Size)> GetInfoAsync(Stream stream, string fileName)
        => Task.FromResult((fileName, stream.Length));
}
