using BetterHaveIt.Repositories;
using CloudBot.Models;
using CloudBot.Services;
using CloudBot.Settings;
using CloudBot.Statics;
using SolmangoNET.Rpc;
using Solnet.Rpc;

WebApplication CreateWebApplication()
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSlashCommandsModules();
    builder.Services.AddDiscordClientEventHandlers();
    builder.Services.AddOfflineRunner<BotCoreRunner>();

    // Settings
    builder.Services.Configure<ConnectionSettings>(builder.Configuration.GetSection(ConnectionSettings.Position));
    builder.Services.Configure<PathsSettings>(builder.Configuration.GetSection(PathsSettings.Position));
    builder.Services.Configure<DebugSettings>(builder.Configuration.GetSection(DebugSettings.Position));

    // Repositories
    builder.Services.AddSingleton<IRepository<PreferencesModel>>(_ =>
        new RepositoryJson<PreferencesModel>(builder.Configuration.GetSection(PathsSettings.Position).Get<PathsSettings>().Get("Prefs").CompletePath));

    // Solana
    builder.Services.AddSingleton((_) => ClientFactory.GetClient(builder.Configuration.GetSection(ConnectionSettings.Position).Get<ConnectionSettings>().SolanaEndpoint));
    builder.Services.AddSingleton<IRpcScheduler>((_) =>
    {
        var scheduler = new BasicRpcScheduler(100);
        scheduler.Start();
        return scheduler;
    });

    var app = builder.Build();
    return app;
}

var app = CreateWebApplication();
await app.RunOfflineAsync(CancellationToken.None);