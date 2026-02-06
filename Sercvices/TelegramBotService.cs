using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace bot.Sercvices;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        var token = configuration["TelegramBot:Token"];
        Console.WriteLine("token");
        _botClient = new TelegramBotClient(token ?? throw new NullReferenceException(nameof(token)));
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _botClient.DeleteWebhook(dropPendingUpdates:true, stoppingToken);
        _botClient.StartReceiving(
            updateHandler : HandleUpdateAsync,
            errorHandler : HandleErrorAsync,
            receiverOptions : new ReceiverOptions {},
            cancellationToken: stoppingToken);
        
        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation($"Bot @{me.Username} started!");
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        var chatId = message.Chat.Id;

        _logger.LogInformation($"Received message: {messageText}");
        
        if (messageText.ToLower() == "/start")
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Message",
                cancellationToken: cancellationToken
            );
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred");
        return Task.CompletedTask;
    }
}