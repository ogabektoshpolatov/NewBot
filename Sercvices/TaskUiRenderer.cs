using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Sercvices;

public class TaskUiRenderer(AppDbContext dbContext)
{
    public async Task RenderTaskWithUsersAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        int taskId,
        InlineKeyboardMarkup keyboard,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        var users = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Select(tu => tu.User.FirstName) 
            .ToListAsync(cancellationToken);

        var userCount = users.Count;
        
        var userListText = users.Any()
            ? string.Join("\n", users.Select((u, i) => $"{i + 1}. {u}"))
            : " ";
        
        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text:
            $"""
             📌 *{task.Name}*
             👥 Userlar soni: {userCount}
             ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}

             👤 Userlar:
             {userListText}
             """,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}