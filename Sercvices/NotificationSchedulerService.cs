namespace bot.Sercvices;

public class NotificationSchedulerService(
    ILogger<NotificationSchedulerService> logger, 
    TaskNotificationService notificationService) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    
    private readonly TimeSpan _morningTime = new(10, 50, 0);   // 09:00
    private readonly TimeSpan _eveningTime = new(15, 50, 0);  // 18:00

    private DateTime _lastMorningRun = DateTime.MinValue;
    private DateTime _lastEveningRun = DateTime.MinValue;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🕐 NotificationScheduler boshlandi!");

        while (!stoppingToken.IsCancellationRequested)
        {   
            try
            {
                logger.LogInformation("TaskScheduler boshlandi!");
                logger.LogInformation("Morning time " + _morningTime);
                logger.LogInformation("Evening time " + _eveningTime);
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
        logger.LogInformation("CheckAndSendNotifications method ichiga kirdi");
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;
        var today = now.Date;

        // if (currentTime >= _morningTime && 
        //     currentTime < _morningTime.Add(TimeSpan.FromMinutes(2)) &&
        //     _lastMorningRun.Date < today)
        // {
        //     logger.LogInformation("🌅 Ertalabki notification jo'natilmoqda...");
        //     await notificationService.SendDailyNotifications();
        //     _lastMorningRun = now;
        // }

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