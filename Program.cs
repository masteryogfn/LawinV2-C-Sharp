using System.Net.Sockets;
using System.Threading.RateLimiting;
using RadiumServer.Structs;
using RadiumServer.TokenManager;
using MongoDB.Bson;
using MongoDB.Driver;

if (!Directory.Exists("./ClientSettings"))
{
    Directory.CreateDirectory("./ClientSettings");
}

Globals.JwtSecret = Functions.MakeId();

var config = AppConfig.Load();

TokenManager.Initialize();

_ = Task.Run(async () =>
{
    try
    {
        var settings = MongoClientSettings.FromConnectionString(config.MongoDatabase);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        var client = new MongoClient(settings);
        var dbName = MongoUrl.Create(config.MongoDatabase).DatabaseName ?? "radiumdb";
        var db = client.GetDatabase(dbName);
        await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        Logger.Backend("App successfully connected to MongoDB!");
    }
    catch
    {
        Logger.Error("MongoDB failed to connect, please make sure you have MongoDB installed and running.");
    }
});

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(config.Port);
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "default";
        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 45,
                Window = TimeSpan.FromSeconds(30),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
    {
        await EpicError.WriteErrorAsync(
            context,
            "errors.com.epicgames.common.not_found",
            "Sorry the resource you were trying to find could not be found",
            null,
            1004,
            null,
            StatusCodes.Status404NotFound
        );
    }
});

try
{
    await app.StartAsync();
    Logger.Backend($"App started listening on port {config.Port}");
    await app.WaitForShutdownAsync();
}
catch (Exception ex) when (IsAddressInUse(ex))
{
    Logger.Error($"Port {config.Port} is already in use!\nClosing in 3 seconds...");
    await Functions.Sleep(3000);
    Environment.Exit(0);
}

static bool IsAddressInUse(Exception ex)
{
    if (ex is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse })
        return true;
    if (ex is IOException && ex.InnerException is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse })
        return true;
    return false;
}
