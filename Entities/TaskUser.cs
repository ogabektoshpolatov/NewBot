namespace bot.Entities;

public class TaskUser
{
    public long Id { get; set; } 
    public int TaskId { get; set; }
    public Task Task { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public int QueuePosition { get; set; }
    public bool IsActive { get; set; } = true;
}