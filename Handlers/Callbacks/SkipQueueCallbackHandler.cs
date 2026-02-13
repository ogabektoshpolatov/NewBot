using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class SkipQueueCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "skipQueue";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        
        var taskUsers = await dbContext.TaskUsers.Where(x => x.TaskId == taskId).ToListAsync(cancellationToken);
        var currentQueueUser = taskUsers.FirstOrDefault(x => x.IsCurrent);

        if (currentQueueUser != null)
        {
            var queuePosition = currentQueueUser.QueuePosition;
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

            currentQueueUser.IsCurrent = false;
            currentQueueUser.UserQueueTime = null;
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}