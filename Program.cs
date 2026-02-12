using bot.Data;
using bot.Handlers;
using bot.Handlers.Callbacks;
using bot.Handlers.StateHandlers;
using bot.Handlers.TaskCommands;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddScoped<TaskUiRenderer>();

builder.Services.AddScoped<ICommandHandler, StartCommandHandler>();
builder.Services.AddScoped<ICommandHandler, UsersCommandHandler>();
builder.Services.AddScoped<ICommandHandler, CreateTaskCommandHandler>();
builder.Services.AddScoped<ICommandHandler, GetTasksCommandHandler>();

builder.Services.AddScoped<IStateHandler, AwaitingTaskNameHandler>();

builder.Services.AddScoped<ICallbackHandler, TaskMenuCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AddUserCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AddUserToTaskConfirmCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, DeleteUserCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, DeleteUserConfirmCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, ViewTaskCallbackHandler>();

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
app.Run();