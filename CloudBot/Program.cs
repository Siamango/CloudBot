using BetterHaveIt.Repositories;
using CloudBot;
using CloudBot.Models;
using CloudBot.Services;
using CloudBot.Settings;
using CloudBot.Statics;
using SolmangoNET.Models;
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
    builder.Services.AddSingleton<IRepository<List<string>>>((_) => new RepositoryJson<List<string>>(builder.Configuration.GetSection(PathsSettings.Position).Get<PathsSettings>().Get("Whitelist")!.CompletePath));
    builder.Services.AddSingleton<IRepository<WhitelistPreferencesModel>>(_ => new RepositoryJson<WhitelistPreferencesModel>(builder.Configuration.GetSection(PathsSettings.Position).Get<PathsSettings>().Get("WhitelistPref")!.CompletePath));
    builder.Services.AddSingleton<IRepository<PreferencesModel>>(_ => new RepositoryJson<PreferencesModel>(builder.Configuration.GetSection(PathsSettings.Position).Get<PathsSettings>().Get("Prefs")!.CompletePath));
    builder.Services.AddSingleton<IRepository<List<RarityModel>>>(_ => new RepositoryJson<List<RarityModel>>(builder.Configuration.GetSection(PathsSettings.Position).Get<PathsSettings>().Get("Gen0Rarities")!.CompletePath));
    builder.Services.AddSingleton<IRepository<CandyMachineModel>>(_ => new RepositoryJson<CandyMachineModel>(builder.Configuration.GetSection(PathsSettings.Position).Get<PathsSettings>().Get("Gen0Cm")!.CompletePath));

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