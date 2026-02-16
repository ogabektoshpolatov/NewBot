namespace bot.Sercvices;

public class NotificationSchedulerService(
    ILogger<NotificationSchedulerService> logger, 
    TaskNotificationService notificationService) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _morningTime = new(18, 12, 0);   // 09:00
    private readonly TimeSpan _eveningTime = new(23, 12, 0);  // 18:00

    private DateTime _lastMorningRun = DateTime.MinValue;
    private DateTime _lastEveningRun = DateTime.MinValue;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🕐 NotificationScheduler boshlandi!");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendNotifications();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Notification scheduler da xatolik");
            }
            
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
    
    private async Task CheckAndSendNotifications()
    {
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;
        var today = now.Date;

        if (currentTime >= _morningTime && 
            currentTime < _morningTime.Add(TimeSpan.FromMinutes(2)) &&
            _lastMorningRun.Date < today)
        {
            logger.LogInformation("🌅 Ertalabki notification jo'natilmoqda...");
            await notificationService.SendDailyNotifications();
            _lastMorningRun = now;
        }

        if (currentTime >= _eveningTime && 
            currentTime < _eveningTime.Add(TimeSpan.FromMinutes(2)) &&
            _lastEveningRun.Date < today)
        {
            logger.LogInformation("🌆 Kechki notification jo'natilmoqda...");
            await notificationService.SendDailyNotifications();
            _lastEveningRun = now;
        }
    }
}