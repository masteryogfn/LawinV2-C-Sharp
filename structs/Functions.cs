namespace RadiumServer.Structs;

public static class Functions
{
    public static string MakeId()
    {
        return Guid.NewGuid().ToString();
    }

    public static Task Sleep(int milliseconds)
    {
        return Task.Delay(milliseconds);
    }
}
