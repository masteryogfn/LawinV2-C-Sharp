namespace RadiumServer.Structs;

public class AppConfig
{
    public int Port { get; set; } = 8080;
    public string MongoDatabase { get; set; } = "mongodb://127.0.0.1/radiumdb";
    public bool EnableGlobalChat { get; set; } = false;
    public string MatchmakerIp { get; set; } = "127.0.0.1:80";
    public string GameServerIp { get; set; } = "127.0.0.1:7777";
    public string DiscordBotToken { get; set; } = string.Empty;
    public string[] Moderators { get; set; } = Array.Empty<string>();

    public static AppConfig Load()
    {
        DotNetEnv.Env.Load();

        var config = new AppConfig();

        if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
        {
            config.Port = port;
        }

        var mongo = Environment.GetEnvironmentVariable("MONGODB_DATABASE");
        if (!string.IsNullOrWhiteSpace(mongo))
        {
            config.MongoDatabase = mongo;
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_GLOBAL_CHAT"), out var chat))
        {
            config.EnableGlobalChat = chat;
        }

        var mmIp = Environment.GetEnvironmentVariable("MATCHMAKER_IP");
        if (!string.IsNullOrWhiteSpace(mmIp))
        {
            config.MatchmakerIp = mmIp;
        }

        var gsIp = Environment.GetEnvironmentVariable("GAMESERVER_IP");
        if (!string.IsNullOrWhiteSpace(gsIp))
        {
            config.GameServerIp = gsIp;
        }

        var botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (!string.IsNullOrWhiteSpace(botToken))
        {
            config.DiscordBotToken = botToken;
        }

        var mods = Environment.GetEnvironmentVariable("MODERATORS");
        if (!string.IsNullOrWhiteSpace(mods))
        {
            config.Moderators = mods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return config;
    }
}
