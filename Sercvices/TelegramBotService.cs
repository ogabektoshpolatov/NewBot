using bot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Sercvices;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotService(
        IConfiguration configuration, 
        ILogger<TelegramBotService> logger, 
        IServiceProvider serviceProvider)
    {
        var token = configuration["TelegramBot:Token"];
        Console.WriteLine("token");
        _botClient = new TelegramBotClient(token ?? throw new NullReferenceException(nameof(token)));
        _logger = logger;
        _serviceProvider = serviceProvider;
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

        _logger.LogInformation($"Received '{messageText}' from {message.From?.Username}");
        
        using var scope = _serviceProvider.CreateScope();
        var commandHandlers = scope.ServiceProvider.GetServices<ICommandHandler>();
        
        var handler = commandHandlers.FirstOrDefault(h => 
            h.Command.Equals(messageText, StringComparison.OrdinalIgnoreCase));

        if (handler != null)
        {
            await handler.HandleAsync(botClient, message, cancellationToken);
        }
        else
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Tushunmadim. /help ni yozing.",
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