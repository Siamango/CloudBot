using BetterHaveIt.Repositories;
using CloudBot;
using CloudBot.Services;
using CloudBot.Statics;
using System.Reflection;

void OnShutdown(IServiceProvider services)
{
    var preferencesRepo = services.GetService<IRepository<WhitelistPreferencesModel>>();
    if (preferencesRepo is not null)
    {
        preferencesRepo.Save();
    }
}

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == null) Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((config) =>
        config.SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets(Assembly.GetEntryAssembly(), true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
        .AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        services.AddSlashCommandsModules();
        services.AddDiscordClientEventHandlers();
        services.AddSingleton<ICoreRunner, BotCoreRunner>();
        services.AddSingleton<IRepository<WhitelistPreferencesModel>>(new RepositoryJson<WhitelistPreferencesModel>(context.Configuration.GetValue<string>("Paths:PreferencesPath")));
    })
    .Build();

var applicationLifetime = host.Services.GetService<IHostApplicationLifetime>();
if (applicationLifetime != null)
{
    applicationLifetime.ApplicationStopping.Register(() => OnShutdown(host.Services));
}

var core = host.Services.GetService<ICoreRunner>();

if (core != null)
{
    await core.RunAsync();
}