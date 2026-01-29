using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AdpmDbContext : DbContext
{
    public AdpmDbContext(DbContextOptions<AdpmDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobTarget> JobTargets => Set<JobTarget>();
    public DbSet<JobResource> JobResources => Set<JobResource>();
    public DbSet<ServerInventory> ServerInventories => Set<ServerInventory>();
    public DbSet<ServerGroup> ServerGroups => Set<ServerGroup>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.Subject).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<AppUserRole>(entity =>
        {
            entity.ToTable("AppUserRoles");
            entity.HasKey(x => new { x.AppUserId, x.RoleId });
            entity.HasOne(x => x.AppUser)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("Jobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(x => x.RequestedBy).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.ServerGroup)
                .WithMany()
                .HasForeignKey(x => x.ServerGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobTarget>(entity =>
        {
            entity.ToTable("JobTargets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ServerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(100);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Job)
                .WithMany(x => x.Targets)
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobResource>(entity =>
        {
            entity.ToTable("JobResources");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ResourceType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(x => x.ResourceName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ResourcePath).HasMaxLength(500);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.JobTarget)
                .WithMany(x => x.Resources)
                .HasForeignKey(x => x.JobTargetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServerGroup>(entity =>
        {
            entity.ToTable("ServerGroups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<ServerInventory>(entity =>
        {
            entity.ToTable("ServerInventories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Hostname).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(100);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.Hostname);
            entity.HasOne(x => x.ServerGroup)
                .WithMany(x => x.Servers)
                .HasForeignKey(x => x.ServerGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Who).HasMaxLength(200).IsRequired();
            entity.Property(x => x.When).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.TicketRef).HasMaxLength(100);
            entity.Property(x => x.TargetAccount).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ServerGroup).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ResultSummary).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.OccurredOn).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
