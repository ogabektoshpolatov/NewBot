using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class CompleteTaskCallbackHandler(
    ILogger<CompleteTaskCallbackHandler> logger,
    QueueManagementService queueService) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("complete_task:");

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var parts = callbackQuery.Data!.Split(':');
        var taskId = int.Parse(parts[1]);
        var allowedUserId = long.Parse(parts[2]);
        var pressedUserId = callbackQuery.From.Id;
        
        if (pressedUserId != allowedUserId)
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "⛔️ Siz joriy navbatchi emassiz!",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        logger.LogInformation("User {UserId} completed task {TaskId}", pressedUserId, taskId);

        await queueService.CompleteCurrentTaskAsync(taskId, pressedUserId, ct);

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "✅ Bajarildi! Keyingi navbatchiga xabar yuborildi.",
            cancellationToken: ct);
        
        await botClient.EditMessageReplyMarkup(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: null,
            cancellationToken: ct);
    }
}