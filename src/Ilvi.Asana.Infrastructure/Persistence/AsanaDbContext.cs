using Ilvi.Asana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilvi.Asana.Infrastructure.Persistence;

/// <summary>
/// Ana veritabanı context'i
/// </summary>
public class AsanaDbContext : DbContext
{
    public AsanaDbContext(DbContextOptions<AsanaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AsanaTask> Tasks => Set<AsanaTask>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<SyncConfiguration> SyncConfigurations => Set<SyncConfiguration>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Turkish collation for proper sorting
        modelBuilder.UseCollation("Turkish_CI_AS");

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email);
        });

        // Workspace
        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasMany(w => w.Projects)
                .WithOne(p => p.Workspace)
                .HasForeignKey(p => p.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.Archived);
            
            entity.HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AsanaTask
        modelBuilder.Entity<AsanaTask>(entity =>
        {
            entity.ToTable("Tasks");
            
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasIndex(e => e.Completed);
            entity.HasIndex(e => e.DueOn);

            entity.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(t => t.CompletedBy)
                .WithMany()
                .HasForeignKey(t => t.CompletedById)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(t => t.ParentTask)
                .WithMany()
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(t => t.Attachments)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Stories)
                .WithOne(s => s.Task)
                .HasForeignKey(s => s.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskDependency (many-to-many)
        modelBuilder.Entity<TaskDependency>(entity =>
        {
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.DependsOnTaskId);
            entity.HasIndex(e => new { e.TaskId, e.DependsOnTaskId }).IsUnique();

            entity.HasOne(d => d.Task)
                .WithMany(t => t.Dependencies)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.DependsOnTask)
                .WithMany(t => t.Dependents)
                .HasForeignKey(d => d.DependsOnTaskId)
                .OnDelete(DeleteBehavior.NoAction); // Circular reference önlemek için
        });

        // Attachment
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.IsDownloaded);
        });

        // Story
        modelBuilder.Entity<Story>(entity =>
        {
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.Type);

            entity.HasOne(s => s.CreatedBy)
                .WithMany(u => u.CreatedStories)
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // SyncLog
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // UpdatedAt otomatik güncelleme
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
