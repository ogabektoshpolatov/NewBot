using bot.Data;
using bot.Handlers;
using bot.Handlers.Callbacks;
using bot.Handlers.StateHandlers;
using bot.Handlers.TaskCommands;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddHostedService<NotificationSchedulerService>();
builder.Services.AddHostedService<PendingConfirmationChecker>();

builder.Services.AddScoped<TaskUiRenderer>();
builder.Services.AddScoped<QueueManagementService>();

builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<TaskNotificationService>();
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var token = configuration["TelegramBot:Token"];
    return new TelegramBotClient(token ?? throw new NullReferenceException("TelegramBot:Token"));
});

builder.Services.AddScoped<ICommandHandler, StartCommandHandler>();
builder.Services.AddScoped<ICommandHandler, UsersCommandHandler>();
builder.Services.AddScoped<ICommandHandler, CreateTaskCommandHandler>();
builder.Services.AddScoped<ICommandHandler, GetTasksCommandHandler>();
builder.Services.AddScoped<ICommandHandler, GetGroupIdCommandHandler>();

builder.Services.AddScoped<IStateHandler, AwaitingTaskNameHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingGroupIdHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingAutoNotifyHandler>();

builder.Services.AddScoped<ICallbackHandler, TaskMenuCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AddUserCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AddUserToTaskConfirmCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, DeleteUserCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, DeleteUserConfirmCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, ViewTaskCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, SkipQueueCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, NotifyCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AssignUserToQueueHandler>();
builder.Services.AddScoped<ICallbackHandler, AssignUserToQueueConfirmHandler>();
builder.Services.AddScoped<ICallbackHandler, CompleteTaskCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AcceptQueueCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, RejectQueueCallbackHandler>();

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
if (builder.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("✅ Database migrated");
}
else if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/test-notification", async (TaskNotificationService notificationService) =>
{
    await notificationService.SendDailyNotifications();
    return Results.Ok(new { message = "Notification sent!", timestamp = DateTime.UtcNow });
});
app.Run();