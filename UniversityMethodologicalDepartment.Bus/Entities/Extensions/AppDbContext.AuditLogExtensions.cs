using Microsoft.EntityFrameworkCore;

namespace UniversityMethodologicalDepartment.Bus.Entities;

public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.ActorDisplayName)
                .HasComment("Отображаемое имя пользователя в момент операции (снимок из JWT claims)")
                .HasColumnName("actor_display_name");
        });
    }
}
