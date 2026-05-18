using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace UniversityMethodologicalDepartment.Bus.Entities;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CurriculumItem> CurriculumItems { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Discipline> Disciplines { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<Specialty> Specialties { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Do not override connection configured by app factory (runtime/user override).
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Name=DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("net", "request_status", new[] { "PENDING", "SUCCESS", "ERROR" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_net")
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_pkey");

            entity.ToTable("audit_log", tb => tb.HasComment("Аудит изменений: кто, когда и что изменил/удалил/создал"));

            entity.HasIndex(e => e.OccurredAt, "idx_audit_log_occurred_at_desc").IsDescending();

            entity.HasIndex(e => e.TableName, "idx_audit_log_table_name");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.ActorDisplayName)
                .HasComment("Отображаемое имя пользователя в момент операции (снимок из JWT claims)")
                .HasColumnName("actor_display_name");
            entity.Property(e => e.ActorUserId)
                .HasComment("auth.uid() пользователя в момент операции")
                .HasColumnName("actor_user_id");
            entity.Property(e => e.EntityId)
                .HasComment("Значение поля id изменяемой сущности")
                .HasColumnName("entity_id");
            entity.Property(e => e.NewRow)
                .HasColumnType("jsonb")
                .HasColumnName("new_row");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("occurred_at");
            entity.Property(e => e.OldRow)
                .HasColumnType("jsonb")
                .HasColumnName("old_row");
            entity.Property(e => e.TableName).HasColumnName("table_name");
        });

        modelBuilder.Entity<CurriculumItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("curriculum_item_pkey");

            entity.ToTable("curriculum_item", tb => tb.HasComment("Учёт дисциплины по специальности в семестре: часы, курсовой проект, экзамен/зачёт"));

            entity.HasIndex(e => new { e.DisciplineId, e.SpecialtyId, e.Semester }, "idx_curriculum_item_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.Property(e => e.HasCourseProject).HasColumnName("has_course_project");
            entity.Property(e => e.IsCredit).HasColumnName("is_credit");
            entity.Property(e => e.IsExam).HasColumnName("is_exam");
            entity.Property(e => e.LabHours).HasColumnName("lab_hours");
            entity.Property(e => e.LectureHours).HasColumnName("lecture_hours");
            entity.Property(e => e.PracticalHours).HasColumnName("practical_hours");
            entity.Property(e => e.Semester).HasColumnName("semester");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.UsrHours).HasColumnName("usr_hours");

            entity.HasOne(d => d.Discipline).WithMany(p => p.CurriculumItems)
                .HasForeignKey(d => d.DisciplineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("curriculum_item_discipline_id_fkey");

            entity.HasOne(d => d.Specialty).WithMany(p => p.CurriculumItems)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("curriculum_item_specialty_id_fkey");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("department_pkey");

            entity.ToTable("department", tb => tb.HasComment("Кафедра: название, телефоны, факультет, заведующий"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FacultyId).HasColumnName("faculty_id");
            entity.Property(e => e.HeadId).HasColumnName("head_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phones).HasColumnName("phones");

            entity.HasOne(d => d.Faculty).WithMany(p => p.Departments)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("department_faculty_id_fkey");

            entity.HasOne(d => d.Head).WithMany(p => p.Departments)
                .HasForeignKey(d => d.HeadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("department_head_id_fkey");
        });

        modelBuilder.Entity<Discipline>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("discipline_pkey");

            entity.ToTable("discipline", tb => tb.HasComment("Дисциплина: название, кафедра"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Department).WithMany(p => p.Disciplines)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("discipline_department_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employee_pkey");

            entity.ToTable("employee", tb => tb.HasComment("Сотрудники: ФИО, учёная степень, звание, контакты"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Degree).HasColumnName("degree");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Patronymic).HasColumnName("patronymic");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.SectionId)
                .HasComment("Секция в рамках кафедры (department_id); опционально")
                .HasColumnName("section_id");
            entity.Property(e => e.Surname).HasColumnName("surname");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("fk_employee_department");

            entity.HasOne(d => d.Section).WithMany(p => p.Employees)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("employee_section_id_fkey");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("faculty_pkey");

            entity.ToTable("faculty", tb => tb.HasComment("Факультет; деканом является один из сотрудников"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeanId).HasColumnName("dean_id");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Dean).WithMany(p => p.Faculties)
                .HasForeignKey(d => d.DeanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("faculty_dean_id_fkey");
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("section_pkey");

            entity.ToTable("section", tb => tb.HasComment("Предметно-методическая секция кафедры: название, руководитель (опционально), контакты"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.HeadId).HasColumnName("head_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phones).HasColumnName("phones");

            entity.HasOne(d => d.Department).WithMany(p => p.Sections)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("section_department_id_fkey");

            entity.HasOne(d => d.Head).WithMany(p => p.Sections)
                .HasForeignKey(d => d.HeadId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("section_head_id_fkey");
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("specialty_pkey");

            entity.ToTable("specialty", tb => tb.HasComment("Специальность: код, название, квалификация, продолжительность, форма обучения"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.FormOfStudy).HasColumnName("form_of_study");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Qualification).HasColumnName("qualification");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
