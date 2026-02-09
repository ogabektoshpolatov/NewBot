namespace bot.Entities;

public class Task
{
    public int Id { get; set; }
    public string? Name { get; set; } 
    public bool IsActive { get; set; } = true;
    public DateTime ScheduleTime { get; set; } 
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public long TelegramGroupId { get; set; } 
    public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}