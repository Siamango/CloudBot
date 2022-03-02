using BetterHaveIt.Repositories;
using CloudBot.Services;
using CloudBot.Settings;
using CloudBot.Statics;
using SolmangoNET.Rpc;
using Solnet.Rpc;

void OnShutdown(IServiceProvider services)
{
    var repo = services.GetService<IRepository<List<string>>>();
    if (repo is not null)
    {
        repo.Save();
    }
}

WebApplication CreateWebApplication()
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSlashCommandsModules();
    builder.Services.AddDiscordClientEventHandlers();
    builder.Services.AddSingleton<ICoreRunner, BotCoreRunner>();

    builder.Services.Configure<ConnectionSettings>(builder.Configuration.GetSection(ConnectionSettings.POSITION));
    builder.Services.Configure<PathsSettings>(builder.Configuration.GetSection(PathsSettings.POSITION));

    builder.Services.AddSingleton<IRepository<List<string>>>((services) =>
    {
        var pathsSettings = builder.Configuration.GetSection(PathsSettings.POSITION).Get<PathsSettings>();
        return new RepositoryJson<List<string>>(pathsSettings.Get("AddressesList")!.CompletePath);
    });
    builder.Services.AddSingleton<IRpcScheduler>((services) =>
    {
        var scheduler = new BasicRpcScheduler(100);
        scheduler.Start();
        return scheduler;
    });
    builder.Services.AddSingleton<IRpcClient>((services) =>
    {
        var connectionSettings = builder.Configuration.GetSection(ConnectionSettings.POSITION).Get<ConnectionSettings>();
        return ClientFactory.GetClient(connectionSettings.SolanaEndpoint);
    });

    var app = builder.Build();
    var applicationLifetime = app.Services.GetService<IHostApplicationLifetime>();
    if (applicationLifetime != null)
    {
        applicationLifetime.ApplicationStopping.Register(() => OnShutdown(app.Services));
    }
    return app;
}

var app = CreateWebApplication();
var core = app.Services.GetService<ICoreRunner>();
if (core != null)
{
    await core.RunAsync();
}