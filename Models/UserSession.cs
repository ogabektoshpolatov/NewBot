namespace bot.Models;

public class UserSession
{
    public long UserId { get; set; }
    public string? CurrentState { get; set; }
    public string? TaskName { get; set; } 
    public long? TelegramGroupId { get; set; }
    public bool? SendToGroup { get; set; }
}