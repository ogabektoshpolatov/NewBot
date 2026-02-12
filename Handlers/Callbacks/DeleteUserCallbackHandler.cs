using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class DeleteUserCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "removeUser";
        // task:1:deleteUser:256578
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        
        var taskUsers = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .OrderBy(tu => tu.QueuePosition) 
            .Include(tu => tu.User)
            .ToListAsync(cancellationToken);
        
        var users  = taskUsers.Select(tu => tu.User).ToList();
        
        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text:
            $"""
             📌 *{task.Name}*
             👥 Userlar soni: {taskUsers.Count()}
             ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
             
             ! Ochirmoqchi bo`lgan foydalanavuvchini tanlang.
             """,
            replyMarkup: BotKeyboards.ViewUserList(taskId, "removeUser", users),
            cancellationToken: cancellationToken
        );
    }
}

public class DeleteUserConfirmCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "removeUser" && parts[4] == "confirm";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        var userId = long.Parse(callbackQuery!.Data!.Split(':')[3]);
        
        var taskUser = await dbContext.TaskUsers
            .FirstOrDefaultAsync(tu => tu.TaskId == taskId && tu.UserId == userId && tu.IsActive, cancellationToken);

        if (taskUser != null)
        {
            if (taskUser.IsCurrent)
            {
                var queuePosition = taskUser.QueuePosition;
                var nextTaskUser = await dbContext.TaskUsers.Where(tu => tu.QueuePosition > queuePosition)
                    .OrderBy(tu => tu.QueuePosition)
                    .FirstOrDefaultAsync(cancellationToken);

                if (nextTaskUser != null)
                {
                    nextTaskUser.IsCurrent = true;
                    nextTaskUser.UserQueueTime = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    if (await dbContext.TaskUsers.AnyAsync(tu => tu.TaskId == taskId, cancellationToken))
                    {
                        var firstTaskUser = await dbContext.TaskUsers.FirstOrDefaultAsync(tu => tu.TaskId == taskId, cancellationToken);
                        firstTaskUser!.IsCurrent = true;
                        firstTaskUser!.UserQueueTime = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            dbContext.Remove(taskUser);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        var taskUsers = await dbContext.Users
            .Where(u => dbContext.TaskUsers
                .Any(tu => tu.TaskId == taskId && tu.UserId == u.UserId && tu.IsActive))
            .ToListAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            text:
            $"""
             📌 *{task.Name}*
             👥 Userlar soni: {taskUsers.Count}
             ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}

             ✅ Foydalanuvchi o‘chirildi.
             """,
            cancellationToken: cancellationToken
        );
        
        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text:
            $"""
             📌 *{task.Name}*
             👥 Userlar soni: {taskUsers.Count()}
             ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}

             ! O`chirmoqchi bo`lgan foydalanavuvchini tanlang.
             """,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.ViewUserList(taskId, "removeUser", taskUsers),
            cancellationToken: cancellationToken
        );
    }
}