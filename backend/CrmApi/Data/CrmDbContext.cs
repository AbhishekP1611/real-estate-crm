using CrmApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Data;

public class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadStatusHistory> LeadStatusHistories => Set<LeadStatusHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().ToTable("Roles");
        b.Entity<Module>().ToTable("Modules");
        b.Entity<RolePermission>().ToTable("RolePermissions");
        b.Entity<User>().ToTable("Users");
        b.Entity<Source>().ToTable("Sources");
        b.Entity<Project>().ToTable("Projects");
        b.Entity<Lead>().ToTable("Leads");
        b.Entity<LeadStatusHistory>().ToTable("LeadStatusHistory");

        b.Entity<RolePermission>()
            .HasOne(x => x.Role).WithMany(r => r.Permissions).HasForeignKey(x => x.RoleId);
        b.Entity<RolePermission>()
            .HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId);

        b.Entity<User>()
            .HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);

        b.Entity<Lead>().HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId);
        b.Entity<Lead>().HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        b.Entity<Lead>().HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId);
        b.Entity<Lead>().Property(x => x.LeadCode).ValueGeneratedOnAddOrUpdate();
        b.Entity<Lead>().Property(x => x.Budget).HasPrecision(18, 2);
        b.Entity<Lead>().Property(x => x.DealValue).HasPrecision(18, 2);

        b.Entity<LeadStatusHistory>()
            .HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId);

        base.OnModelCreating(b);
    }
}
