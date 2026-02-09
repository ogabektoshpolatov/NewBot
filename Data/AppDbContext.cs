using bot.Entities;
using Microsoft.EntityFrameworkCore;
using Task = bot.Entities.Task;

namespace bot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<TaskUser> TaskUsers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.UserId).IsUnique();

            entity.Property(u => u.Username).HasMaxLength(100);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
        });
        
        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.IsActive).HasDefaultValue(true);
            entity.Property(t => t.CreatedDate).HasDefaultValueSql("NOW()");
            entity.Property(t => t.ScheduleTime).IsRequired();
        });
        
        modelBuilder.Entity<TaskUser>(entity =>
        {
            entity.HasKey(tu => tu.Id);
            entity.HasIndex(tu => new { tu.TaskId, tu.QueuePosition }).IsUnique();
            
            entity.HasOne(tu => tu.Task)
                .WithMany(t => t.TaskUsers)
                .HasForeignKey(tu => tu.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tu => tu.User)
                .WithMany(u => u.TaskUsers)
                .HasForeignKey(tu => tu.UserId)
                .HasPrincipalKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(tu => tu.QueuePosition).IsRequired();
            entity.Property(tu => tu.IsActive).HasDefaultValue(true);
        });
    }
}