using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers.TaskCommands;

public class GetTasksCommandHandler(AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/mytasks";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var dbTasks = await dbContext.Tasks.Where(t => t.CreatedUserId == message.Chat.Id).ToListAsync(cancellationToken);
        
        if (!dbTasks.Any())
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Sizda hozircha tasklar mavjud emas ❌",
                cancellationToken: cancellationToken);
            return;
        }

        var buttons = dbTasks
            .Select(t => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: t.Name ?? "NoName",
                    callbackData: $"{t.Id}"
                    
                )
            })
            .ToList();

        var keyboard = new InlineKeyboardMarkup(buttons);
        
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "📋 My Tasks",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}