using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class UserTodoService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : IUserTodoService
{
    public async Task<UserTodoPage> GetMyTodosAsync(bool includeCompleted, int page)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var owned = db.UserTodos.AsNoTracking().Where(x => x.UserId == actor.UserId && !x.IsDeleted);
        var pending = await owned.CountAsync(x => !x.IsCompleted);
        var query = includeCompleted ? owned : owned.Where(x => !x.IsCompleted);
        var count = await query.CountAsync();
        var rows = await query.OrderBy(x => x.IsCompleted).ThenBy(x => x.DueDate == null).ThenBy(x => x.DueDate)
            .ThenByDescending(x => x.Priority == "High").ThenByDescending(x => x.Priority == "Medium").ThenByDescending(x => x.UserTodoId)
            .Skip(Math.Max(0, page) * 10).Take(10).Select(x => new UserTodoDto
            { Id = x.UserTodoId, Title = x.Title, Notes = x.Notes, Priority = x.Priority, DueDate = x.DueDate, IsCompleted = x.IsCompleted, RowVersion = x.RowVersion }).ToListAsync();
        return new(rows, count, pending);
    }
    public async Task SaveAsync(UserTodoDto model)
    {
        var actor = await access.RequireContextAsync();
        var title = model.Title?.Trim() ?? "";
        var notes = model.Notes?.Trim();
        if (title.Length is < 1 or > 200 || notes?.Length > 1000 || model.Priority is not ("Normal" or "Medium" or "High"))
            throw new InvalidOperationException("Enter a title of up to 200 characters, notes of up to 1,000 characters and a valid priority.");
        await using var db = await factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        UserTodo todo;
        if (model.Id == 0)
        {
            todo = new UserTodo { UserId = actor.UserId, CreatedAtUtc = now };
            db.UserTodos.Add(todo);
        }
        else
        {
            todo = await OwnedAsync(db, actor.UserId, model.Id, model.RowVersion);
        }
        todo.Title = title; todo.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        todo.Priority = model.Priority; todo.DueDate = model.DueDate?.Date; todo.UpdatedAtUtc = now;
        await SaveChangesAsync(db);
    }
    public async Task SetCompletedAsync(int id, byte[] rowVersion, bool completed)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var todo = await OwnedAsync(db, actor.UserId, id, rowVersion);
        todo.IsCompleted = completed; todo.CompletedAtUtc = completed ? DateTime.UtcNow : null; todo.UpdatedAtUtc = DateTime.UtcNow;
        await SaveChangesAsync(db);
    }
    public async Task DeleteAsync(int id, byte[] rowVersion)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var todo = await OwnedAsync(db, actor.UserId, id, rowVersion);
        todo.IsDeleted = true; todo.UpdatedAtUtc = DateTime.UtcNow;
        await SaveChangesAsync(db);
    }
    private static async Task<UserTodo> OwnedAsync(HrmTeContext db, int userId, int id, byte[] rowVersion)
    {
        var todo = await db.UserTodos.SingleOrDefaultAsync(x => x.UserTodoId == id && x.UserId == userId && !x.IsDeleted)
            ?? throw new InvalidOperationException("This task is not available in your to-do list.");
        if (rowVersion == null || rowVersion.Length != 8) throw new InvalidOperationException("Refresh your list before changing this task.");
        db.Entry(todo).Property(x => x.RowVersion).OriginalValue = rowVersion;
        return todo;
    }
    private static async Task SaveChangesAsync(HrmTeContext db)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("This task changed elsewhere. Refresh your list and try again."); }
    }
}
