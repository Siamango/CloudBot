using BetterHaveIt.Repositories;
using CloudBot.Models;

namespace CloudBot.Services;

public class RepositorySerializer : IHostedService
{
    private readonly ILogger<RepositorySerializer> logger;
    private readonly IRepository<List<string>> addressesRepo;
    private readonly IRepository<PreferencesModel> preferencesRepo;
    private readonly IRepository<WhitelistPreferencesModel> wlPrefRepo;

    public RepositorySerializer(IRepository<List<string>> addressesRepo, ILogger<RepositorySerializer> logger, IRepository<PreferencesModel> preferencesRepo, IRepository<WhitelistPreferencesModel> wlPrefRepo)
    {
        this.addressesRepo = addressesRepo;
        this.logger = logger;
        this.preferencesRepo = preferencesRepo;
        this.wlPrefRepo = wlPrefRepo;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Serializing repositories");
        addressesRepo.Save();
        preferencesRepo.Save();
        wlPrefRepo.Save();
        return Task.CompletedTask;
    }
}