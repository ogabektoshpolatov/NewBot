using bot.Data;
using bot.Entities;
using bot.Models;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Task = System.Threading.Tasks.Task;

namespace bot.Handlers.Callbacks;

public class AddUserCallbackHandler(AppDbContext dbContext, TaskUiRenderer taskUiRenderer) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "addUser";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        
        var availableUsers = await dbContext.Users
            .Where(u => !dbContext.TaskUsers
                .Any(tu => tu.TaskId == taskId && tu.UserId == u.UserId && tu.IsActive))
            .ToListAsync(cancellationToken);
        
        await taskUiRenderer.RenderTaskWithUsersAsync(
            botClient,
            callbackQuery,
            taskId,
            BotKeyboards.ViewUserList(taskId, "addUser",availableUsers),
            cancellationToken);
    }
}

public class AddUserToTaskConfirmCallbackHandler(AppDbContext dbContext, TaskUiRenderer taskUiRenderer) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "addUser" && parts[4] == "confirm";
        //task:1:addUser:5592363193:confirm
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        var userId = long.Parse(callbackQuery!.Data!.Split(':')[3]);

        var maxPos = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId)
            .MaxAsync(tu => (int?)tu.QueuePosition, cancellationToken) ?? 0;

        dbContext.TaskUsers.Add(new Entities.TaskUser
        {
            TaskId        = taskId,
            UserId        = userId,
            QueuePosition = maxPos + 1,
            IsActive      = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var availableUsers = await dbContext.Users
            .Where(u => !dbContext.TaskUsers
                .Any(tu => tu.TaskId == taskId && tu.UserId == u.UserId && tu.IsActive))
            .ToListAsync(cancellationToken);
        
        await taskUiRenderer.RenderTaskWithUsersAsync(
            botClient,
            callbackQuery,
            taskId,
            BotKeyboards.ViewUserList(taskId, "addUser", availableUsers),
            cancellationToken);
    }
}