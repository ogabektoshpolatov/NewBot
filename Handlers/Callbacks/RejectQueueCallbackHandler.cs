using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class RejectQueueCallbackHandler(
    ILogger<RejectQueueCallbackHandler> logger, 
    QueueManagementService queueService) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("reject_queue:");

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        // reject_queue:{taskId}:{allowedUserId}
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

        logger.LogInformation("User {UserId} rejected queue for task {TaskId}", pressedUserId, taskId);

        await queueService.RejectQueueAsync(taskId, pressedUserId, ct);

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "❌ Navbatchilik rad etildi.",
            cancellationToken: ct);

        // Tugmalarni o'chiramiz
        await botClient.EditMessageReplyMarkup(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: null,
            cancellationToken: ct);
    }
}