using BetterHaveIt.Repositories;
using CloudBot;
using CloudBot.Services;
using Discord;
using Discord.Commands;
using System.Reflection;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((config) =>
        config.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true, true)
        .AddUserSecrets(Assembly.GetEntryAssembly(), true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
        .AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ICoreRunner, BotCoreRunner>();
        services.AddSingleton<IRepository<Preferences>>(new RepositoryJson<Preferences>(context.Configuration.GetValue<string>("Paths:PreferencesPath")));
        services.AddScoped<IMessageDispatchMiddleware, WhitelistMessageDispatchMiddleware>();
        services.AddSingleton(new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false
        }));
    })
    .Build();

var core = host.Services.GetService<ICoreRunner>();

if (core != null)
{
    await core.RunAsync();
}