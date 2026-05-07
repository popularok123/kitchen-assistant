using Microsoft.EntityFrameworkCore;

namespace KitchenAssistant.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MessageRecord> Messages => Set<MessageRecord>();
    public DbSet<SessionRecord> Sessions => Set<SessionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageRecord>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.UserName).HasMaxLength(100);
            e.Property(m => m.Content).HasMaxLength(2000);
        });

        modelBuilder.Entity<SessionRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.HostUserName).HasMaxLength(100);
            e.Property(s => s.RecipeName).HasMaxLength(200);
        });
    }
}

public class MessageRecord
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Type { get; set; }
}

public class SessionRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public string HostUserName { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
