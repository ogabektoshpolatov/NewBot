using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace bot.Sercvices;

public class TaskNotificationService(
    ILogger<TaskNotificationService> logger, 
    IServiceProvider serviceProvider,
    ITelegramBotClient botClient)
{
    public async Task SendDailyNotifications()
    {
        logger.LogInformation("📢 Kunlik notification jo'natish boshlandi...");

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var tasks = await dbContext.Tasks
            .Where(t => t.IsActive && t.SendToGroup)
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ThenInclude(tu => tu.User)
            .ToListAsync();

        logger.LogInformation($"📊 {tasks.Count} ta task topildi");

        foreach (var task in tasks)
        {
            try
            {
                await SendTaskNotification(task, dbContext);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Task ID {task.Id} uchun xabar yuborishda xatolik");
            }
        }

        logger.LogInformation("✅ Kunlik notification tugadi");
    }
    
    private async Task SendTaskNotification(Entities.Task task, AppDbContext dbContext)
    {
        if (!task.TaskUsers.Any())
        {
            logger.LogWarning($"⚠️ Task '{task.Name}' (ID: {task.Id}) da userlar yo'q");
            return;
        }
        
        var allTaskUsers = task.TaskUsers
            .Where(tu => tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .ToList();
        
        var currentTaskUser = allTaskUsers.FirstOrDefault(tu => tu.IsCurrent);

        if (currentTaskUser == null)
        {
            logger.LogWarning($"⚠️ Task '{task.Name}' da joriy navbatchi topilmadi");
            return;
        }

        var currentUser = currentTaskUser.User;
        
        var userListText = string.Join("\n",
            allTaskUsers.Select((tu, index) =>
            {
                var user = tu.User;
                var position = index + 1;
                var userName = user.FirstName ?? user.Username ?? "User";

                if (tu.IsCurrent)
                {
                    var acceptedTime = tu.UserQueueTime.HasValue 
                        ? tu.UserQueueTime.Value.AddHours(5).ToString("dd.MM.yyyy HH:mm")
                        : "Noma'lum";
                    
                    return $"👉 <b>{position}. <a href=\"tg://user?id={user.UserId}\">{userName}</a></b> 🟢\n" +
                           $"    └ ⏰ <b>Qabul qilingan vaqt:</b> <code>{acceptedTime}</code>";
                }
                else
                {
                    return $"   {position}. {userName}";
                }
            }));

        var message = $"🔔 <b>NAVBATCHILIK BILDIRISH</b>\n\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n" +
                      $"📋 <b>Task:</b> {task.Name}\n" +
                      $"📅 <b>Sana:</b> {DateTime.UtcNow.AddHours(5):dd.MM.yyyy HH:mm}\n" +
                      $"👥 <b>Jami navbatchilar:</b> {allTaskUsers.Count}\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n\n" +
                      $"👤 <b>Joriy navbatchi:</b>\n" +
                      $"<a href=\"tg://user?id={currentUser.UserId}\">{currentUser.FirstName ?? "User"}</a>\n\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n" +
                      $"📋 <b>Navbatchilik ketma-ketligi:</b>\n\n" +
                      $"{userListText}\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n\n";

        await botClient.SendMessage(
            chatId: task.TelegramGroupId,
            text: message,
            parseMode: ParseMode.Html,
            replyMarkup:BotKeyboards.TaskCompletionButton(task.Id, currentUser.UserId));

        logger.LogInformation($"✅ Xabar yuborildi: Task '{task.Name}' → User {currentUser.FirstName}");
        
        // Navbatni yangilaymiz (ixtiyoriy)
        // await RotateQueue(task, dbContext);
    }

    private async Task RotateQueue(Entities.Task task, AppDbContext dbContext)
    {
        var activeUsers = task.TaskUsers
            .Where(tu => tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .ToList();

        if (activeUsers.Count <= 1) return;
        
        var firstUser = activeUsers[0];
        var maxPosition = activeUsers.Max(tu => tu.QueuePosition);

        firstUser.QueuePosition = maxPosition + 1;
        
        for (int i = 1; i < activeUsers.Count; i++)
        {
            activeUsers[i].QueuePosition--;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation($"🔄 Queue rotated for task '{task.Name}'");
    }
}