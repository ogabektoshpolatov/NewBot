using bot.Data;
using bot.Entities;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Task = System.Threading.Tasks.Task;

namespace bot.Sercvices;

public class QueueManagementService(
    ILogger<QueueManagementService> logger,
    ITelegramBotClient botClient,
    IServiceProvider serviceProvider)
{
    public async Task CompleteCurrentTaskAsync(int taskId, long currentUserId, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await dbContext.Tasks
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ThenInclude(tu => tu.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null) return;

        var currentTaskUser = task.TaskUsers
            .FirstOrDefault(tu => tu.IsCurrent && tu.UserId == currentUserId);

        if (currentTaskUser is null) return;

        var nextTaskUser = FindNextUser(task.TaskUsers.ToList(), currentTaskUser.QueuePosition);

        if (nextTaskUser is null)
        {
            logger.LogWarning("Task {TaskId} da keyingi user topilmadi", taskId);
            return;
        }

        nextTaskUser.IsPendingConfirmation = true;
        nextTaskUser.PendingConfirmationSince = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        
        await botClient.SendMessage(
            chatId: task.TelegramGroupId,
            text: $"🔔 <b>NAVBAT O'ZGARISHI</b>\n\n" +
                  $"📋 <b>Task:</b> {task.Name}\n\n" +
                  $"✅ <a href=\"tg://user?id={currentUserId}\">{currentTaskUser.User.FirstName ?? "User"}</a> navbatchilikni yakunladi!\n\n" +
                  $"➡️ Keyingi navbatchi:\n" +
                  $"<a href=\"tg://user?id={nextTaskUser.UserId}\">{nextTaskUser.User.FirstName ?? "User"}</a>, " +
                  $"navbatchilikni qabul qilasizmi?\n\n" +
                  $"⚠️ 24 soat ichida javob bermasangiz,\n" +
                  $"avtomatik qabul qilinadi.",
            parseMode: ParseMode.Html,
            replyMarkup: BotKeyboards.QueueConfirmationButtons(taskId, nextTaskUser.UserId),
            cancellationToken: ct);

        logger.LogInformation("✉️ Guruhga taklif yuborildi: {User}", nextTaskUser.User.FirstName);
    }
    
    private TaskUser? FindNextUser(List<TaskUser> taskUsers, int currentPosition)
    {
        var activeUsers = taskUsers
            .Where(tu => tu.IsActive && !tu.IsCurrent)
            .ToList();
        
        var nextUser = activeUsers
            .Where(tu => tu.QueuePosition > currentPosition)
            .OrderBy(tu => tu.QueuePosition)
            .FirstOrDefault();
        
        if (nextUser is null)
        {
            nextUser = activeUsers
                .OrderBy(tu => tu.QueuePosition)
                .FirstOrDefault();
        }

        return nextUser;
    }
    
    public async Task AcceptQueueAsync(int taskId, long userId, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await dbContext.Tasks
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ThenInclude(tu => tu.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null) return;

        var pendingUser = task.TaskUsers
            .FirstOrDefault(tu => tu.UserId == userId && tu.IsPendingConfirmation);

        if (pendingUser is null)
        {
            logger.LogWarning("User {UserId} pending confirmation emas", userId);
            return;
        }

        var oldCurrentUser = task.TaskUsers.FirstOrDefault(tu => tu.IsCurrent);
        if (oldCurrentUser is not null)
        {
            oldCurrentUser.IsCurrent = false;
            oldCurrentUser.UserQueueTime = null;
        }

        pendingUser.IsCurrent = true;
        pendingUser.IsPendingConfirmation = false;
        pendingUser.PendingConfirmationSince = null;
        pendingUser.UserQueueTime = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("✅ Queue qabul qilindi: {User}", pendingUser.User.FirstName);
        
        await SendGroupNotificationAsync(task, pendingUser, ct);
    }

    public async Task RejectQueueAsync(int taskId, long userId, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await dbContext.Tasks
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ThenInclude(tu => tu.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null) return;

        var pendingUser = task.TaskUsers
            .FirstOrDefault(tu => tu.UserId == userId && tu.IsPendingConfirmation);

        if (pendingUser is null)
        {
            logger.LogWarning("User {UserId} pending confirmation emas", userId);
            return;
        }
        
        var currentUser = task.TaskUsers.FirstOrDefault(tu => tu.IsCurrent);

        pendingUser.IsPendingConfirmation = false;
        pendingUser.PendingConfirmationSince = null;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("❌ Queue rad etildi: User {UserId}", userId);
        
        await botClient.SendMessage(
            chatId: task.TelegramGroupId,
            text: $"❌ <b>NAVBATCHILIK RAD ETILDI</b>\n\n" +
                  $"📋 <b>Task:</b> {task.Name}\n\n" +
                  $"<a href=\"tg://user?id={userId}\">{pendingUser.User.FirstName ?? "User"}</a> " +
                  $"navbatchilikni rad etdi.\n\n" +
                  $"⏳ Navbatchilik " +
                  $"<a href=\"tg://user?id={currentUser?.User.UserId}\">" +
                  $"{currentUser?.User.FirstName ?? "oldingi navbatchi"}</a> sizda qoldi",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
        
        if (currentUser is not null)
        {
            await botClient.SendMessage(
                chatId: currentUser.UserId,
                text: $"⚠️ <b>Ogohlantirish!</b>\n\n" +
                      $"📋 Task: <b>{task.Name}</b>\n\n" +
                      $"<a href=\"tg://user?id={userId}\">{pendingUser.User.FirstName ?? "User"}</a> " +
                      $"navbatchilikni rad etdi.\n\n" +
                      $"🔄 Navbatchilik hali sizda qolmoqda!",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
    }
    
    public async Task AutoAcceptPendingQueuesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var threshold = DateTime.UtcNow.AddHours(-24);

        var pendingUsers = await dbContext.TaskUsers
            .Where(tu => tu.IsPendingConfirmation && tu.PendingConfirmationSince <= threshold)
            .ToListAsync(ct);

        foreach (var pendingUser in pendingUsers)
        {
            logger.LogInformation("🕐 Avtomatik qabul: User {UserId}, Task {TaskId}", 
                pendingUser.UserId, pendingUser.TaskId);
            
            await AcceptQueueAsync(pendingUser.TaskId, pendingUser.UserId, ct);
        }
    }
    
    private async Task SendGroupNotificationAsync(Entities.Task task, TaskUser currentTaskUser, CancellationToken ct)
    {
        var allTaskUsers = task.TaskUsers
            .Where(tu => tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .ToList();

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
                        : "Hozir";

                    return $"👉 <b>{position}. <a href=\"tg://user?id={user.UserId}\">{userName}</a></b> 🟢\n" +
                           $"    └ ⏰ <b>Qabul qilgan vaqt:</b> <code>{acceptedTime}</code>";
                }
                return $"   {position}. {userName}";
            }));

        var message = $"🔄 <b>NAVBATCHILIK O'ZGARDI</b>\n\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n" +
                      $"📋 <b>Task:</b> {task.Name}\n" +
                      $"📅 <b>Sana:</b> {DateTime.UtcNow.AddHours(5):dd.MM.yyyy HH:mm}\n" +
                      $"👥 <b>Jami:</b> {allTaskUsers.Count} kishi\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n\n" +
                      $"⭐️ <b>YANGI NAVBATCHI:</b>\n" +
                      $"👤 <a href=\"tg://user?id={currentTaskUser.UserId}\">" +
                      $"{currentTaskUser.User.FirstName ?? "User"}</a>\n\n" +
                      $"━━━━━━━━━━━━━━━━━━━\n" +
                      $"📋 <b>Navbat ro'yxati:</b>\n\n" +
                      $"{userListText}\n\n" +
                      $"━━━━━━━━━━━━━━━━━━━";

        await botClient.SendMessage(
            chatId: task.TelegramGroupId,
            text: message,
            parseMode: ParseMode.Html,
            replyMarkup: BotKeyboards.TaskCompletionButton(task.Id, currentTaskUser.UserId),
            cancellationToken: ct);
    }
}