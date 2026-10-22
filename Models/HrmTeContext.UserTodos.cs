using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<UserTodo> UserTodos => Set<UserTodo>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTodo>(entity =>
        {
            entity.ToTable("UT_UserTodos", "dbo", table => table.HasCheckConstraint("CK_UserTodos_Priority", "[Priority] IN ('Normal','Medium','High')"));
            entity.HasKey(x => x.UserTodoId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Priority).HasMaxLength(10).IsRequired();
            entity.Property(x => x.DueDate).HasColumnType("date");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.IsDeleted, x.IsCompleted, x.DueDate });
        });
    }
}
