using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers;

public class StartCommandHandler(ILogger<StartCommandHandler> logger, AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/start";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation($"User {message.From?.Username} started bot");
        
        var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == message.Chat.Id, cancellationToken);
        if (dbUser is null)
        {
            var user = new Entities.User()
            {
                UserId = message.Chat.Id,
                Username = message.From?.Username,
                FirstName = message.From?.FirstName,
                IsActive = true,
            };
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        Console.WriteLine($"User {dbUser.UserId}");

        await botClient.SetMyCommands(new[]
        {
            new BotCommand { Command = "start", Description = "Open main menu" },
        }, scope:null, languageCode:null, cancellationToken: cancellationToken);
        // var keyboard = new InlineKeyboardMarkup(new[]
        // {
        //     new[]
        //     {
        //       InlineKeyboardButton.WithCallbackData("\u2b05\ufe0f Left", "button_left"),
        //       InlineKeyboardButton.WithCallbackData("\u2b05\ufe0f Right", "button_right"),
        //     },
        //     new[]
        //     {
        //         InlineKeyboardButton.WithCallbackData("ℹ️ Info", "button_info")
        //     }
        // });

        await botClient.SendMessage(
            chatId:message.Chat.Id,
            text:"\ud83d\ude0a Salom! Botga xush kelibsiz!",
            cancellationToken:cancellationToken);
    }
}