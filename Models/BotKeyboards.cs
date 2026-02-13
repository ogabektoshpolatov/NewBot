using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Models;

public class BotKeyboards
{
    public static InlineKeyboardMarkup TaskMenu(int taskId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("👥 Navbatchilikni ko`rish", CB.ViewUsers(taskId)) },
        new[] { InlineKeyboardButton.WithCallbackData("➕ User qo'shish",     CB.AddUser(taskId)) },
        new[] { InlineKeyboardButton.WithCallbackData("➖ User o'chirish",    CB.RemoveUser(taskId)) },
    });
    
    public static InlineKeyboardMarkup BackToTask(int taskId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("🔙 Orqaga", CB.Task(taskId)) }
    });
    
    public static InlineKeyboardMarkup ViewUserList(int taskId, string order, List<Entities.User> users)
    {
        var buttons = users
            .Select(u =>
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"👤 {u.FirstName}",
                        $"task:{taskId}:{order}:{u.UserId}:confirm")
                })
            .ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅ Back", $"task:{taskId}")
        });

        return new InlineKeyboardMarkup(buttons);
    }
    
    public static InlineKeyboardMarkup QueueHandleMenu(int taskId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("Navbatni surish",CB.SkipQueue(taskId)) },
        new[] { InlineKeyboardButton.WithCallbackData("⬅ Orqaga",CB.Task(taskId)) },
    });
}