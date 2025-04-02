using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyDMVpro.Common;
using Microsoft.Extensions.Configuration;
using MyDMVpro.Models.DocumentsReceived_NoRequest;

namespace MyDMVpro.Models;

public partial class RequestStatusDbContext : DbContext
{
    public RequestStatusDbContext()
    {
    }

    public RequestStatusDbContext(DbContextOptions<RequestStatusDbContext> options)
    {
    }
    public virtual DbSet<RequestStatus> RequestStatus { get; set; }
    public virtual DbSet<MdpAppTypesSimple> MdpAppTypes { get; set; }
    public virtual DbSet<DocumentReceivedViewModel> DocumentReceivedView { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only.");
    }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only.");
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(MyDMVpro.Common.DataHelpers.SqlConnectionString);
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MdpAppTypesSimple>(entity =>
        {
            entity.HasKey(e => e.AppTypeId);

            entity.ToTable("mdpAppTypes");

            entity.HasIndex(e => new { e.VendorId, e.AppType })
                .HasDatabaseName("UK_mdpAppTypes")
                .IsUnique();

            entity.Property(e => e.AppTypeId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.AliasForAppType).HasMaxLength(5);

            entity.Property(e => e.AppType)
                .IsRequired()
                .HasMaxLength(5);

            entity.Property(e => e.AutoIMSEnabled).HasColumnName("AutoIMSEnabled");

            entity.Property(e => e.QueueName).HasMaxLength(50);

            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder
           .Entity<RequestStatus>()
                .HasNoKey()
                .ToView(MyDMVpro.Models.RequestStatus.RequestStatusViewName);

        modelBuilder
           .Entity<DocumentReceivedViewModel>()
                .HasNoKey()
                .ToView(DocumentReceivedViewModel.DocumentReceivedViewName);
    }
}
