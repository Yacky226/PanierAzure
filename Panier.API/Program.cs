using StackExchange.Redis;
using Panier.API.Services;
using Panier.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Redis - tentative de connexion
var redisConnStr = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
ConfigurationOptions cfg = ConfigurationOptions.Parse(redisConnStr);
cfg.AbortOnConnectFail = false;
cfg.ConnectTimeout = 10000;
cfg.SyncTimeout = 10000;
cfg.KeepAlive = 180;

IConnectionMultiplexer? mux = null;
const int maxAttempts = 10;
for (int attempt = 1; attempt <= maxAttempts; attempt++)
{
    try
    {
        mux = await ConnectionMultiplexer.ConnectAsync(cfg);
        if (mux.IsConnected)
        {
            Console.WriteLine($"Redis connecté ({redisConnStr})");
            break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tentative {attempt}/{maxAttempts} - connexion Redis échouée : {ex.Message}");
    }
    await Task.Delay(2000);
}

if (mux == null)
{
    throw new Exception($"Impossible de se connecter à Redis après {maxAttempts} tentatives (target: {redisConnStr}). Vérifie docker-compose / service Redis.");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
builder.Services.AddScoped<IPanierService, RedisPanierService>();

var app = builder.Build();

// Route de test par défaut
app.MapGet("/", () => Results.Ok(new
{
    message = "API Panier fonctionne correctement",
    status = "OK",
    timestamp = DateTime.UtcNow,
    redis = "Connected"
}));

// Mapper les endpoints du panier
app.MapPanierEndpoints();

app.Run();