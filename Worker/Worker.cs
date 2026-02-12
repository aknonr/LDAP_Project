using Microsoft.Extensions.Options;
using Worker.Configuration;

namespace Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IOptions<WorkerRoleOptions> _workerRoles;

    public Worker(ILogger<Worker> logger, IOptions<WorkerRoleOptions> workerRoles)
    {
        _logger = logger;
        _workerRoles = workerRoles;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var roles = _workerRoles.Value;
        _logger.LogInformation(
            "Worker basladi. Discovery={Discovery}, Update={Update}, Verify={Verify}, InventorySync={InventorySync}",
            roles.EnableDiscovery,
            roles.EnableUpdate,
            roles.EnableVerify,
            roles.EnableInventorySync);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Worker heartbeat at {UtcNow}", DateTimeOffset.UtcNow);
        }
    }
}
