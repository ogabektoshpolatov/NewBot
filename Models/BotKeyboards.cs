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
        new[] { InlineKeyboardButton.WithCallbackData("👥 Navbatchilikni ko`rish", CB.ViewUsers(taskId)),InlineKeyboardButton.WithCallbackData("👥 Navbatchi belgilsh",    CB.AssignUserToQueue(taskId)) },
        new[] { InlineKeyboardButton.WithCallbackData("➕ User qo'shish",     CB.AddUser(taskId)), InlineKeyboardButton.WithCallbackData("➖ User o'chirish",    CB.RemoveUser(taskId))},
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
    
    public static InlineKeyboardMarkup TaskCompletionButton(int taskId, long userId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "✅ Navbatchilikni tugatdim",
                    $"complete_task:{taskId}:{userId}")  
            }
        });
    }

    public static InlineKeyboardMarkup QueueConfirmationButtons(int taskId, long userId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "✅ Qabul qilaman", 
                    $"accept_queue:{taskId}:{userId}"), 
                InlineKeyboardButton.WithCallbackData(
                    "❌ Rad etaman",   
                    $"reject_queue:{taskId}:{userId}")
            }
        });
    }
}