using HRM.Audit.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AuditLogActionType> AuditLogActionTypes { get; set; }
        public DbSet<AuditLogDataType> AuditLogDataTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // If you need any specific Fluent API configurations that Data Annotations 
            // can't handle, you put them here. Otherwise, the annotations on the models are enough.
        }
    }

     
}
