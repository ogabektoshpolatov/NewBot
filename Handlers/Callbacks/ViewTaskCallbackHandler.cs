using System.Runtime.InteropServices.JavaScript;
using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers.Callbacks;

public class ViewTaskCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "viewUsers";
    }

    public async Task HandleAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);

        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        var taskUsers = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .Select(tu => new { tu.User.FirstName, tu.User.Username, tu.QueuePosition, tu.IsCurrent, tu.UserQueueTime })
            .ToListAsync(cancellationToken);

        var userCount = taskUsers.Count;
        
        var userListText = string.Join("\n",
            taskUsers.Select((u, i) =>
                u.IsCurrent
                    ? $"👉 *{i + 1}. {u.FirstName ?? u.Username}*  🟢\n" +
                      $"    └ ⏰ *Qabul qilingan vaqt:* `{DateTime.Parse(u.UserQueueTime.ToString() ?? "").AddHours(5):dd.MM.yyyy HH:mm}`"
                    : $"   {i + 1}. {u.FirstName ?? u.Username}"
            ));

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text:
            $"""
             📌 *{task.Name}*

             👥 *Navbatchilar soni:* `{userCount}`
             ⏰ *Yaratilgan vaqt:* `{task.ScheduleTime:dd.MM.yyyy HH:mm}`

             ───────────────────
             👤 *Navbatchilik ketma-ketligi:*

             {userListText}
             ───────────────────
             """,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.QueueHandleMenu(taskId),
            cancellationToken: cancellationToken
        );
    }
}