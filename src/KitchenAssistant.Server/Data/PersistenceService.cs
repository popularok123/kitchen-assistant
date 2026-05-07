using KitchenAssistant.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenAssistant.Server.Data;

public class PersistenceService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PersistenceService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SaveMessageAsync(ChatMessage message)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Messages.Add(new MessageRecord
        {
            Id = message.Id,
            UserName = message.UserName,
            Content = message.Content,
            Timestamp = message.Timestamp,
            Type = (int)message.Type
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> LoadRecentMessagesAsync(int count = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .Select(m => new ChatMessage
            {
                Id = m.Id,
                UserName = m.UserName,
                Content = m.Content,
                Timestamp = m.Timestamp,
                Type = (MessageType)m.Type
            })
            .ToListAsync();
    }

    public async Task SaveSessionAsync(CookingSession session)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Sessions.FindAsync(session.Id);
        if (existing == null)
        {
            db.Sessions.Add(new SessionRecord
            {
                Id = session.Id,
                Name = session.Name,
                RecipeId = session.RecipeId,
                RecipeName = session.RecipeName,
                HostUserName = session.HostUserName,
                Status = (int)session.Status,
                StartTime = session.StartTime,
                EndTime = session.EndTime
            });
        }
        else
        {
            existing.Status = (int)session.Status;
            existing.EndTime = session.EndTime;
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<SessionRecord>> LoadRecentSessionsAsync(int days = 7)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var cutoff = DateTime.Now.AddDays(-days);
        return await db.Sessions
            .Where(s => s.StartTime >= cutoff)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }
}
