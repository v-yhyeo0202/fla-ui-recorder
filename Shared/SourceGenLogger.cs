using System.IO;

namespace Shared;

public static class SourceGenLogger
{
    private static readonly object _lock = new();

    static SourceGenLogger()
    {
        File.WriteAllText(Global.sourceGenLogPath, "Source generator log\n");

        return;
    }

    public static void Log(string message)
    {
        lock (_lock)
        {
            File.AppendAllText(Global.sourceGenLogPath, $"{message}\n");
        }

        return;
    }
}