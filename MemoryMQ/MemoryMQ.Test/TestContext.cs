using System.Data.SQLite;

namespace MemoryMQ.Test;

public class TestContext:IDisposable
{
    public void Dispose()
    {
        var dbFiles=Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.db", SearchOption.TopDirectoryOnly);
        foreach (var dbFile in dbFiles)
        {
            File.Delete(dbFile);
        }
    }
}