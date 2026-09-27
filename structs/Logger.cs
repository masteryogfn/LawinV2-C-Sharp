namespace RadiumServer.Structs;

public static class Logger
{
    private static readonly object SyncRoot = new();

    public static void Backend(params object[] args)
    {
        Log("\u001b[32mBACKEND\u001b[0m", string.Join(" ", args));
    }

    public static void Bot(params object[] args)
    {
        Log("\u001b[33mBOT\u001b[0m", string.Join(" ", args));
    }

    public static void Xmpp(params object[] args)
    {
        Log("\u001b[34mXMPP\u001b[0m", string.Join(" ", args));
    }

    public static void Error(params object[] args)
    {
        Log("\u001b[31mERROR\u001b[0m", string.Join(" ", args));
    }

    private static void Log(string prefix, string message)
    {
        lock (SyncRoot)
        {
            Console.WriteLine($"{prefix}: {message}");
        }
    }
}
