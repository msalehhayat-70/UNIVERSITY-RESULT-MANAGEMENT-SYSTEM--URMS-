using Microsoft.EntityFrameworkCore;
using URMS.Models.Entities;

namespace URMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "Host=localhost;Port=3000;Database=postgres;Username=postgres;Password=admin123";
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    // ── DbSets ───────────────────────────────────────────────────────────────
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AcademicProgram> Programs { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<BatchSemester> BatchSemesters { get; set; }
    public DbSet<SubjectConfiguration> SubjectConfigurations { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Gradesheet> Gradesheets { get; set; }
    public DbSet<StudentMark> StudentMarks { get; set; }
    public DbSet<SGPAResult> SGPAResults { get; set; }
    public DbSet<CGPAResult> CGPAResults { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AIAlert> AIAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── SystemConfig ─────────────────────────────────────────────────────
        modelBuilder.Entity<SystemConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ConfigKey).IsUnique();
            e.HasData(new SystemConfig { Id = 1, ConfigKey = "registered_accounts", ConfigValue = "0" });
        });

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Role).HasConversion<string>();
            e.HasOne(x => x.CreatedBy)
             .WithMany()
             .HasForeignKey(x => x.CreatedById)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── AcademicProgram ──────────────────────────────────────────────────
        modelBuilder.Entity<AcademicProgram>(e =>
        {
            e.HasKey(x => x.ProgramId);
            e.HasData(
                new AcademicProgram { ProgramId = 1, ProgramName = "BS Computer Science", ProgramCode = "CS" },
                new AcademicProgram { ProgramId = 2, ProgramName = "BS Computer Engineering", ProgramCode = "CE" }
            );
        });

        // ── Batch ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Batch>(e =>
        {
            e.HasKey(x => x.BatchId);
            e.HasOne(x => x.Program)
             .WithMany(p => p.Batches)
             .HasForeignKey(x => x.ProgramId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BatchSemester ─────────────────────────────────────────────────────
        modelBuilder.Entity<BatchSemester>(e =>
        {
            e.HasKey(x => x.BatchSemesterId);
            e.HasOne(x => x.Batch)
             .WithMany(b => b.Semesters)
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.BatchId, x.SemesterNo }).IsUnique();
        });

        // ── SubjectConfiguration ──────────────────────────────────────────────
        modelBuilder.Entity<SubjectConfiguration>(e =>
        {
            e.HasKey(x => x.ConfigId);
            e.Property(x => x.SubjectType).HasConversion<string>();
            e.HasOne(x => x.BatchSemester)
             .WithMany(bs => bs.Subjects)
             .HasForeignKey(x => x.BatchSemesterId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Teacher)
             .WithMany(u => u.AssignedSubjects)
             .HasForeignKey(x => x.TeacherId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Student ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Student>(e =>
        {
            e.HasKey(x => x.StudentId);
            e.HasIndex(x => x.RegistrationNo).IsUnique();
            e.Property(x => x.FatherName).HasDefaultValue(string.Empty);
            e.HasOne(x => x.Batch)
             .WithMany(b => b.Students)
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Gradesheet ────────────────────────────────────────────────────────
        modelBuilder.Entity<Gradesheet>(e =>
        {
            e.HasKey(x => x.GradesheetId);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.SubjectConfig)
             .WithOne(sc => sc.Gradesheet)
             .HasForeignKey<Gradesheet>(g => g.SubjectConfigId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Teacher)
             .WithMany()
             .HasForeignKey(x => x.TeacherId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedBy)
             .WithMany()
             .HasForeignKey(x => x.ReviewedById)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── StudentMark ───────────────────────────────────────────────────────
        modelBuilder.Entity<StudentMark>(e =>
        {
            e.HasKey(x => x.MarkId);
            e.Property(x => x.MarksObtained).HasPrecision(5, 2);
            e.Property(x => x.GradePoints).HasPrecision(3, 1);
            e.HasOne(x => x.Student)
             .WithMany(s => s.Marks)
             .HasForeignKey(x => x.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Gradesheet)
             .WithMany(g => g.StudentMarks)
             .HasForeignKey(x => x.GradesheetId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SubjectConfig)
             .WithMany()
             .HasForeignKey(x => x.SubjectConfigId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SGPAResult ────────────────────────────────────────────────────────
        modelBuilder.Entity<SGPAResult>(e =>
        {
            e.HasKey(x => x.SGPAId);
            e.Property(x => x.SGPA).HasPrecision(4, 2);
            e.HasOne(x => x.Student)
             .WithMany(s => s.SGPAResults)
             .HasForeignKey(x => x.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StudentId, x.SemesterNo }).IsUnique();
        });

        // ── CGPAResult ────────────────────────────────────────────────────────
        modelBuilder.Entity<CGPAResult>(e =>
        {
            e.HasKey(x => x.CGPAId);
            e.Property(x => x.CGPA).HasPrecision(4, 2);
            e.HasOne(x => x.Student)
             .WithMany(s => s.CGPAResults)
             .HasForeignKey(x => x.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StudentId, x.UpToSemester }).IsUnique();
        });

        // ── Notification ──────────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.NotifId);
            e.Property(x => x.NotifType).HasConversion<string>();
            e.HasOne(x => x.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AIAlert ───────────────────────────────────────────────────────────
        modelBuilder.Entity<AIAlert>(e =>
        {
            e.HasKey(x => x.AlertId);
            e.HasOne(x => x.Gradesheet)
             .WithMany()
             .HasForeignKey(x => x.GradesheetId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}