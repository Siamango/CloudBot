using BetterHaveIt.Repositories;

namespace CloudBot.Services;

public class RepositorySerializer : IHostedService
{
    private readonly IRepository<List<string>> addressesRepo;
    private readonly ILogger<RepositorySerializer> logger;

    public RepositorySerializer(IRepository<List<string>> addressesRepo, ILogger<RepositorySerializer> logger)
    {
        this.addressesRepo = addressesRepo;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Serializing repositories");
        addressesRepo.Save();
        return Task.CompletedTask;
    }
}