using System.ComponentModel.DataAnnotations;

namespace bot.Entities;

public class User
{
    [Key]
    public long UserId { get; set; } 
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}