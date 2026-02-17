namespace bot.Sercvices;

public class PendingConfirmationChecker(
    ILogger<PendingConfirmationChecker> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🕐 PendingConfirmationChecker boshlandi!");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<QueueManagementService>();
                await queueService.AutoAcceptPendingQueuesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ PendingConfirmationChecker xatolik");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}