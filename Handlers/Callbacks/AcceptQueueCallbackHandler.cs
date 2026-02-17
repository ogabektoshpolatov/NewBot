using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class AcceptQueueCallbackHandler(
    ILogger<AcceptQueueCallbackHandler> logger,
    QueueManagementService queueService) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("accept_queue:");

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        // accept_queue:{taskId}:{allowedUserId}
        var parts = callbackQuery.Data!.Split(':');
        var taskId = int.Parse(parts[1]);
        var allowedUserId = long.Parse(parts[2]);
        var pressedUserId = callbackQuery.From.Id;

        // ✅ Faqat taklif yuborilgan user bosa oladi
        if (pressedUserId != allowedUserId)
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "⛔️ Bu taklif siz uchun emas!",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        logger.LogInformation("User {UserId} accepted queue for task {TaskId}", pressedUserId, taskId);

        await queueService.AcceptQueueAsync(taskId, pressedUserId, ct);

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "✅ Navbatchilikni qabul qildingiz!",
            cancellationToken: ct);

        // Tugmalarni o'chiramiz
        await botClient.EditMessageReplyMarkup(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: null,
            cancellationToken: ct);
    }
}