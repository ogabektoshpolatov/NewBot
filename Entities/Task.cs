namespace bot.Entities;

public class Task
{
    public int Id { get; set; }
    public string? Name { get; set; } 
    public bool IsActive { get; set; } = true;
    public DateTime ScheduleTime { get; set; } 
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long TelegramGroupId { get; set; }
    public long CreatedUserId { get; set; }
    public bool SendToGroup { get; set; } = false;
    public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}